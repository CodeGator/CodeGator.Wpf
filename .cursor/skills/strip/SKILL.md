---
name: strip
description: >-
  Removes C# #region/#endregion pairs and decorative star-banner section comments
  (// *** lines plus // Section. titles). Use when the user asks to strip regions,
  remove IDE regions, clean section dividers, apply the strip skill, or match the
  CodeGator style cleanup for banner comments.
---

# Strip (regions and banner comments)

## When to apply

Use this skill when the user wants **less visual clutter** in C# sources by removing:

1. **`#region` / `#endregion`** directives (and the blank line immediately around them if it becomes redundant—keep normal spacing between members).
2. **Decorative “star banner” comments**: lines that are only `//` followed by asterisks, optionally wrapping a single **section title** line like `// Public methods.`

Do **not** use this skill to delete ordinary `//` comments, `///` XML documentation, or `/* */` block comments unless the user explicitly expands scope.

## What to remove

### Regions

- Remove every line that matches `^\s*#region\b.*$` and its matching `^\s*#endregion\b.*$` in the same type/file (pair by nesting order if multiple regions exist in one type).
- Typical pattern in this codebase was:

```csharp
    #region Public methods

    /// <summary>
```

After strip, the `#region` line and the blank line under it are removed so `/// <summary>` follows the previous block or section naturally.

### Star banners

**A. Full three-line banner** (remove all three lines when they appear together):

```csharp
    // *******************************************************************
    // Public methods.
    // *******************************************************************
```

The middle line uses one of these **exact titles** (case and trailing dot as shown) in projects that followed this convention:

- `// Fields.`
- `// Public methods.`
- `// Properties.`
- `// Constructors.`
- `// Stream overrides.`

**B. Standalone separator** (single line—often placed between methods):

```csharp
    // *******************************************************************
```

**Star line rule:** treat a line as a star banner line if, after optional leading whitespace and `//`, the remainder is **only** `*` characters (length may vary; some files used `// ******************************************************************` with a different count—still match “only asterisks”).

**Section title line rule:** remove a line if it matches (indent may vary; use `\s*`):

```text
^\s*// (Fields|Public methods|Properties|Constructors|Stream overrides)\.\s*$
```

Remove section title lines **only** when they are part of this cleanup (they existed as the middle of the banner). If a line like `// Public methods.` appears in a different context, use judgment; in CodeGator-style extension files it was always the banner middle.

## What to keep

- All executable code and attributes.
- **`///` XML doc comments** and all non-banner `//` comments (attribution URLs, inline explanations, etc.).
- `#pragma`, `#if`, and other preprocessor directives except `#region` / `#endregion`.

## Procedure for the agent

1. **Discover scope:** default to `*.cs` under the paths the user names; if unspecified, ask or limit to `src/` (or the main library folder) so tests and generated code are not changed unintentionally.
2. **Regions:** remove `#region` / `#endregion` lines; preserve one blank line between logical members if the file already used that style.
3. **Banners:** remove lines matching the star-only and section-title patterns above (process file line-by-line or with a small script—see below).
4. **Verify:** run `dotnet build` on the relevant solution or project and fix any accidental damage (e.g. merged lines).
5. **Do not** reformat unrelated code, rename symbols, or remove non-banner comments.

## Reference script (optional)

Line-oriented removal is easy to verify in a review. Example for **one directory** of `.cs` files (adjust `root`); run from the agent or locally after backup:

```python
import re
from pathlib import Path

root = Path("src/CodeGator")  # change as needed
star_re = re.compile(r"^\s*// \*+\s*$")
section_re = re.compile(
    r"^\s*// (Fields|Public methods|Properties|Constructors|Stream overrides)\.\s*$"
)

for path in sorted(root.rglob("*.cs")):
    raw = path.read_text(encoding="utf-8")
    lines = raw.splitlines(keepends=True)
    out = []
    for line in lines:
        bare = line.rstrip("\r\n")
        if bare.strip().startswith("#region") or bare.strip() == "#endregion":
            continue
        if star_re.match(bare) or section_re.match(bare):
            continue
        out.append(line)
    new = "".join(out)
    if new != raw:
        path.write_text(new, encoding="utf-8", newline="\n")
```

**Regions in the script:** the snippet above only handles star and section lines; **`#region` / `#endregion` are often removed first** with structured search-replace in the editor so pairing stays obvious.

## Checklist before finishing

- [ ] No remaining `#region` / `#endregion` in the chosen scope (confirm with search).
- [ ] No remaining long `// ***…`-only lines used as section dividers.
- [ ] No erroneous removal of `///` docs or meaningful `//` comments.
- [ ] `dotnet build` succeeds for the affected solution or project.
