# Codex Dash

Double-click **Codex Dash.exe**. A portable Windows dashboard inspired by Codex Meter's dark green interface. No installer, API key or internet connection needed; uses Windows .NET Framework.

Filter by Folders, Apps, Websites, Commands or Overview, search records, select a row for its scope and source, and click Refresh to reload.

Version 1.1 overview update: access records are paginated with **<** and **>**, page number and result count. Choose 10, 20 or 50 records per page. Search includes names, status, category, source and explanatory details. Filter attention needed (priority at least 7), rated records or unrated records. Sort by priority, name, category, suggested score, your score or average. Missing personal scores and averages sort last. **Wis filters** clears the selection. The top three always reflect all matching records, independent of the current page or sort order; clicking a card searches for that record. These access logs are saved configuration records, not a chronological activity audit.

Priority columns: **Mijn score** is a fixed suggested attention score (1–10), **Jouw score** is your editable whole-number score (1–10; clear to remove), and **Gemiddelde** is the arithmetic mean of both. Higher means more attention. Until you rate an item, its average stays blank and the suggested score is used provisionally for ranking. Scores save beside the executable in `priority-scores.json`. Codex settings remain untouched. The top three of the current filtered list appear as numbered cards; click a card to select its score cell.

Suggested scores prioritize unknown/unparsed records and broad configured access over disabled or denied entries. These are transparent fixed heuristics, not a live AI assessment or a verified security risk score. Equal scores are ordered by name, then stable record identity. Command scores remain attached to their prefix when rule line numbers change; command arguments are hashed and never written into the score file.

Reads `config.toml`, `.codex-global-state.json` (only recorded chat writable roots), and `rules/*.rules` from `CODEX_HOME` or `%USERPROFILE%\.codex`. Never changes Codex permissions. Does not read authentication files, browser history, chat contents or account tokens. Command arguments and MCP server URLs are hidden.

This is a saved-record viewer, not an effective permissions evaluator. Trusted projects are not permanent write grants. Saved chat roots may be historical. Enabled plugins do not prove connected account access. Website access, browser grants, remote connectors, one-time approvals, organization restrictions and active chat overrides cannot be fully verified from these sources. Unsupported rules and unreadable files are labelled explicitly. The lightweight config reader supports single-line standard settings; multiline TOML arrays and complex profiles are not fully resolved.

Manage actual permissions in Codex Settings and the chat permission selector. See https://learn.chatgpt.com/docs/sandboxing.

Build: run `build.ps1` with PowerShell. Diagnostic: run `Codex Dash.exe --check`, which writes category counts to `check.txt` beside the executable.
