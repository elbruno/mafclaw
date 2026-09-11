---
name: valuation
description: Value a mock stock holding with the bundled mock price reference.
---

# Mock holding valuation

Use this skill when the user asks for the value of one mock stock holding.

1. Read `references/mock-prices.csv`.
2. Find the requested ticker's mock price.
3. Multiply the supplied share count by that price.
4. State the calculation and label the result as mock educational data.

Do not retrieve real prices, predict future performance, or recommend a trade.
