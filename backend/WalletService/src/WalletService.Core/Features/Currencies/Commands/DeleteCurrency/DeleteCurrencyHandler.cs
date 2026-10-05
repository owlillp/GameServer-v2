using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Currencies.Models;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Commands.DeleteCurrency;

public sealed class DeleteCurrencyHandler(
    IValidator<DeleteCurrencyCommand> validator,
    ICurrencyRepository currencyRepository,
    ITransactionManager transactionManager) : ICommandHandler<CurrencyDto, DeleteCurrencyCommand>
{
    public async Task<Result<CurrencyDto, Error>> Handle(
        DeleteCurrencyCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var currencyResult = Currency.Create(command.Code);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        string code = currencyResult.Value.Code;

        var getResult = await currencyRepository.GetByAsync(
            currency => currency.Code == code,
            cancellationToken);

        if (getResult.IsFailure)
        {
            return getResult.Error;
        }

        CurrencyDefinition currency = getResult.Value;
        if (!currency.IsActive)
        {
            return CurrencyMapper.ToDto(currency);
        }

        currency.Deactivate();

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return CurrencyMapper.ToDto(currency);
    }
}
