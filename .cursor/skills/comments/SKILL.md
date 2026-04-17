---
name: comments
description: >-
  Adds or edits C# XML documentation for interfaces, classes, methods, public controller
  endpoints, properties, constructors, and enumerations using fixed summary prefixes, an
  80-character summary line rule (except constructors), optional remarks, and
  param/returns/exception tags on methods, endpoints, and constructors. Use when the user asks for XML
  comments, /// documentation, API docs, CS1591 fixes, or the comments skill in this repo.
---

# Comments (C# XML documentation)

## When to apply

Use this skill for **C#** `///` XML documentation in this solution: types, members, reducing **CS1591** warnings, and OpenAPI-visible summaries on public API surfaces.

## Canonical prompts (perform these)

Carry out the following tasks when documenting the codebase (or when the user invokes this skill without narrowing scope—apply each part that matches the requested surface, e.g. “only interfaces” means run the interface prompt only).

### Interfaces

Add xml comments to all interfaces, with a short, single line description that starts like: "This interface represents ..." followed by whatever the interface represents. If the description is longer than a single line (80 chars max) then add the complete description in the remarks section, and use a shorter summary for the interface description.

### Classes

Add xml comments to all the classes, with a short, single line description that starts like: "This class ..." followed by the description. If the description is longer than a single line (80 chars max) then add the complete description in the remarks section, and use a shorter summary for the class description.

Treat `public sealed record` and other reference `record` types the same as classes for this prompt unless the user says otherwise.

### Methods

Add xml comments to all methods, with a short, single line description that starts like: "This method ..." followed by the description. If the description is longer than a single line (80 chars max) then add the complete description in the remarks section, and use a shorter summary for the method description. Where applicable, Xml comments should include short parameter descriptions, a description of any return type, and a description of any exceptions that are thrown by the method.

Use these tags when applicable (omit lines that do not apply):

- `/// <param name="…">…</param>` — one per parameter; keep each line concise.
- `/// <returns>…</returns>` — when the return type is not `void` or when non-obvious side effects matter.
- `/// <exception cref="…">…</exception>` — for each **documented** exception type that callers should expect (thrown or propagated), including custom and BCL types.

For **public action methods** on ASP.NET Core **API controller** types (types derived from `ControllerBase` and used as request endpoints), use the **Public controller endpoints** prompt for `<summary>` wording instead of the `This method ` prefix. Still apply the **Methods** guidance for `<param>`, `<returns>`, and `<exception>` where applicable.

### Public controller endpoints

Add xml comments to **public** controller action methods (including `Task<IActionResult>` and `IActionResult` actions) with a short, single line description that starts like: `This endpoint ` followed by what the HTTP endpoint does (verb, resource, and main success or error shape when helpful). If the description is longer than a single line (**80 characters max** on the `<summary>` line), put the **complete** text in `<remarks>` and keep a shorter `<summary>` that still begins with `This endpoint `.

Treat route templates, HTTP verbs, and response codes as details that often belong in `<remarks>` or in `<returns>` rather than in an overlong `<summary>`.

### Properties

Add xml comments to **properties** that are in scope for the task (when the user does not narrow scope, document **`public`** and **`protected`** properties on those types, including compiler-synthesized properties from positional `record` parameters). Use a short, single line description that starts like: `This property ` followed by what the property holds or how callers should use it. If the description is longer than a single line (**80 characters max** on the `<summary>` line), put the **complete** text in `<remarks>` and keep a shorter `<summary>` that still begins with `This property `.

For **positional `record`** declarations, do **not** place `/// <summary>` inside the primary constructor parameter list (the compiler emits **CS1587**). Instead, document each primary constructor parameter on the **type** using `/// <param name="ParameterName">…</param>` immediately after the type’s `<summary>` / `<remarks>`, using the same `This property ` wording inside each `<param>` body.

### Constructors

Add xml comments to all constructors, with a short, single line description that starts like: "This constructor initializes a new instance of the {{class}} class" where {{class}} is the name of the class.

**Constructors are exempt from the 80-character summary rule:** use the full required sentence in `<summary>` even when it is longer than 80 characters. Optional `<remarks>` may add extra detail when helpful; they are not required for constructor length.

Where applicable, document constructor parameters with `/// <param name="…">…</param>` (one per parameter). Document exceptions the constructor **throws or propagates** using `/// <exception cref="…">…</exception>`—same expectations as in **Methods** (include `ArgumentNullException`, validation failures, `OperationCanceledException` when a `CancellationToken` parameter is honored, and any custom or BCL types callers should anticipate). Omit `<exception>` lines only when the constructor body does not throw or propagate anything beyond routine object initialization.

### Enumerations

Add xml comments to all enumerations, with a short, single line description that starts like: "This enumeration represents ..." followed by a short description of whatever the enumeration represents. If the description is longer than a single line (80 chars max) then add the complete description in the remarks section, and use a shorter summary for the description.

Document **`enum` types** with this prompt. Optionally document individual **`enum` members** with a one-line `/// <summary>` each when values are non-obvious; the user did not mandate a fixed prefix for members—use clear, short text.

## Summary line rule (80 characters)

- For **interfaces, classes, methods, enumerations, public controller endpoints, and properties**: measure the **text inside** `<summary>` on its single `///` line (the characters after `/// ` on that line)—keep it **≤ 80 characters**.
- If the true description does not fit, put the **complete** text in `<remarks>` (may span multiple `///` lines) and shorten `<summary>` while keeping the **required opening phrase** for that kind.
- **Constructors are exempt:** do not shorten constructor `<summary>` for length; use the full required constructor sentence (see Constructors prompt above).

## Opening phrases (quick reference)

| Kind | Summary opening |
|------|-----------------|
| `interface` | `This interface represents ` |
| `class`, `record`, nested type | `This class ` |
| Method (including operators, local functions only if user asks) | `This method ` |
| Public API controller action | `This endpoint ` |
| Property (`public` / `protected` in scope) | `This property ` |
| Constructor | `This constructor initializes a new instance of the {ClassName} class` (no 80-char limit on this summary) |
| `enum` | `This enumeration represents ` |

Use normal sentence case after the prefix where it fits (including after `This endpoint ` and `This property `). End summaries with appropriate punctuation when it reads naturally.

## Placement

- Put `/// <summary>…</summary>` (and optional `/// <remarks>…</remarks>`) **immediately above** the member.
- Put `/// <param>`, `/// <returns>`, `/// <exception>` **after** `<summary>` / `<remarks>` and **before** attributes, or **above** the member with attributes directly on the member—**above the whole attribute list** is preferred for readability.
- For `partial` types, prefer **one** class-level or interface-level summary on the primary declaration; avoid repeating identical XML on every partial unless the project already does.

## Style

- Prefer what the member **does** and **who consumes it** over repeating BCL names only.
- Match project tone: short, factual, minimal filler.
- Do not delete existing non-XML comments unless the user asks.

## Checklist before finishing

- [ ] Interfaces, classes, methods, public controller endpoints, properties, constructors, and enums (per scope) have summaries with the correct prefix.
- [ ] No non-constructor `<summary>` line exceeds 80 characters; overflow is in `<remarks>` with a shorter summary.
- [ ] Methods and public controller actions include `<param>`, `<returns>`, and `<exception>` where applicable.
- [ ] Constructor summaries use the full `This constructor initializes a new instance of the {ClassName} class` pattern (length exempt); constructors include `<param>` (when they have parameters) and `<exception>` for any thrown or propagated exception types callers should expect.
- [ ] Build succeeds; XML tags are well-formed.
