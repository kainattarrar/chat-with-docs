@AGENTS.md

# Project structure conventions

- `src/components/layout/` — app shell pieces (Header, Sidebar, etc.).
- `src/components/ui/` — reusable presentational primitives (buttons, spinners,
  badges, status indicators). Not tied to any one feature or layout region.
- `src/features/<feature>/` — feature-specific code, e.g. `src/features/documents`,
  `src/features/chat`, each holding that feature's own components and hooks.
  Create a feature folder when that feature is actually built, not ahead of time.
- `src/lib/` — cross-cutting code shared by everything: the API client (`api.ts`)
  and shared types (`types.ts`).
