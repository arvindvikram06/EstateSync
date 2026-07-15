# EstateSync Styling Cheat Sheet

One page. Read this and you can style anything in the app without breaking the system.

## The one sentence that explains everything

We use **three tools with three jobs**: Angular Material provides interactive widgets (buttons, forms, dialogs), Tailwind handles layout and spacing in templates, and `styles.css` owns all colors (as tokens) and reusable patterns (as shared classes).

## The two token families

- **`--es-*`** (defined in `src/styles.css`) — OUR palette. Use these in any CSS you write.
- **`--mat-sys-*`** (defined in `src/material-theme.scss`) — feeds Angular Material's components. You rarely touch these; they make Material buttons coral instead of purple.

That's the whole difference. Hex color codes may exist ONLY inside those two files — `npm run lint:css` fails the build otherwise.

## The decision ladder (stop at the first step that fits)

1. Interactive widget (button, input, dialog, menu)? → **Material component** (`mat-flat-button`, `mat-form-field`…).
2. Layout, spacing, grid, one-off sizing? → **Tailwind classes in the template** (`flex gap-4 p-6 lg:grid-cols-2`).
3. A color, or a pattern used on 3+ pages? → **token / shared class in `styles.css`** (check the vocabulary below first — it probably exists).
4. Structure unique to one component? → **that component's own `.css` file**, colors via `var(--es-*)`.
5. Never `style="..."` in templates. Never hex outside the two token files.

## The ~25 tokens that matter

| Token | Value | Use it for |
|---|---|---|
| `--es-brand` | coral `#FF385C` | anything brand-colored you write by hand |
| `--es-brand-rgb` | `255 56 92` | don't use directly — it powers Tailwind's `bg-brand/10` alpha classes |
| `--mat-sys-primary` | same coral | Material components pick this up automatically |
| `--es-bg` | `#F7F7F7` | page background, grey tiles |
| `--es-bg-subtle` | near-white | subtle panel backgrounds |
| `--es-text` | `#222` | headings, primary text |
| `--es-text-secondary` | `#717171` | muted/secondary text |
| `--es-text-soft` | `#444` | body text between the two above |
| `--es-border` | `#DDD` | input borders, stronger lines |
| `--es-border-light` | `#EBEBEB` | card borders, dividers (most common) |
| `--es-radius` / `--es-radius-md` | 16 / 12px | card / inner-element rounding |
| `--es-success` / `--es-danger` / `--es-warning` | green / red / amber | status text & icons |
| `--es-green-bg` / `--es-danger-bg` / `--es-amber-bg` / `--es-blue-bg` | pale washes | status backgrounds (badges, banners) |
| `--es-primary-tint` / `--es-primary-wash` | pale brand | brand-tinted backgrounds |

(There are a few more in `styles.css` — hero gradients for the landing page, deep status shades — but the ones above cover day-to-day work.)

**Tailwind equivalents:** most tokens are wired into `tailwind.config.js`, so in templates you can write `text-es-muted`, `border-es-border-light`, `bg-es-subtle`, `bg-brand/10` instead of dropping into CSS at all. Prefer that.

## Shared-class vocabulary (use these before writing anything new)

| Class | Use when |
|---|---|
| `.es-card` | any white rounded card (matches the design spec: white / light border / 16px / soft shadow) |
| `.es-badge` + `.es-badge--approved/pending/rejected/…` | status pills — but normally just use `<app-status-badge [status]="x" />`, it picks the class for you |
| `.es-text-sm` / `.es-text-xs` / `.es-text-strong` | small/muted/bold text without inventing font sizes |
| `.es-text-success` / `.es-text-danger` / `.es-text-warn-deep` | colored status text (use as `[class]` toggles for conditional color) |
| `.es-info-banner--success/error/warning/info` | full-width message banners |
| `.kyc-banner--*` | the verification status banners on dashboards |
| `.stat-grid` + `.stat-card` + `.stat-icon--*` | dashboard KPI tiles |
| `.filter-pill` / `.option-pill` | pill-shaped filter/selector buttons above lists and in dialogs |
| `.admin-table` | data tables. ⚠ has `min-width: 560px` — ALWAYS put it inside `.admin-table-wrap` or an `overflow-x-auto` container or it will widen the page on mobile |
| `.table-scroll` | scrollable table body inside a card (see lease-detail) |
| `.no-scrollbar` | horizontal scroll with the scrollbar hidden (mobile pill rows) |
| `.es-btn-danger` / `.es-btn-danger-solid` | destructive buttons |
| `.rounded-dialog` | panelClass for dialogs → 20px rounded corners |

## How do I…

**…add a new color?** Add `--es-mything: #hex;` to `:root` in `styles.css` (section 3). If you also want Tailwind classes like `bg-mything`, add one line referencing the var in `tailwind.config.js` colors. Never put the hex anywhere else.

**…show a status badge?** `<app-status-badge [status]="lease.status" />` — it maps ~30 known statuses to the right colors. New status? Add one line to `STATUS_CLASS` in `status-badge.component.ts`.

**…open a dialog that behaves on mobile?** `this.dialog.open(X, { width: '100%', maxWidth: '480px', panelClass: 'rounded-dialog' })`. A global guard caps all dialogs at 92vw on phones anyway.

**…draw a border with Tailwind?** Tailwind's preflight is OFF (it would fight Material), so borders need the style spelled out: `border border-solid border-es-border-light`. One-side-only: `border-0 border-b border-solid`.

**…check I didn't break the rules?** `npm run lint:css` — fails on any hex outside the two token files.

## Never do

- `style="..."` in a template (conditional colors → `[class]` toggles with token classes)
- hex colors anywhere except `styles.css` `:root` and `material-theme.scss` (exception: color arrays passed to chart libraries — that's data)
- install another UI library (no Bootstrap/PrimeNG/etc.)
- recreate a Material widget with Tailwind, or use Material for layout
