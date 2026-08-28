# Observability

DreamLens emits operational signals for API health, AI cost, quota pressure, and provider failures.

## Metrics

Application metrics:

- `personakit.ai.estimated_cost_usd`: estimated AI provider cost per call.
- `personakit.ai.tokens`: AI provider token usage per call.
- `dreamlens.rate_limit.rejections`: global rate limiter rejections.
- `dreamlens.quota.rejections`: daily dream quota rejections.
- `dreamlens.provider.failures`: failed or invalid AI provider results.
- `dreamlens.async_jobs.completed`: completed SQS-backed jobs.
- `dreamlens.async_jobs.retried`: jobs scheduled for another attempt.
- `dreamlens.async_jobs.failed`: jobs that exhausted their retry budget.
- `dreamlens.async_jobs.lease_skipped`: messages whose jobs could not be leased because another worker owns or completed them.
- `dreamlens.async_jobs.processing.duration`: end-to-end worker processing latency, tagged only by job type and outcome.

The ADOT collector config in `infra/adot/collector.yaml` receives OTLP telemetry and exports traces to X-Ray and metrics to CloudWatch EMF under the `DreamLens` namespace.

## Hardening

The API adds response headers for content sniffing, framing, referrer policy, permissions policy, and a restrictive default content security policy.

Dream text is treated as sensitive user content. Tests assert that dream submission logs do not contain raw dream text.

## Load Smoke

Run the k6 smoke test against a local or deployed API:

```powershell
$env:DREAMLENS_BASE_URL = "https://api.example.com"
$env:DREAMLENS_TEST_TOKEN = "<optional bearer token>"
k6 run tests/load/dream-smoke.js
```

Without `DREAMLENS_TEST_TOKEN`, the script checks health endpoints only. With a token, it also probes `POST /v1/dreams` and accepts expected guarded responses such as 401, 429, or 503.
