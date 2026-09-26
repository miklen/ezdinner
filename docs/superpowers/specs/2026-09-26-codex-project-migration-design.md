# Codex Project Migration Design

## Intent

Make Codex the primary coding agent for EzDinner without losing the project knowledge, reusable workflows, or specialist review gates that existed in the Claude setup. Preserve `.claude/` unchanged as a rollback reference while giving Codex native, discoverable configuration.

Success means a fresh Codex session in this repository receives the project rules from `AGENTS.md`, discovers the project-specific skills, can run the maintained OpenSpec workflows, and is reminded to use focused backend or frontend review subagents after relevant edits.

## Current State

- `AGENTS.md` contains only `@CLAUDE.md`. Codex passes that text into the prompt but does not expand the referenced file.
- The complete project guide is in `CLAUDE.md`.
- Custom workflows and reference skills live under `.claude/skills/`; Codex does not discover that directory as project skills.
- Seven Vue-related Claude skills are byte-identical to skills already installed in the user's global `.agents/skills/` directory.
- Ten other project skills are not currently available to Codex.
- OpenSpec 1.5.0 is installed globally. The current release is 1.13.2 and supports generating Codex workflows through its CLI.
- Backend and frontend reviewer definitions exist under `.claude/agents/`, but Codex does not consume Claude agent definitions directly.
- The Claude review hook scripts exist but are not registered in `.claude/settings.json`.
- Claude permission allowlists are specific to Claude Code and do not map directly to Codex sandbox policy.

## Migration Strategy

### Preserve the Claude setup

Leave `CLAUDE.md` and `.claude/` unchanged. They remain a rollback reference and make the migration non-destructive. Codex-native files become the maintained path going forward.

### Make `AGENTS.md` the Codex entrypoint

Replace the ineffective import-only file with a concise Codex guide that contains:

- the project overview and universal product constraints;
- contextual routing for backend, frontend, routing, debugging, and testing skills;
- the canonical build, test, and lint commands;
- instructions for OpenSpec workflow invocation;
- instructions to delegate backend and frontend review to focused subagents after relevant changes;
- links to detailed project guidance where loading it is contextually useful.

Avoid copying the entire 16 KB Claude guide into every Codex prompt. Keep always-on rules in `AGENTS.md` and move or reference detailed, task-specific gotchas through project skills so they are loaded only when relevant.

### Let OpenSpec generate its Codex integration

OpenSpec owns its workflow files. Do not manually copy or edit the four `.claude/skills/openspec-*` skills or the `.claude/commands/opsx` commands.

The migration sequence is:

1. Upgrade the global `@fission-ai/openspec` package from 1.5.0 to the current release.
2. Run the upgraded CLI's project initialization flow with Codex selected (`openspec init --tools codex .`, adjusted only if the upgraded CLI reports a different current syntax).
3. Review the generated diff and preserve existing `openspec/` specifications and archived changes.
4. Use the generated Codex workflows as the sole OpenSpec integration.
5. Run the CLI's validation/status commands to verify the existing OpenSpec store remains intact.

The package upgrade changes user-level tooling and therefore requires an approval at execution time. Generated project files remain inside the repository.

### Port only project-owned skills

Create Codex skills under `.agents/skills/` for the project-owned workflows that are not supplied by OpenSpec and are not already installed globally:

- `data-visualization`
- `questions-are-not-instructions`
- `software-design-principles`
- `tactical-ddd`
- `technical-investigator`
- `writing-tests`

Preserve the useful behavior of each skill while adapting Claude-specific wording or tool assumptions. Do not duplicate the globally installed Vue skills or `create-adaptable-composable`; `AGENTS.md` can continue to route to them by name.

### Convert specialist agents into Codex review skills

Create focused `backend-ddd-reviewer` and `frontend-vue-reviewer` skills from the existing Claude agent procedures. Remove Claude-only frontmatter such as fixed Haiku model selection and tool allowlists. Preserve the review scope, project-specific rules, line-referenced findings, and pass/fail output contract.

`AGENTS.md` will tell the coordinating Codex agent to spawn a focused subagent for each applicable review and instruct that subagent to use the corresponding reviewer skill. The coordinator remains responsible for applying or reporting review findings.

Do not retain the old persistent-agent-memory instruction. Repository knowledge belongs in reviewed project files, not opaque agent memory.

### Add a native Codex review hook

Add `.codex/hooks.json` and a Windows-compatible PowerShell hook script.

The hook will:

- record the initial state of relevant `api/` and `web/` files at session start;
- detect relevant changes when the root turn attempts to stop;
- issue one continuation request naming the required reviewer skill and subagent flow;
- avoid repeating indefinitely by recording that the reminder was issued for the session;
- remain advisory reinforcement rather than claiming to prove that a review occurred.

Codex requires repository hooks to be reviewed and trusted by the user through `/hooks`. This one-time trust action will be included in the handoff.

### Keep permissions in Codex's native model

Do not translate `.claude/settings*.json` command-by-command. Codex already applies its sandbox and approval policy, and the repository is trusted in the user's Codex configuration. Build and test commands remain documented in `AGENTS.md`; commands requiring additional access use Codex's normal approval flow.

User-level credentials and unrelated global configuration are outside this repository migration. The plaintext AgentMail credential observed in the user Codex config should be moved and rotated separately.

## Validation

Validation must prove both discovery and behavior:

1. Run the skill validator against every new or adapted project skill.
2. Use `codex debug prompt-input` from the repository to confirm that the full `AGENTS.md` content and project skill metadata are model-visible.
3. Confirm OpenSpec-generated Codex skills are present and no manually maintained duplicate OpenSpec workflows were added.
4. Run `openspec --version`, `openspec status`, and the appropriate OpenSpec validation command after initialization.
5. Exercise the hook script with synthetic session-start and stop payloads for no changes, backend-only changes, frontend-only changes, and both areas changed.
6. Run one backend and one frontend review scenario through fresh subagents, verifying that each uses the correct skill and produces the required report shape.
7. Run `codex doctor --summary` and inspect the final repository diff.

## Rollback

- Delete the generated `.agents/` and `.codex/` project additions and restore the previous `AGENTS.md` to return to the pre-migration repository state.
- `.claude/` and `CLAUDE.md` remain untouched throughout.
- If the global OpenSpec upgrade causes a regression, reinstall `@fission-ai/openspec@1.5.0`; repository specifications and archives are not rewritten during package installation.

## Out of Scope

- Changing application behavior or production code.
- Rewriting existing OpenSpec specifications or archived changes.
- Porting Claude's historical permission allowlist into Codex.
- Editing user-level Codex configuration or credentials.
- Removing the Claude setup after migration.
