# Chat With Your Documents — Build Plan

A RAG ("Retrieval-Augmented Generation") web app: upload documents, ask
questions, and get answers grounded in those documents, with the exact source
passages cited. Built as a portfolio piece to showcase strong backend
engineering with a modern frontend.

> **RAG in one line:** instead of asking the AI a question blindly, first search
> *your own documents* for the relevant passages, then hand those passages to the
> AI so its answer is grounded in your data.

---

## Stack

- **Frontend:** Next.js (TypeScript)
- **Backend:** ASP.NET Core Web API (C#) — where all the real work happens
- **Database:** PostgreSQL + pgvector (stores both normal data and the searchable
  embedding vectors)
- **AI:** Claude API (writes the answers) + an embeddings provider
  (**Voyage AI**, `voyage-4-lite`, on its free tier by default)
- **Deployment:** Docker (`docker compose`)

**Security principle:** both API keys (Claude + embeddings) live **only in the
backend**, as environment variables. The browser never holds a key — it always
calls *your* API, and your API calls the outside services.

---

## Architecture

```
   Browser
      │
   ┌──▼──────────────┐
   │ Next.js frontend│   chat + upload UI (thin — just display & input)
   └──┬──────────────┘
      │  HTTP
   ┌──▼───────────────────┐
   │ ASP.NET Core Web API  │   the brain: ingestion, retrieval, orchestration
   └──┬───────────────┬────┘
      │               │
 ┌────▼─────┐   ┌─────▼──────────┐
 │ Postgres │   │  Claude API    │
 │(+pgvector│   │  Voyage API    │
 │  search) │   │ (embeddings)   │
 └──────────┘   └────────────────┘
```

**Journey 1 — ingestion (adding a document):** extract text → split into chunks
→ embed each chunk (Voyage) → store chunks + vectors in Postgres → mark the
document "Ready".

**Journey 2 — retrieval (asking a question):** embed the question → vector-search
Postgres for the closest chunks → send those chunks + the question to Claude with
"answer using only this context" → stream the answer back → show which chunks
were used as sources.

---

## Screens

The whole app is **one screen with three regions**, plus a couple of supporting
pieces.

- **Layout (the frame):** left sidebar (documents), wide main area (chat), slim
  top header (app name + "New chat").
- **Region 1 — Document sidebar:** "Upload document" button; list of documents
  each with a status badge ("Processing…" → "Ready") and a delete icon; a
  friendly empty state when there are none.
- **Region 2 — Chat area:** an empty state with 2–3 clickable example questions;
  a conversation state with alternating message bubbles, answers that **stream**
  in word-by-word, and an expandable **"Sources"** section under each answer
  showing the retrieved chunks + document names. *(The Sources feature is the
  single most impressive element — polish it.)*
- **Region 3 — Input bar:** text box + Send button pinned to the bottom; disabled
  with a spinner while an answer is streaming; Enter-to-send.
- **Supporting — Upload flow:** a modal (a small window that pops up over the
  page) with a drag-and-drop zone and a progress bar; a toast notification
  confirms success.

**Optional stretch (ship the MVP first, then add at most one):** a landing page;
document filtering (chat with only selected docs); multiple saved chat sessions;
login/auth.

---

## Build order (phase by phase)

Do **one phase at a time**. After each: run it, confirm the "Done when" check,
then commit to Git with a clear message before starting the next. Keep this
README/plan and a project `README.md` updated as you go.

**Phase 0 — Skeleton & Docker plumbing.** Repo with `/backend` (ASP.NET Core Web
API) and `/frontend` (Next.js), plus a root `docker-compose.yml` running three
containers: backend, frontend, and PostgreSQL with the pgvector extension
enabled. Add `.gitignore` and a git-ignored `.env` for secrets.
*Done when:* `docker compose up` starts all three; frontend shows a placeholder
page; backend returns a health-check response.

**Phase 1 — Data model.** EF Core with two entities: `Document` (id, filename,
status, timestamps) and `Chunk` (id, document id, text, vector column). Create
and apply the migration.
*Done when:* the tables exist in Postgres with the vector column present.

**Phase 2 — Ingestion endpoint.** Accept a PDF → extract text → chunk it → embed
each chunk (Voyage `voyage-4-lite`) → store chunks + vectors → flip document
status to "Ready".
*Done when:* uploading a PDF (via Postman/curl) produces `Chunk` rows with
populated vectors.

**Phase 3 — Chat endpoint (the RAG core).** Embed the question → vector
similarity search for the top chunks → build a prompt with those chunks + the
question → call the Claude API → **stream** the answer back, and return which
chunks were used.
*Done when:* a question returns a streamed, document-grounded answer plus its
source chunks.

**Phase 4 — Frontend skeleton & wiring.** Scaffold Next.js with the layout
(sidebar + header + main) as empty shells; centralize the code that calls your
API; confirm it reaches the backend health-check.
*Done when:* the framed app loads and can reach the backend.

**Phase 5 — Document sidebar.** Upload modal, document list with status badges,
live Processing → Ready updates, delete. Wire to Phase 2.
*Done when:* you can upload a PDF in the browser and watch it turn "Ready".

**Phase 6 — Chat UI.** Message thread, input bar, streaming display, expandable
Sources section. Wire to Phase 3.
*Done when:* you can ask a question and watch a grounded answer stream in with
working sources. **(This is the demo moment.)**

**Phase 7 — Polish.** Empty states, loading spinners, error messages, the
example-question buttons, consistent styling.
*Done when:* nothing looks broken or blank at the edges.

**Phase 8 — Ship it properly.** Finalize the multi-container Docker setup
(one `docker compose up`). Strong `README.md` with an architecture diagram, a
"Design decisions & trade-offs" section, and screenshots. A handful of backend
tests + a GitHub Actions workflow that runs them on every push.
*Done when:* a stranger could clone and run it from the README alone.

**Phase 9 (optional) — Deploy online.** Push the containers to Render, Railway,
or Fly.io (all take Docker directly) for a public URL to put on your CV.
*Done when:* the app is reachable at a public URL.

---

## How to build this with Claude Code

**The rule: one phase at a time.** Give Claude Code this file as context so it
understands the whole vision, but make it work phase-by-phase and stop after each
one so *you* review, test, and commit. Do not let it build multiple phases in a
single run — that produces unreviewable changes, messy Git history, and code you
don't understand well enough to discuss.

Workflow for every phase:
1. Tell Claude Code which phase to do (start with Phase 0).
2. Let it implement just that phase, then stop and summarize.
3. Run the phase's "Done when" check yourself.
4. Commit with a clear message (e.g. `feat: phase 2 — document ingestion`).
5. Tell it to proceed to the next phase.
