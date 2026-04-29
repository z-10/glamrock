---
title: "Modules \U0001F48E"
layout: docs
examples: /examples/13-modules/
nav_order: "1012"
summary: "GlamRock extension: split your programs across files with channel and light."
---
> **GlamRock Extension** 🎸 This feature is a GlamRock extension and is not part of standard Rockstar.

## Modules

As any band manager will tell you, the secret to a great show is knowing who's on stage and who's backstage. GlamRock's module system lets you split your programs across multiple `.rock` files, control what's visible, and reuse code like a greatest hits compilation.

### Exporting with `light`

The `light` keyword marks variables or functions as exported. Aliases: `ignite`, `shine`, `beacon`. You can light multiple exports in one statement using `,`, `&`, or `and`.

{% rockstar_include math_module.rock %}

### Importing with `channel`

The `channel` keyword imports a module. Alias: `bring`.

#### Import all exports

{% rockstar_include featuring-without-alias.rock %}

#### Selective import with `'s`

Use `channel Module's Export` to import only specific symbols:

{% rockstar_include featuring-with-alias.rock %}

#### Selective import with `from`

You can also write it the other way around — `Channel Add from Math Module` is equivalent to `Channel Math Module's Add`.

### Module isolation

Modules execute in their own isolated environment. Variables not marked with `light` cannot be imported — attempting to do so throws an error.

### How modules work

* **Module names**: Follow variable naming conventions. `Channel Divine Math` resolves to `divine_math.rock`.
* **Caching**: Each module is parsed and executed only once.
* **Circular imports**: Detected and rejected with an error.
* **Isolation**: Modules run in their own scope. No global leakage.
