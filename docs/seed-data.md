# Seed Data Strategy

## Purpose
Provide a coherent fictional institution that demonstrates realistic end-to-end workflows. Seed data is not random disconnected sample data.

## Institution
- Name: Vidyadeep Institute of Technology
- Location: Bengaluru, Karnataka, India
- Explicitly fictional; do not imply affiliation with a real institution.

## Programs
Examples: BCA, B.Sc. Computer Science, B.Com., BBA, MCA, MBA.
Program durations, course structures, grading rules and eligibility criteria are demo configuration, not universal Indian rules.

## Names
Use fictional Indian names such as Ananya Rao, Rohan Shetty, Aditi Nair, Arjun Kulkarni, Kavya Reddy, Nikhil Gowda, Meera Iyer, Ishaan Patil, Sneha Menon, and Rahul Deshmukh. Avoid real identifying combinations or using real people's records.

## Data relationships
Seed in dependency order:
1. Institution
2. Departments
3. Programs
4. Academic years and terms
5. Courses/subjects
6. Staff users and profiles
7. Course offerings and faculty assignments
8. Applications and students
9. Term registrations and course enrollments
10. Class sessions and attendance
11. Fee structures and obligations
12. Payments, allocations and receipts
13. Assessments, marks and results
14. Audit/demo scenarios as appropriate

## Required demo scenarios
- New application awaiting review
- Approved application ready for enrollment
- Active student with a complete profile
- Student with partial fee payment and a remaining balance
- Student with varied attendance history
- Draft marks not yet published
- Published result
- Student with a backlog/unsatisfied requirement
- A recorded payment reversal or result amendment only if its workflow is implemented

## Seed rules
- Idempotent: repeated seed runs update/ensure known demo records rather than duplicate them.
- Use fixed seed IDs or stable natural keys.
- Never seed production credentials or real personal information.
- Development-only demo credentials must be documented separately and must not be reused in production.
- Validate all references and unique indexes after seeding.
- Do not seed a state that violates business rules unless explicitly labeled as a test fixture for that rule.
