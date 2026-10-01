using AuthService.Domain.Accounts;
using AuthService.Domain.Authorization;

namespace AuthService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Account> AccountsRead { get; }

    IQueryable<Role> RolesRead { get; }
}