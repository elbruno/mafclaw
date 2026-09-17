// Objective: represent one fictional holding without account identifiers.
// A. Name a mock symbol and asset class.
// B. Store decimal units and prices for repeatable arithmetic.
// C. Compute value without accessing market services.

namespace MafClaw.Sample42;

public sealed record Holding(string Symbol, string AssetClass, decimal Units, decimal Price)
{
    public decimal Value => Units * Price;
}
