// Objective: make the valuation output independently testable.
// A. Return the total, Technology percentage and synthetic-data label.
namespace MafClaw.Session04;

public sealed record PortfolioSummary(decimal Total, decimal TechnologyPercent, string Source);
