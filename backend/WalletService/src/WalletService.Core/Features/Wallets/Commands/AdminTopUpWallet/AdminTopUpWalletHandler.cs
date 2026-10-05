using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Features.Wallets;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Commands.AdminTopUpWallet;

public sealed class AdminTopUpWalletHandler(
    IValidator<AdminTopUpWalletCommand> validator,
    WalletWriter walletWriter,
    UserScopedData userScope) : ICommandHandler<WalletDto, AdminTopUpWalletCommand>
{
    public async Task<Result<WalletDto, Error>> Handle(
        AdminTopUpWalletCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Guid? actorId = userScope.Id;
        if (actorId is null)
        {
            return Error.Authentication("authentication.required", "User identity is not available");
        }

        var currencyResult = Currency.Create(command.Currency);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        var reasonResult = LedgerReasonParser.Parse(command.Reason);
        if (reasonResult.IsFailure)
        {
            return reasonResult.Error;
        }

        var operation = new WalletOperation(
            command.WalletId,
            currencyResult.Value,
            command.Amount,
            reasonResult.Value,
            command.OperationId ?? Guid.CreateVersion7(),
            command.ReferenceId,
            WalletActorResolver.Resolve(userScope.Roles),
            actorId,
            command.Comment);

        return await walletWriter.TopUpAsync(operation, cancellationToken);
    }
}
