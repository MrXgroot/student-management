# UI Design System and Frontend Interaction Guidelines

## 1. Visual Direction
A calm, modern, and high-density Indian higher-education administrative interface. Trustworthy, responsive, and legible for administrators and students.

## 2. Technology & Styling Specifications
- **Framework:** **Tailwind CSS (v3+)** configured via `tailwind.config.js`.
- **Icons:** **Lucide React** (`lucide-react`) for lightweight, consistent, accessible SVG icons.
- **Color Palette (Custom Tokens):**
  - **Primary (Deep Navy):** `primary-900: #0f172a`, `primary-800: #1e293b`, `primary-700: #334155`, `primary-600: #475569`.
  - **Accent (Academic Teal):** `accent-600: #0d9488`, `accent-500: #14b8a6`, `accent-100: #ccfbf1`.
  - **Neutrals (Cool Surfaces):** `surface-50: #f8fafc`, `surface-100: #f1f5f9`, `surface-200: #e2e8f0`, `surface-300: #cbd5e1`.
  - **Text (High-Contrast Charcoal):** `text-primary: #0f172a`, `text-secondary: #475569`, `text-muted: #64748b`.
  - **Semantics:**
    - Success: `emerald-600` (`#059669`) / `emerald-50`
    - Warning / Attention: `amber-600` (`#d97706`) / `amber-50`
    - Danger / Error: `rose-600` (`#e11d48`) / `rose-50`
    - Info: `sky-600` (`#0284c7`) / `sky-50`
- **Typography:** Modern sans-serif stack (`font-sans: Inter, system-ui, -apple-system, sans-serif`) with tabular figures (`tabular-nums`) enabled for marks, attendance percentages, and currency values.

---

## 3. Key Layout and Interaction Patterns

### 3.1 Multi-Role Workspace Switcher
- Located in the top header bar next to user profile details.
- Displays the user's active workspace context (e.g., *"Active Workspace: Faculty"*).
- When a user possesses multiple roles (e.g. `FACULTY` and `ACADEMIC_ADMIN`), clicking the switcher opens a dropdown allowing instant switching between contexts.
- Switching updates the sidebar navigation to show role-relevant links while preserving underlying user permissions.

### 3.2 Tabular Batch-Entry Pattern (Attendance & Marks)
Batch data entry for attendance sessions (30–70 students) and assessment marks (30–70 marks rows) uses a dedicated high-performance pattern:
1. **Local Grid State in React:** The component loads initial records into a local array (`useState` / `useReducer`) so keystrokes and status clicks do not trigger remote server calls or lag.
2. **Keyboard Navigation:**
   - Arrow keys (`Up` / `Down` / `Left` / `Right`) move focus between cells.
   - `Enter` commits the current cell and advances to the next student row.
   - For attendance, single-key shortcuts (`P` = Present, `A` = Absent, `L` = Late, `E` = Excused) update status immediately.
3. **Dirty-Cell Indicators:** Any cell modified from its original state receives a subtle visual indicator (e.g., a small teal corner dot or amber border highlight) so staff can easily scan unsaved modifications.
4. **Inline Validation:** Invalid values (e.g., marks $> \text{maxMarks}$ or negative numbers) render a localized red outline and tooltip, disabling the final submit action until resolved.
5. **Atomic Batch Submission:**
   - The user clicks an explicit primary action ("Submit Register" / "Save Draft").
   - The entire roster/marks array is sent in a single `PUT` request to the backend.
   - A loading overlay disables the grid during submission.
6. **Optimistic Concurrency Collision Handling:**
   - If another staff member modified the session while local editing was underway, the backend returns **HTTP 409 Conflict**.
   - The UI displays an alert: *"This register has been updated by another user since you opened it."* with two options:
     - **Review Changes:** Shows a side-by-side comparison of local edits against the latest server data.
     - **Reload:** Discards local edits and refreshes with the latest server state.

---

## 4. Accessibility and Semantic UI Rules

- Status badges must always display legible text alongside color markers (e.g., a green badge containing the text `"ACTIVE"` or a red badge with `"SUSPENDED"`). Never rely on color alone.
- Destructive actions (e.g., payment reversal, student suspension, course withdrawal) require an explicit confirmation modal detailing the consequence and requiring a mandatory reason.
- Form inputs retain entered values upon submission errors.
- Data tables provide explicit loading skeletons, zero-data empty states with helpful illustrations/actions, and retry buttons for error states.
