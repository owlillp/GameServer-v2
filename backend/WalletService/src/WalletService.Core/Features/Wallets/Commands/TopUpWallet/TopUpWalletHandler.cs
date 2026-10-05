using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Core.Services;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletHandler(
    IValidator<TopUpWalletCommand> validator,
    IWalletService walletService) : ICommandHandler<WalletDto, TopUpWalletCommand>
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

        var request = command.Request;

        var currencyResult = Currency.Create(request.Currency);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        var reasonResult = LedgerReasonParser.Parse(request.Reason);
        if (reasonResult.IsFailure)
        {
            return reasonResult.Error;
        }

        var operation = new WalletOperation(
            command.WalletId,
            currencyResult.Value,
            request.Amount,
            reasonResult.Value,
            request.OperationId ?? Guid.CreateVersion7(),
            request.ReferenceId,
            LedgerActorType.Service,
            ActorId: null,
            Comment: null);

        return await walletService.TopUpAsync(operation, cancellationToken);
    }
}
