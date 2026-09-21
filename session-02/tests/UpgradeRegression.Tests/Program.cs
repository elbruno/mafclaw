// Objective: run offline regressions against the real Session 2 policy and memory source.
// A. Exercise approval decisions without a model or an interactive console.
// B. Save and reload isolated synthetic memory without touching prior demo state.
// C. Return a failing process status if an invariant regresses.

return await UpgradeRegressionTests.RunAsync();
