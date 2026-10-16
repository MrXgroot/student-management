# ADR-001: MongoDB Replica Set and Transaction Strategy for Critical Multi-Document Operations

- Status: Accepted
- Date: 2026-10-16
- Decision owners: Architecture & Engineering Team

## Context
EduCore uses MongoDB as its primary persistence store. While MongoDB natively provides single-document atomicity, several core institutional workflows require updating multiple documents across collections:
1. **Fee Payment Recording:** Inserting a `payment` record and updating one or more `feeObligations` (reducing outstanding balances and updating status).
2. **Payment Reversal:** Marking a `payment` as `REVERSED`, voiding allocations, cancelling receipt metadata, and restoring previous outstanding balances on affected `feeObligations`.
3. **Course Enrollment with Section Capacity:** Decrementing available seats in `courseOfferings` and creating an enrollment in `courseEnrollments`.
4. **Admission Confirmation & Student Provisioning:** Transitioning an `application`, creating a `students` record, and provisioning a `users` account.

In a standalone MongoDB instance, multi-document transactions (`IClientSession.StartTransaction()`) are not supported. If an error occurs midway through a multi-document write on a standalone instance, the system risks inconsistent state (e.g. money recorded as received without updating the student's fee obligation). Furthermore, over-relying on transactions can degrade performance if aggregate boundaries are poorly designed.

## Decision
1. **Mandate Single-Node Replica Set for Local Development & Testing:**
   Local development (via Docker Compose or local `mongod --replSet rs0`) and CI environments will run MongoDB as a single-node replica set (`rs0`). This enables native multi-document ACID transactions without requiring a full clustered deployment.
2. **Pragmatic Aggregate Design to Maximize Single-Document Atomicity:**
   Where operations are strictly bounded and naturally part of an aggregate:
   - **Payment Allocations & Receipt:** Embed `allocations` (`[{ obligationId, amountPaise }]`) and receipt details directly inside the `payments` document. A single document insert records the payment, its receipt number, and the allocation distribution.
   - **Class Session Attendance:** Embed the student attendance register (`roster: [{ studentId, status, remarks, recordedAt }]`) inside `classSessions`. A single document replacement or update records attendance for all students in that session.
3. **Explicit Multi-Document Transactions for Ledger & Cross-Aggregate Operations:**
   Use explicit MongoDB multi-document transactions (via `IClientSession`) strictly where multiple distinct collection aggregates must remain consistent:
   - **Payment Collection:** In one transaction: insert `payments` (with embedded allocations/receipt), update affected `feeObligations` balances/statuses, and record `auditEvents`.
   - **Payment Reversal:** In one transaction: update `payments` to `REVERSED`, revert affected `feeObligations` balances/statuses, and record `auditEvents`.
   - **Admission Confirmation:** In one transaction: update `application` to `CONFIRMED`, insert `students` record, insert `users` record (in `PENDING_ACTIVATION` state), and increment the atomic counter.
4. **Transaction Failure Handling and Retry Safety:**
   - Transactions must execute through an application execution strategy that detects `MongoTransientTransactionException` and retries transient failures.
   - State-changing operations must require an `idempotencyKey` provided in the API request header (`X-Idempotency-Key`).
   - If a transaction aborts, all uncommitted writes are rolled back by MongoDB, ensuring no partial ledger states persist.

## Alternatives Considered
- **Option A: Pure Standalone MongoDB with Application-Level Sagas / Compensations:**
  Reject. Application-level compensation for payment collection and reversals is error-prone, complex, and vulnerable to system crashes midway through compensating actions.
- **Option B: Complete Normalization (Everything in Separate Collections with Distributed Transactions):**
  Reject. Highly normalized collections (e.g. separate `attendanceEntries` per student) produce excessive collection churn, high index overhead, and unnecessary multi-document transactions for everyday operations.
- **Option C: Relational Database (PostgreSQL):**
  Reject. Technology constraints dictate MongoDB. A single-node replica set brings ACID transactions to MongoDB with minimal configuration friction.

## Consequences
- **Benefits:** Guaranteed financial ledger integrity and atomic admission provisioning; high performance for bounded aggregates (attendance, embedded allocations); elimination of partial state corruptions.
- **Trade-offs:** Local developer setup and CI must initialize a single-node replica set (`rs.initiate()`) rather than a raw standalone `mongod`. Docker compose files and documentation must provide the exact startup script.
- **Testing Implications:** Integration tests must run against a replica set instance (e.g. via Testcontainers or pre-initialized Mongo container).

## Follow-up
- Update `docs/architecture.md`, `docs/database-schema.md`, and `docs/workflows/fees-and-payments.md`.
- Include replica-set initialization scripts in Phase 1 setup documentation.
