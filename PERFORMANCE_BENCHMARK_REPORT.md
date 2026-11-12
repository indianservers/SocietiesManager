# Performance benchmark report

12 November 2026. Actual local execution, synthetic data, freshly migrated disposable PostgreSQL database. No production capacity or SLA guarantee.

## Acceptance outcome

**LOCAL TARGETS NOT ALL MET.** Failed local p95 objectives: dashboard, ageing. Keep the performance release gate blocked.

Phase 6 read target: p95 below 5,000 ms. Existing finance read target retained at 2,000 ms. Thresholds were not raised to hide misses. The benchmark writes its complete artifact before asserting targets; a nonzero exit caused by these misses is an honest failed performance gate, not a missing report.

## Hardware and method

- os: Windows-11-10.0.26200-SP0
- logical_cpu_count: 12
- processor: Intel64 Family 6 Model 151 Stepping 5, GenuineIntel
- memory_bytes: 34190524416

Thirty warm authenticated HTTP samples per route after warm-up. p50 is median, p95/p99 use 30-sample order statistics (p99 is the sample maximum). This is a Development-mode HTTP baseline with current cookies/CSRF, preserved rate limits and synthetic independent reviewer accounts; it does not include deployed TLS/proxy/MFA/provider latency. Local developer/background processes were present. CPU/RSS refer only to the API process; PostgreSQL/whole-host peaks were not measured. Cold starts, sustained production concurrency and long-running tail/error budgets are not certified.

## Asserted fixture dimensions

- societies: 1,000
- regions: 10,000
- chapters: 100,000
- memberships: 100,000
- invoices: 300,000
- journal_lines: 600,000
- activities: 100,000
- events: 10,000
- messages: 100,000
- service_projects: 10,000

The main tenant has 100,000 memberships across 10,000 chapters; the remaining tenants extend global organization totals. This is not a uniform 100,000-members-per-society test. Large accounting/activity/message datasets are direct SQL synthetic fixtures with integrity triggers enabled; these are not claimed as API write or real delivery throughput. Default bulk-selection timing scans the 100,000-row membership dataset with the Active filter; it does not mean all 100,000 rows were selected or rendered. Actual rendering is measured separately.

## Authenticated finance read latency

- invoice_register: p50 672.283 ms; p95 840.933 ms; p99 870.503 ms; target PASS.
- chapter_filter: p50 86.213 ms; p95 108.138 ms; p99 111.947 ms; target PASS.
- dashboard: p50 1972.23 ms; p95 2451.668 ms; p99 2517.138 ms; target FAIL.
- ledger_cursor: p50 1438.208 ms; p95 1788.173 ms; p99 1862.995 ms; target PASS.
- trial_balance: p50 1114.006 ms; p95 1348.671 ms; p99 1639.103 ms; target PASS.
- ageing: p50 1572.129 ms; p95 2341.538 ms; p99 2954.408 ms; target FAIL.

## Phase 6 read latency

- integrated_intelligence: p50 2927.909 ms; p95 3294.178 ms; p99 3321.71 ms; 0/30 failures; target PASS.
- bulk_selection_100k: p50 484.892 ms; p95 565.832 ms; p99 574.178 ms; 0/30 failures; target PASS.
- project_directory: p50 72.493 ms; p95 87.87 ms; p99 88.834 ms; 0/30 failures; target PASS.
- verified_impact: p50 1649.381 ms; p95 1748.3 ms; p99 1774.097 ms; 0/30 failures; target PASS.
- unified_members_report: p50 608.043 ms; p95 726.69 ms; p99 826.956 ms; 0/30 failures; target PASS.
- platform_tenant_page: p50 513.564 ms; p95 709.076 ms; p99 716.605 ms; 0/30 failures; target PASS.

## Queue, concurrency and actual documents

- Annual billing: 1000/1000 completed in 13739.76 ms (72.78/s). Actual assessment and pending-review invoice creation; not issued-journal/PDF/payment throughput for 100000 recipients.
- Overlapping readers: 6; requests 24; failures 0; p95 672.818 ms.
- Card: {"recipients": 1000, "state": "Completed", "issued": 1000, "failed": 0, "elapsed_ms": 197729.803, "documents_per_second": 5.057, "rendering": "Actual server-side 300 DPI front/back image and physical-size PDF generation, not SQL fixtures"}.
- Certificate: {"recipients": 100, "state": "Completed", "issued": 100, "failed": 0, "elapsed_ms": 100324.399, "documents_per_second": 0.997, "rendering": "Actual server-side 300 DPI single-sided certificate image and physical-size PDF generation; template has no back"}.
- ZIP: {"credentials": 1000, "state": "Completed", "elapsed_ms": 129952.32, "scope": "Actual durable ZIP export generation; download authorization tested in integration suite"}.

Cards/certificates are actual independently approved durable jobs and 300 DPI server image/PDF output, not counts inferred from SQL. Default templates use text/QR, without photo-heavy or complex-script rendering. The ZIP job really creates the archive; authorized download contents and user-bound links are separately exercised by HTTP integration tests. A 100,000-recipient rendering run, physical printer speed and disaster recovery at this scale were not measured.

API process metrics: {"peak_api_rss_bytes": 627879936, "api_cpu_seconds": 449.641}.

## Representative PostgreSQL plans

- invoice_due_index: execution 0.891 ms; top node Limit; actual rows 50; shared hits 0; shared reads 53.
- member_statement: execution 0.949 ms; top node Index Scan; actual rows 6; shared hits 4; shared reads 5.
- credential_worker: execution 0.017 ms; top node Limit; actual rows 0; shared hits 1; shared reads 0.
- project_verified_impact: execution 3.297 ms; top node Aggregate; actual rows 1; shared hits 264; shared reads 0.

Full EXPLAIN ANALYZE/BUFFERS trees are in tests/phase6-performance.json. These representative plans are not claimed as complete plans for every dashboard/ageing subquery. Statement-level quota triggers and partial worker indexes remain enabled; integrity was not disabled for speed.

## Actual encrypted backup and restore

- AES-256-GCM encrypted archive: 118333487 bytes.
- Backup 60.972 seconds; isolated restore/verification 98.725 seconds.
- 130 tables; all snapshot counts matched; 3232 object hashes matched; 1 test key-directory files; unbalanced posted journals 0.
- Generated verification database dropped afterward. No running database or objects were overwritten. The test used an ephemeral AES key and a marker, not deployed Data Protection secret recovery. Off-host RPO/RTO/PITR remain unverified.

## Reproducibility and remaining work

Build the API, then run `python tests/phase6_api_benchmark.py`; optionally select an isolated build through ASSEMBLIA_API_DLL. It creates and drops only its generated benchmark database. Run `python tests/backup_restore_integration.py` for the isolated logical restore drill. Generate this report with `python scripts/phase6_benchmark_report.py` after both artifacts exist. Raw artifacts are the source of the figures, including failures.

The first attempt after the date change lost its fixture session; the harness now renews fixture authentication without weakening application sessions. A subsequent rendering attempt reached 934/1,000 cards with zero record failures but hit an arbitrary 240-second harness wait. The completed run uses a 900-second wait and records actual throughput; the longer wait is not represented as a speed improvement.

For missed finance targets, capture full scoped dashboard/ageing SQL plans, reduce repeated/large aggregations while preserving legal-entity separation and immutable historical postings, and repeat the scenario on intended launch hardware. Do not mark the release gate passed until measured objectives are accepted. Production concurrency, resource limits, queue fairness, storage cleanup and a representative photo/language/printer campaign remain acceptance work.

Measured API binary SHA-256: `C29F8260310C7E7D4282CC664909C7808EE587F355AD2CC4CF8C26C73ACC7904`. The final functional suite additionally includes nested-null request validation; this benchmark is not claimed as a production release-artifact certification.
