// Objective: describe one fictional classroom holding.
// A. Store its mock symbol and asset class.
// B. Use decimal quantities and prices.
// C. Derive value without any market lookup.

namespace MafClaw.Sample43;

public sealed record Holding(string Symbol, string AssetClass, decimal Units, decimal Price)
{
    public decimal Value => Units * Price;
}
