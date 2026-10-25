namespace ClanWarsService.Domain.Battles;

/// <summary>
/// Win points shrink linearly once the winner's rating advantage passes <see cref="Gap"/>,
/// down to <see cref="MinFactor"/> of the normal win at <see cref="Gap"/> + <see cref="Scale"/>.
/// </summary>
public sealed record WeakOpponentPolicy(long Gap, long Scale, double MinFactor)
{
    public static WeakOpponentPolicy None { get; } = new(long.MaxValue, 1, 1);

    public long WinPoints(long winPoints, long winnerRating, long loserRating)
    {
        long advantage = winnerRating - loserRating;
        if (advantage <= Gap)
        {
            return winPoints;
        }

        double factor = Math.Max(MinFactor, 1 - ((double)(advantage - Gap) / Math.Max(1, Scale)));
        return (long)Math.Round(winPoints * factor, MidpointRounding.AwayFromZero);
    }
}
