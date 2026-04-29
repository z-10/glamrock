---
title: "Modules \U0001F48E"
layout: docs
examples: /examples/13-modules/
nav_order: "1012"
summary: "GlamRock extension: split your programs across files with featuring and spotlight."
---
> **GlamRock Extension** 🎸 This feature is a GlamRock extension and is not part of standard Rockstar.

## Modules

As any band manager will tell you, the secret to a great show is knowing who's on stage and who's backstage. GlamRock's module system lets you split your programs across multiple `.rock` files, control what's visible, and reuse code like a greatest hits compilation.

### Exporting with `spotlight`

The `spotlight` keyword marks a variable or function as exported. Only spotlighted symbols are visible to other files — everything else stays backstage.

{% rockstar_include math-module.rock %}

In this example, `The Pi`, `Add`, and `Multiply` are in the spotlight. `The Secret` stays hidden.

### Importing with `featuring`

The `featuring` keyword imports a module, just like a featured artist on a track.

#### Namespace imports

Use `featuring ... as ...` to import a module's exports into a named namespace. Access members using `at` with the export name:

{% rockstar_include featuring-with-alias.rock %}

#### Direct imports

Use `featuring` without `as` to import all exported symbols directly into the current scope:

{% rockstar_include featuring-without-alias.rock %}

> Direct imports are convenient but can cause name collisions. Namespace imports are recommended for larger programs.

### Module isolation

Modules execute in their own isolated environment. Non-spotlighted variables are completely invisible to the importer:

{% rockstar_include module-isolation.rock %}

`The Answer` was spotlighted, so it's accessible. `The Hidden` was not, so it returns `mysterious`.

### How modules work

* **File resolution**: Paths are relative to the importing file. The `.rock` extension is optional, so `Featuring "math"` and `Featuring "math.rock"` are equivalent.
* **Caching**: Each module is parsed and executed only once, no matter how many times it's imported.
* **Circular imports**: If module A imports module B which imports module A, GlamRock will throw an error rather than loop forever.
* **Isolation**: Modules run in their own scope. Global variables in a module do not leak into the importer.

### Namespace member access

When you import a module with an alias, the exports are stored as a collection with string keys. Use `at` to access them:

```
Featuring "math.rock" as Math

(Access a variable)
Shout Math at "The Pi"

(Access a function — assign it to a local name, then call it)
Let Multiply be Math at "Multiply"
Let The result be Multiply taking 5, 5
Shout The result
```

Export names are case-insensitive, so `Math at "the pi"`, `Math at "The Pi"`, and `Math at "THE PI"` all work.
