// Objective: represent one synthetic snapshot row with decimal arithmetic.
// A. Store symbol, quantity, snapshot price and sector.
// B. Calculate its snapshot value.
namespace MafClaw.Session04;

public sealed record Holding(string Symbol, int Shares, decimal Price, string Sector)
{
    public decimal Value => Shares * Price;
}
