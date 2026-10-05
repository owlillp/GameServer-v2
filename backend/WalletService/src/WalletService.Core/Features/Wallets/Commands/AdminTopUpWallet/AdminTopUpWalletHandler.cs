using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Core.Services;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Wallets.Commands.AdminTopUpWallet;

public sealed class AdminTopUpWalletHandler(
    IValidator<AdminTopUpWalletCommand> validator,
    IWalletService walletService,
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

        var request = command.Request;

        Guid? actorId = userScope.Id;
        if (actorId is null)
        {
            return Error.Authentication("authentication.required", "User identity is not available");
        }

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
            WalletActorResolver.Resolve(userScope.Roles),
            actorId,
            request.Comment);

        return await walletService.TopUpAsync(operation, cancellationToken);
    }
}
