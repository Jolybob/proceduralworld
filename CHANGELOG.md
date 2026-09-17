# Changelog

## [0.1.107] - 2026-09-17

- Added `WorldGenerationExecutionCheckpoint` for resumable generation phase state across bounded execution budgets.
- Added lease-bound checkpoint storage with stale-worker protection, monotonic progress validation, and deterministic active-work ordering.
- Added checkpoint rebinding so expired work resumes from its last committed progress under a new execution attempt.
- Added `IWorldGenerationResumableWorkExecutor` and `WorldGenerationResumableExecutionRunner` for one-step-at-a-time generation execution across frames or worker budgets.
- Preserved failed checkpoints across explicit retries while removing execution checkpoints after successful completion.
- Added regression coverage for budgeted resume, lease reclaim, stale checkpoint rejection, deterministic active-work resumption, and retry state.
- Added world-generation execution checkpoint architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.107.

## [0.1.106] - 2026-09-17

- Added `WorldGenerationExecutionLease` as a deterministic ownership token for running generation work.
- Added `Claim`, `Renew`, `RecoverExpired`, and lease-authorized `Complete` / `Fail` lifecycle operations to the dependency work graph.
- Added deterministic attempt numbers and lease IDs so reclaimed work is distinguishable from stale worker executions.
- Added stale-worker protection: an expired or superseded lease cannot complete or fail a later execution attempt.
- Added `WorldGenerationExecutionLeaseCoordinator`, `IWorldGenerationLeasedWorkExecutor`, and `WorldGenerationExecutionLeaseRunner` for dependency-safe worker execution.
- Added regression coverage for lease creation, renewal, expiry/reclaim, deterministic claim ordering, stale-worker rejection, and runner success/failure.
- Added world-generation execution lease architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.106.

## [0.1.105] - 2026-09-17

- Added `WorldGenerationExecutionResult` with stable fingerprints for deterministic world-generation outputs.
- Added `WorldGenerationExecutionReceipt` and `IWorldGenerationExecutionReceiptStore` as a persistence-agnostic commit-evidence boundary.
- Added `InMemoryWorldGenerationExecutionReceiptStore` for runtime composition and regression testing.
- Added `WorldRealizationBatchCommitter` for atomic, idempotent world-space realization commits and exact replay handling.
- Added `IWorldGenerationTransactionalWorkExecutor` and `WorldGenerationTransactionalRunner` to complete generation work only after world commit and receipt persistence succeed.
- Added deterministic, process-independent execution fingerprints using an explicit stable hash.
- Added regression coverage for idempotent replay, atomic conflict handling, fingerprint determinism, transactional completion, failure isolation, and retry after receipt persistence failure.
- Added world-generation transaction architecture documentation and Unity `.meta` metadata.
- Bumped the package version to 0.1.105.

## [0.1.104] - 2026-09-17

- Added persistent `WorldGenerationWorkStatus` state for pending, running, completed, and failed generation work.
- Changed the dependency-aware graph so dequeued work remains in the graph as `Running` until explicitly completed or failed; dependency edges therefore survive execution budgets.
- Added `Complete`, `Fail`, `Retry`, status counters, and status queries for deterministic execution lifecycle management.
- Updated the dependency scheduler runner to mark successful executor calls completed and failed calls failed before propagating the exception.
- Added failed-dependency diagnostics and deterministic retry behavior.
- Added regression coverage for multi-budget phase execution, running prerequisites, failed work, retry, and persistent dependency state.
- Hardened cycle diagnostics so running prerequisites are reported as blocked work rather than falsely classified as dependency cycles.
- Bumped the package version to 0.1.104.

## [0.1.103] - 2026-09-17

- Added `WorldGenerationWorkKey` so generation work is identified by `(chunk, phase)` rather than chunk alone.
- Added `WorldGenerationDependency` and `WorldGenerationWorkGraph` for explicit same-chunk and cross-chunk prerequisites.
