using WalletService.Domain.Authorization;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Models;

internal static class WalletActorResolver
{
    public static LedgerActorType Resolve(IReadOnlyList<string> roles)
    {
        if (roles.Contains(WalletRoles.ADMIN, StringComparer.Ordinal))
        {
            return LedgerActorType.Admin;
        }

        if (roles.Contains(WalletRoles.MODERATOR, StringComparer.Ordinal))
        {
            return LedgerActorType.Moderator;
        }

        return LedgerActorType.Player;
    }
}
