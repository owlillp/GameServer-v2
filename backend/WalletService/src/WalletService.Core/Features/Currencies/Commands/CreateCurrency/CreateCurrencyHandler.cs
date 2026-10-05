using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyHandler(
    IValidator<CreateCurrencyCommand> validator,
    IReadDbContext dbContext,
    ICurrencyRepository currencyRepository,
    ITransactionManager transactionManager) : ICommandHandler<CurrencyDto, CreateCurrencyCommand>
{
    public async Task<Result<CurrencyDto, Error>> Handle(
        CreateCurrencyCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var kindResult = CurrencyKindParser.Parse(command.Kind);
        if (kindResult.IsFailure)
        {
            return kindResult.Error;
        }

        var currencyResult = Currency.Create(command.Code);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        string code = currencyResult.Value.Code;

        bool alreadyExists = await dbContext.CurrenciesRead
            .AnyAsync(currency => currency.Code == code, cancellationToken);

        if (alreadyExists)
        {
            return GeneralErrors.AlreadyExists("currency", code);
        }

        var definitionResult = CurrencyDefinition.Create(code, command.DisplayName, kindResult.Value);
        if (definitionResult.IsFailure)
        {
            return definitionResult.Error;
        }

        var addResult = await currencyRepository.AddAsync(definitionResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return CurrencyMapper.ToDto(definitionResult.Value);
    }
}
