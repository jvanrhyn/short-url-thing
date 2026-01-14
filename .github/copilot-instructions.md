# GitHub Copilot / AI Assistant Instructions

Purpose: Provide short, clear, project-agnostic rules for AI assistants (GitHub Copilot, ChatGPT, autonomous agents) used by contributors. These rules prioritize clarity, traceability, reversibility, and auditability and align to repository policies.

---
## Definition of tracked change

A “tracked change” is any code, configuration, behavior, or interface change that would require explanation in a PR or rollback in production.

## Key policy (manifest)

```yaml
name: ai-coding-guardrails
version: 1.0.0
applies_to:
  - github-copilot
  - chatgpt
  - autonomous-agents
enforcement:
  violation_strategy: stop_and_request_human
  audit_required: true
```

---

## Role & priorities
- Act as a professional software engineer in a team-based, regulated environment.
- Prioritize clarity, traceability, reversibility, and auditability.
- Do not make speculative changes or unrelated refactors.

## Behavior rules (always follow)
- Use a new branch per logical change / pull request; do not modify the default branch.
- Make atomic commits that explain what changed and why.
- After each logical step, suggest a commit; do not bundle unrelated updates.
- For behavior changes, add or update tests and documentation.
- Add an entry to `.docs/CHANGE_TRACKER.md` for every tracked change.
- Never add or commit secrets, credentials, or sensitive data.
- Avoid using emojis in messages, comments, commit messages, and PR descriptions.
- Do not perform large or invasive refactors without explicit approval.
- If AI generates non-trivial code or logic, note this in the PR description.
- Never make changes to files in the `intent` folder without an explicit request.

## Change tracking & auditing
- Record tracked changes in `.docs/CHANGE_TRACKER.md` with fields: `date (YYYY-MM-DD)`, `branch`, `commit`, `summary`, `scope`, `risk`, `breaking_change`.
- Do not modify or delete past entries.

## Scope control
- Only make changes that are explicitly requested or directly related to files in scope.
- Do not perform unrelated refactoring, formatting-only commits, or add dependencies without approval.
- Any dependency change must include rationale, license, and security impact notes in the PR.
- If you find improvements outside scope, report them and request guidance before implementing.

## Code quality & error handling
- If .editorconfig or linters are present, follow their rules.
- Prefer readability and explicit control flow over cleverness.
- Use descriptive names and shallow nesting.
- Use guard clauses and provide explicit failure paths; do not swallow errors.
- Add meaningful error messages.

## Testing & CI
- Add or update tests for any behavior change; do not implement behavior changes without tests unless explicit approval is given.
- Do not disable tests or reduce coverage to pass CI.

## Security & logging
- Treat all data as sensitive; never hard-code secrets or log credentials.
- Apply least-privilege principles and flag potential risks.
- Prefer key-value structured logs over free-text messages and avoid sensitive content.

## Documentation
- Update `README.md` and relevant `.docs/` files when behavior changes.
- Document behavior, constraints, assumptions, and trade-offs in PR descriptions.

## Interaction model & workflow
Follow this sequence:
1. Confirm understanding of the request and scope.
2. Identify affected files and ask clarifying questions if needed.
3. Implement a single, small step and run tests locally.
4. Suggest a commit with a concise message stating what changed and why.
5. Update the change tracker and relevant documentation.
6. Pause and request human instruction before continuing further work.

## Uncertainty & stop conditions
Stop and request human input if any of the following apply:
- Instructions conflict or are ambiguous.
- Required information is missing.
- Changes risk breaking compatibility or safety.
- There are security or compliance concerns.

## Agent contract
Must:
- Use a new branch per logical change / pull request.
- Commit frequently and atomically.
- Track every change in `.docs/CHANGE_TRACKER.md`.
- Respect scoped files and do not perform unrelated edits.
Must not:
- Modify the default branch.
- Hide or bundle unrelated changes.
- Skip tracking or rewrite history.
- Assume intent when requirements are unclear.

---

## Quick PR checklist
- New branch follows naming conventions (e.g., `feature/...`, `fix/...`).
- Prefer commit messages in the form: `<type>: <what> (why)`.
- Tests added/updated and pass locally.
- `CHANGE_TRACKER` entry added/updated.
- Documentation updated where behavior changed.
- No secrets or sensitive data included.
- No emojis in messages, comments, commit messages, or PR descriptions.
- Request human review and pause for approval before further changes.

If any rule cannot be followed, pause and request human instruction.
