using System.Data;
using System.Data.Common;
using CSharpFunctionalExtensions;
using EventService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using Wolverine.EntityFrameworkCore;

namespace EventService.Infrastructure.Postgres;

public sealed class TransactionManager(
    IDbContextOutbox<EventServiceDbContext> outbox,
    ILogger<TransactionManager> logger) : ITransactionManager
{
    private IDbContextTransaction? _currentTransaction;

    public async Task<UnitResult<Error>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _currentTransaction = await outbox.DbContext.Database.BeginTransactionAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to begin transaction");
            return GeneralErrors.DatabaseError();
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await outbox.DbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
            }

            return UnitResult.Success<Error>();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogError(exception, "Concurrency conflict during save");
            return GeneralErrors.ConcurrencyConflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException pgException)
        {
            return HandlePostgresException(pgException);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogError(exception, "Operation cancelled during save");
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error during save");
            return GeneralErrors.DatabaseError();
        }
    }

    public async Task<UnitResult<Error>> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return GeneralErrors.DatabaseError();
        }

        try
        {
            await outbox.DbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
            await outbox.FlushOutgoingMessagesAsync();
            return UnitResult.Success<Error>();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogError(exception, "Concurrency conflict during commit");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.ConcurrencyConflict();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException pgException)
        {
            await RollbackAsync(cancellationToken);
            return HandlePostgresException(pgException);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogError(exception, "Operation cancelled during commit");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error during commit");
            await RollbackAsync(cancellationToken);
            return GeneralErrors.DatabaseError();
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public DbConnection GetDbConnection()
    {
        DbConnection connection = outbox.DbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return connection;
    }

    public async ValueTask DisposeAsync() => await DisposeTransactionAsync();

    private Error HandlePostgresException(PostgresException pgException)
    {
        if (string.Equals(pgException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
            logger.LogWarning("Unique constraint violation: {Constraint}", pgException.ConstraintName);
            return GeneralErrors.UniqueConstraintViolation(pgException.ConstraintName);
        }

        if (string.Equals(pgException.SqlState, PostgresErrorCodes.ForeignKeyViolation, StringComparison.Ordinal))
        {
            logger.LogWarning("Foreign key violation: {Constraint}", pgException.ConstraintName);
            return GeneralErrors.ForeignKeyViolation(pgException.ConstraintName);
        }

        logger.LogError(pgException, "Database error: {SqlState}", pgException.SqlState);
        return GeneralErrors.DatabaseError();
    }

    private async Task RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to rollback transaction");
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}
