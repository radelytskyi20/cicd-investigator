# Git workflow

Use main as the reviewed integration branch. Work on one task/Txx-xx branch at a time; the first repository-preparation task may use that branch as its initial branch. Start later branches from the reviewed baseline. Do not create a remote unless its owner/address are supplied.

Inspect git status before editing and preserve unrelated work. Before commit, inspect the working diff, select task-related paths, and inspect the staged diff. Never stage secrets, SDK session files, build output or accidental local settings. Use focused English commit messages such as docs: prepare repository for codex development.

Run the task's relevant checks before a completion commit. If credentials, tools or environment block a check, state that explicitly and keep the task Blocked or Ready for review with the limitation; do not mark it Done. If Git identity is missing, report the blocker rather than editing global identity silently.

After implementation, update affected documentation and the task completion section in the same change. The coding agent sets Ready for review and reports the commit hash in chat. Avoid a second commit solely to write its own hash into the task file. The owner reviews the diff and behavior, accepts Done, and decides when to merge.

No automatic push, merge, force-push, reset of unrelated changes or history rewriting. Do not amend commits from completed/reviewed tasks without an explicit request. Do not start the next task unless assigned. A task can contain more than one commit if corrections are needed, but keep the history understandable and scope focused.
