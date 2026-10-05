namespace WalletService.Domain.Wallets.Ledgers;

public enum LedgerReason
{
    Unknown = 0,
    Purchase = 1,
    BattlePassReward = 2,
    EventReward = 3,
    ClanSeasonReward = 4,
    DailyReward = 5,
    Refund = 6,
    AdminAdjustment = 7,
    Transfer = 8,
    Compensation = 9,
}
