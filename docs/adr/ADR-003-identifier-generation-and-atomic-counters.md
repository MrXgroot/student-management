# ADR-003: Identifier Generation Strategy and Atomic Sequence Counters

- Status: Accepted
- Date: 2026-10-16
- Decision owners: Architecture & Engineering Team

## Context
EduCore requires four distinct categories of identifiers across academic, financial, and student records:
1. **System Primary Keys:** Internal identifiers for document references and API routing.
2. **Admission Number:** Official human-readable admission reference assigned when an application is approved and confirmed.
3. **Institutional Roll Number:** Human-readable student identifier assigned upon term/cohort registration.
4. **Receipt Number:** Official financial receipt reference assigned when a fee payment is recorded.
5. **University Registration Number:** External identifier issued by an affiliating university (e.g. Bangalore University / VTU), which may be assigned months after admission or remain absent.

In a concurrent web environment, generating human-readable numbers using client-side counting (`CountDocumentsAsync() + 1`) suffers from race conditions leading to duplicate numbers. Conversely, relying strictly on GUIDs creates unwieldy, unreadable codes that violate standard Indian college administrative conventions. Furthermore, database transaction rollbacks mean true "gapless" sequences cannot be guaranteed without serializing writes and inducing severe bottlenecks.

## Decision
1. **System Primary Key Strategy:**
   - All MongoDB documents use native `ObjectId` internally for indexing efficiency and compact storage.
   - In C# domain models, DTOs, and API JSON responses, all `ObjectId` fields are represented strictly as standard 24-character hexadecimal `string` values. This avoids serialization issues with `System.Text.Json` and keeps the API contract clean and portable.
2. **Atomic Counter Collection (`counters`):**
   - Create a dedicated MongoDB collection named `counters` with the schema:
     `{ _id: string, sequenceValue: long, prefix: string, year: int }`
   - Use MongoDB's atomic `FindOneAndUpdateAsync` operation with `$inc` to increment and return the next sequential number:
     ```csharp
     var update = Builders<CounterDoc>.Update.Inc(x => x.SequenceValue, 1);
     var options = new FindOneAndUpdateOptions<CounterDoc> { ReturnDocument = ReturnDocument.After, IsUpsert = true };
     var counter = await _countersCollection.FindOneAndUpdateAsync(filter, update, options);
     ```
3. **Format Standards:**
   - **Admission Number:** `ADM-{YYYY}-{00000}` (e.g., `ADM-2024-00042`), unique per institution.
   - **Receipt Number:** `REC-{YYYY}-{00000}` (e.g., `REC-2024-00105`), unique per institution.
   - **Roll Number:** `{YY}{PROGRAM_CODE}{SECTION}{000}` (e.g., `24BCA-A015`), assigned at cohort registration.
   - **Application Number:** `APP-{YYYY}-{00000}` (e.g., `APP-2024-00301`), assigned upon application submission.
4. **Sequence Invariants and Failed Operation Handling:**
   - Atomic counters guarantee **uniqueness**, not strictly gapless sequences. If a transaction (such as admission confirmation or payment recording) generates a sequence value but subsequently fails or rolls back, the incremented counter value is discarded, resulting in a harmless gap.
   - **Uniqueness Enforcement:** Every generated identifier is backed by a **unique MongoDB index** on the respective collection (e.g., `unique: true` on `students.admissionNumber` and `payments.receipt.receiptNumber`). The application must never rely solely on application logic for uniqueness.

## Alternatives Considered
- **Option A: Pure GUIDs Everywhere:**
  Reject. Impractical for receipts, student IDs, and printed reports. College staff require human-readable reference numbers.
- **Option B: Dynamic Aggregation Counting (`Count() + 1`):**
  Reject. Guaranteed race condition leading to duplicate key violations under concurrent requests.
- **Option C: Strict Gapless In-Memory Sequence Lock:**
  Reject. Incurs distributed locking overhead and risks stalling throughput on high-volume payment or admission days.

## Consequences
- **Benefits:** Thread-safe, atomic, human-readable numbers; high performance; resilient against duplicate key violations.
- **Trade-offs:** Possible numerical gaps in the sequence if transactions abort. This is acceptable under standard accounting and institutional software practices so long as audit records track all attempted operations.
- **Testing Implications:** Seed scripts and concurrent test fixtures can safely call the sequence generator or supply deterministic fixed sequence numbers.

## Follow-up
- Update `docs/database-schema.md`, `docs/indexes.md`, and `docs/business-rules.md`.
- Implement `ISequenceGeneratorService` in Phase 1/Phase 2.
