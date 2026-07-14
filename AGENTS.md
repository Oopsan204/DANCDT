# AGENTS.md instructions

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tools** (when available): `codegraph_explore` answers most code questions in one call -- the relevant symbols' verbatim source plus the call paths between them. `codegraph_node` returns one symbol's source + callers, or reads a whole file with line numbers. If the tools are listed but deferred, load them by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` and `codegraph node <symbol-or-file>` print the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely -- indexing is the user's decision.
<!-- CODEGRAPH_END -->

<!-- NOTEBOOKLM_START -->
## NotebookLM

When the user asks to query or use information from their Google NotebookLM notebooks, use the NotebookLM MCP tools when available. Prefer `ask_question` for notebook-backed answers with citations, and use `add_source` only when the user explicitly wants a URL or pasted text added to a notebook. First-time use may require `setup_auth` so the user can sign in to Google in the spawned browser profile.
<!-- NOTEBOOKLM_END -->

<!-- SUPERPOWERS_START -->
## Superpowers

Superpowers skills are installed globally under `$CODEX_HOME/skills`.
At the start of a development task, check whether a Superpowers skill applies
and use the relevant skill before proceeding. In particular, use
`using-superpowers` when starting a new conversation or after compaction so the
Superpowers workflow is active across projects.
<!-- SUPERPOWERS_END -->

<!-- ANTIGRAVITY_UI_START -->
## Antigravity UI Delegation

Codex is the lead engineering agent.

Codex owns:
- Architecture
- Domain logic
- PLC, QD75, camera, MQTT, WebRTC, and machine-control code
- API integration
- State management
- Validation
- Tests
- Reviewing and integrating UI changes

Antigravity CLI is the UI implementation agent.

When UI work is needed:
1. Create or update `docs/ui-contract.md`.
2. Create or update `docs/ui-task.md`.
3. Invoke Antigravity CLI with `tools/run-antigravity-ui.ps1`.
4. Restrict Antigravity to UI directories.
5. Review the resulting Git diff.
6. Reject changes to logic directories.
7. Run tests before accepting the changes.

Never allow Antigravity to redesign API contracts without Codex review.
Do not use `--dangerously-skip-permissions`.
<!-- ANTIGRAVITY_UI_END -->
