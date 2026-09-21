# Synthetic evaluation corpus

`cases.json` is versioned source data, not captured user conversations. Its
24 unique IDs cover decimal portfolio/quote results, policy, invalid trade
parameters, actual hosted/fixture capability registration and screened output.
The Evals host hashes this file into each distinct result report.

The separate executable test suite adds actual approval decisions, an
out-of-scope memory-tool attempt, cancellation/concurrency, post-processor
telemetry inspection, OTLP transport and the real Responses HTTP lifecycle.
Case results are assertions, not fabricated relevance/coherence scores.

Use the explicit fixture/live/foundry modes described in
[`docs\evaluations.md`](../docs/evaluations.md). Keep raw model responses and
environment settings out of committed datasets and reports.
