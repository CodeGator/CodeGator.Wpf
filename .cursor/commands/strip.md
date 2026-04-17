# Strip

Read and follow the project skill at **`.cursor/skills/strip/SKILL.md`** for this entire task. Treat that file as the source of truth for which lines to remove, what to keep, and how to verify.

## What to do

1. Open `.cursor/skills/strip/SKILL.md` and apply its rules for the user’s scope (paths, projects, or whole `src/` when they did not specify—see the skill’s **Discover scope** step).
2. Remove **`#region` / `#endregion`** pairs and **star-banner** comments (star-only `//` lines and the fixed **section title** middle lines listed in the skill).
3. Do **not** remove ordinary `//` comments, `///` XML documentation, or `/* */` blocks unless the user explicitly asks to widen scope.
4. After edits, run **`dotnet build`** on the relevant solution or project and fix any issues you introduce.

## If the user added extra instructions in chat

Combine those instructions with the skill; the skill wins on conflicts about which comment patterns count as banners or regions unless the user explicitly overrides for this request.
