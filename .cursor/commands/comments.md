# Comments

Read and follow the project skill at **`.cursor/skills/comments/SKILL.md`** for this entire task. Treat that file as the source of truth for wording, prefixes, the 80-character summary rule (and constructor exemption), `<remarks>`, and method XML tags (`<param>`, `<returns>`, `<exception>`).

## What to do

1. Open `.cursor/skills/comments/SKILL.md` and apply every **Canonical prompt** section that matches the user’s scope (if they did not narrow scope, apply all: interfaces, classes, methods, constructors, enumerations).
2. Work in **this repository’s C#** sources unless the user specifies paths or projects.
3. After edits, run a **build** (or the smallest relevant `dotnet build`) and fix any issues you introduce.

## If the user added extra instructions in chat

Combine those instructions with the skill; the skill wins on conflicts about summary prefixes and the 80-character rule unless the user explicitly overrides for this request.
