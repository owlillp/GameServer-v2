using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Features.Wallets;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletHandler(
    IValidator<TopUpWalletCommand> validator,
    WalletWriter walletWriter) : ICommandHandler<WalletDto, TopUpWalletCommand>
{
    public async Task<Result<WalletDto, Error>> Handle(
        TopUpWalletCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
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
            LedgerActorType.Service,
            ActorId: null,
            Comment: null);

        return await walletWriter.TopUpAsync(operation, cancellationToken);
    }
}
