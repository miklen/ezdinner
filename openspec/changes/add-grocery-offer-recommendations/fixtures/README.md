# Prej evaluation fixtures

Captured from authenticated evaluation requests on 2026-10-18. Each JSON envelope records the requested path, retrieval time, HTTP status, safe quota headers, and parsed response. No request authorization header, key, account identity, family data, or image contents are stored.

| Fixture | Evidence |
| --- | --- |
| `prej-nemlig-offers.json` | First three meat/fish offers; endpoint total 77 |
| `prej-rema1000-offers.json` | First three meat/fish offers; endpoint total 22 |
| `prej-nemlig-total.json` | One offer; unfiltered retailer total 965 |
| `prej-rema1000-total.json` | One offer; unfiltered retailer total 188 |
| `prej-product-observations.json` | Two products with per-chain observation dates and classification evidence |
| `prej-rema1000-page2.json` | Second page of three meat/fish offers, distinct from page 1 |
| `prej-chains.json` | All 21 returned chain records and border-trade/flyer-only descriptors |

These small samples are not a complete snapshot, production cache, or proof of all conditional-offer behavior. Preserve source anomalies when testing: product 17590's description mentions 750 g while its structured quantity is 650 g. Product matching and observation freshness need validation before an opportunity can qualify.

The final chains response reports 18 of 25 calls remaining after seven evaluation requests. Do not replay these requests during tests. Use offline fixtures and label synthetic edge cases explicitly. The source contract and unresolved enablement requirements are recorded in [the design](../design.md#authenticated-evaluation-follow-up--2026-10-18).
