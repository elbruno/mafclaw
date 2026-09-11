// A. Keep the calculation in explicit code.
// B. Return inputs and output so the work is auditable.
// C. This is the CodeAct teaching boundary before model-generated code.
var holdings = new[] { new Holding("MSFT", 35, 430.12m), new Holding("NVDA", 20, 142.50m) };
var total = holdings.Sum(item => item.Shares * item.Price);

Console.WriteLine("Sample 30 - plain CodeAct-style calculation");
Console.WriteLine($"Inputs: {string.Join(", ", holdings.Select(item => $"{item.Symbol}={item.Shares}x{item.Price}"))}");
Console.WriteLine($"Mock portfolio value: {total:0.00} USD");

internal sealed record Holding(string Symbol, int Shares, decimal Price);
