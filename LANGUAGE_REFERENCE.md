# GlamRock Language Reference

This reference summarizes the GlamRock/Rockstar language surface implemented in this repository. It is based on the Starship engine grammar in `Starship/Rockstar.Engine/rockstar.peg`, the C# runtime behavior, and the examples in `glamrock.dev/docs` and `Starship/Rockstar.Test/programs`.

## Syntax Basics

GlamRock is case-insensitive for keywords and most variables. Statements are separated by newlines or punctuation: `.`, `?`, `!`, `;`, `:`. Whitespace is significant between most keywords and operators.

Comments:

```rockstar
# This is a line comment
(This is a block comment)
{ChordPro-style comment}
[Another ChordPro-style comment]
```

Blocks are ended by a blank line, EOF, `end`, `oh`, `yeah`, `baby`, or an `oooh`-style closer. `oh` closes one block, `ooh` closes two, `oooh` closes three, and so on.

## Types and Values

| Type | Literals and aliases | Notes |
| --- | --- | --- |
| Number | `123`, `-12.5`, `.5` | Backed by .NET `decimal`; supports signed integers and decimals. |
| String | `"hello"` | Double-quoted strings. Rockstar has no backslash escape sequences. |
| Boolean | `true`, `yes`, `ok`, `right`; `false`, `no`, `wrong`, `lies` | Boolean values also participate in numeric coercion. |
| Null | `null`, `nothing`, `nowhere`, `nobody`, `gone` | Falsy and numerically equivalent to zero. |
| Empty string | `empty`, `silent`, `silence` | Equivalent to `""`. |
| Mysterious | `mysterious` | Default value for unassigned or missing values. |
| Function | Created with `takes` / `wants` | Callable with `taking` or `call`. |
| Array | Created by indexed assignment or `rock` / `push` | Contains both list-style numeric indexes and hash-style arbitrary keys. |

Falsy values are `false`, `null`, `mysterious`, `empty` / `""`, and `0`. Everything else is truthy.

## Variables

Simple variables are identifiers that are not keywords:

```rockstar
Tommy is 1
Whisper Tommy
```

Common variables begin with an article or possessive prefix:

```rockstar
The fire is 10
My heart is true
Your scream is "loud"
```

Common variable prefixes are `a`, `an`, `the`, `my`, `your`, `our`, `their`, `his`, and `her`. The parser restricts `her <keyword>` because `her` can also be a pronoun.

Proper variables are multi-word proper names whose words start with uppercase letters:

```rockstar
Doctor Feelgood is 42
Tom Sawyer is "free"
```

Pronouns refer to the current pronoun subject:

```rockstar
My heart is 100
Whisper it
```

Pronouns are `i`, `me`, `you`, `he`, `him`, `she`, `her`, `it`, `they`, `them`, `ze`, `hir`, `zie`, `zir`, `xe`, `xem`, `ve`, and `ver`.

The built-in `arguments`, `the world`, and `the outside` variable forms refer to command-line arguments.

## Assignment and Declaration

```rockstar
My heart is 100
Put 100 into my heart
Let my heart be 100
Let my heart = 100
My heart = 100
Let my heart
```

`let` declares a variable in the current scope. Assigning without `let` writes to an existing visible binding or creates a broader-scope binding according to the interpreter's scoping rules.

Aliases for `is` are `was`, `are`, `were`, `am`, `'s`, and `'re`.

`is` has compatibility behavior for poetic numbers: if the right-hand side does not start with a keyword or literal, it may be parsed as a poetic number rather than a variable expression. Prefer `let x be y`, `put y into x`, or `x = y` when assigning from another variable.

Compound assignment forms:

```rockstar
My heart is with 5
My heart is minus 2
Let my heart be times 3
```

These combine the current value of the left-hand variable with an expression list using `plus`, `minus`, `times`, or `divided by`.

## Literals

Number literals:

```rockstar
The count is 123
The ratio is -12.5
The half is .5
```

String literals:

```rockstar
The words are "hello world"
```

Poetic numbers use word lengths as digits. Use `like` or `so` for explicit poetic numbers:

```rockstar
My dream is like ice cold beer
```

Each word length becomes a digit modulo 10. Use three dots or Unicode ellipsis (`U+2026`) as a decimal separator. Legacy `x is <poetic text>` remains supported for compatibility.

Poetic strings use `says`, `say`, or `said` and capture the rest of the line:

```rockstar
The chorus says We are the champions
```

Ninja strings append Unicode code points using `holds` / `hold`:

```rockstar
The message holds 72
The message holds 105
```

## Operators

Arithmetic:

| Operation | Symbols and aliases |
| --- | --- |
| Addition | `+`, `plus`, `with` |
| Subtraction | `-`, `minus`, `without` |
| Multiplication | `*`, `times`, `of` |
| Division | `/`, `divided by`, `between`, `over` |

Boolean logic:

| Operation | Syntax |
| --- | --- |
| And | `and` |
| Or | `or` |
| Nor | `nor` |
| Not | `not`, `non`, `non-` |

Equality and identity:

| Operation | Syntax |
| --- | --- |
| Equality | `is`, `was`, `are`, `were`, `am`, `'s`, `'re`, `=` |
| Inequality | `is not`, `isn't`, `isnt`, `ain't`, `aint`, `wasn't`, `wasnt`, `aren't`, `arent`, `weren't`, `werent`, `!=` |
| Strict identity | `is exactly`, `is totally`, `is really` |
| Strict non-identity | `isn't exactly`, `isn't totally`, `isn't really` |

Comparison:

| Operation | Symbols and aliases |
| --- | --- |
| Greater than | `>`, `is above`, `is over`, `is greater than`, `is higher than`, `is bigger than`, `is stronger than`, `is more than` |
| Less than | `<`, `is under`, `is below`, `is less than`, `is lower than`, `is smaller than`, `is weaker than` |
| Greater or equal | `>=`, `is as great as`, `is as high as`, `is as big as`, `is as strong as` |
| Less or equal | `<=`, `is as less as`, `is as low as`, `is as small as`, `is as weak as` |

Precedence, from high to low:

1. Primary expressions: literals, lookups, function calls, dequeues.
2. Multiplication and division.
3. Addition and subtraction.
4. Comparisons.
5. Unary `not` / `non`.
6. Equality and inequality.
7. `and`.
8. `nor`.
9. `or`.

Expressions do not use parentheses. Split complex logic into intermediate assignments if precedence is not what you need.

## Arithmetic Behavior

Numbers use numeric arithmetic. Booleans and null can coerce to numeric values: `true` is `1`, `false` and `null` are `0`.

Adding strings concatenates string representations. Subtracting strings removes occurrences. Multiplying strings can repeat, reverse, or slice depending on the numeric operand. Dividing strings supports substring-like behavior and occurrence counts. Operations involving `mysterious` generally produce `mysterious`.

Arrays have custom arithmetic: adding most values to an array appends; adding numeric values can use array length in numeric contexts; subtracting arrays removes matching elements.

## Lists

Variable lists are used in function definitions and contain only variables. Separators include `and`, `,`, `&`, `, and`, `n'`, and `'n'`.

Primary lists are used in function calls and compound arithmetic. They contain primary expressions only and use `,`, `&`, `n'`, or `'n'`.

Expression lists are used by compound operators and array pushing. They contain full expressions and support the Oxford comma `, and`.

```rockstar
My function takes X, Y and Z
Shout My function taking 1, 2, 3
Rock the list with 1, 2, and 3
```

## Input and Output

Output:

```rockstar
Print "hello"
Shout "hello"
Say "hello"
Scream "hello"
Whisper "hello"
Write "hello"
```

`print`, `shout`, `say`, `scream`, and `whisper` output with a newline. `write` outputs without adding a newline.

Input:

```rockstar
Listen
Listen to the answer
```

`listen to <variable>` stores a line of input. `listen` reads and returns input without assigning it directly.

Debugging:

```rockstar
Debug the value
@dump
```

`debug` outputs a diagnostic expression. `@dump` prints current memory state with object IDs.

## Conditionals

```rockstar
If the fire is hot
  Shout "burning"
Else
  Shout "cold"
Yeah
```

Aliases:

| Concept | Syntax |
| --- | --- |
| If | `if`, `when` |
| Optional then | `then` |
| Else | `else`, `otherwise` |

One-line conditionals are allowed:

```rockstar
If the fire is hot shout "burning"
```

## Loops and Flow Control

Conditional loops:

```rockstar
While the count is lower than 10
  Shout the count
  Build the count up
Yeah

Until the count is 10
  Build the count up
Yeah
```

For loops:

```rockstar
For item in the array
  Shout item
Yeah

For value and index in the array
  Shout index with ": " with value
Yeah

For value of the hash
  Shout value
Yeah

For value and key of the hash
  Shout key
Yeah
```

`for <variable> in <expression>` iterates list-style values: strings, numeric ranges, and array list elements. `for <variable> of <expression>` iterates hash-style array entries. Add `every` to bind loop variables as common variables inside the body:

```rockstar
For every star in the sky
  Whisper the star
Yeah
```

Flow control:

```rockstar
Break
Break my heart
Continue
Take it from the top
Exit
```

`continue` has alias `take`. `break` and `continue` accept wildcard lyric text through the end of the statement.

## Functions

Define functions with `takes` or `wants`:

```rockstar
Add takes X and Y
Give back X with Y
```

No-argument functions use `takes null`:

```rockstar
The clock takes null
Give back "now"
```

Return syntax:

```rockstar
Return X
Return X back
Give X
Give back X
Giving X
Send X
```

Call functions with `taking` when passing arguments:

```rockstar
Shout Add taking 3, 4
```

Call no-argument functions with `call`:

```rockstar
Call The clock
Call The clock into the time
```

General call statement forms:

```rockstar
Call Function
Call Function into Target
Call Function with Arg, OtherArg
Call Function with Arg, OtherArg into Target
```

Function arguments must be primary expressions. If you need `1 + 2` as an argument, assign it first.

Functions create scope and may be nested. Nested functions can close over variables from their creation environment.

## Arrays and Indexing

Index with `at`:

```rockstar
The list at 0 is "first"
The list at 1 is "second"
Shout the list at 0
```

Indexes may be strings, numbers, or arithmetic expressions. Logical expressions are not allowed as indexes. Missing list values read as `mysterious`; numeric list gaps are initialized as `null`.

Arrays can be created or appended with `rock` / `push`:

```rockstar
Rock the list
Rock the list with 1, 2, 3
Push 4 into the list
Push the list using 5, 6
```

Supported push/enlist forms:

```rockstar
Rock the list
Rock the list with value
Rock the list using value, other
Rock value into the list
Push the list
Push the list using value, other
Push value into the list
```

Dequeuing:

```rockstar
Roll the list
Roll the list into the first
Pop the list
Pop the list into the first
```

`roll` and `pop` remove and return the first list element. They can be used as expressions or statements.

Strings and numbers also support indexing. Indexed string assignment can modify characters.

## Increment and Decrement

```rockstar
Build the count up
Build the count up, up
Knock the count down
Knock the count down down
```

Each `up` increments by one. Each `down` decrements by one.

## Mutations and Conversions

Mutations can act in place or write into a target:

```rockstar
Split the string
Split the string into the parts
Split the string using ","
Split the string into the parts using ","
Split the string using "," into the parts
```

Mutation operators:

| Operation | Aliases | Behavior |
| --- | --- | --- |
| Split | `split`, `cut`, `shatter` | Split strings into arrays. Optional modifier is the delimiter. |
| Join | `join`, `unite`, `gather` | Join arrays into strings. Optional modifier is the separator. |
| Cast | `cast`, `burn` | Parse strings as numbers, or convert numeric code points to characters. |

Rounding and casing:

```rockstar
Turn up the number
Turn the number up
Turn down the number
Turn the number down
Turn around the number
Turn the number around
```

For numbers, `turn up` rounds up, `turn down` rounds down, and `turn around` / `turn round` rounds to nearest. For strings, `turn up` uppercases, `turn down` lowercases, and `turn around` reverses.

## Albums, Modules, and Tracklists

GlamRock uses a unified album loader. A load name maps to a lowercase underscore path, so `Math Module` resolves as `math_module`. The loader then merges any matching sources in this order: built-in album, `.rock` module, `.tracklist` file. Later sources override earlier ones on name collisions, so `.rock` exports override built-ins and `.tracklist` tracks override both.

There is no separate command family for "modules" versus "tracklists". The commands differ by import style, not by what they are allowed to load.

Load all exports into the current scope:

```rockstar
Channel Math Module
Bring Math Module
Conjure Gates of Heaven

Know Math Module
Invoke GDI Wrapper
Divine Gates of Heaven
```

`channel`, `bring`, and `conjure` can also do selective imports. `invoke`, `know`, and `divine` always import the full merged export set.

Export values from a `.rock` module with `light`, `ignite`, `shine`, or `beacon`:

```rockstar
The Pi is 3.14159
Add takes X and Y
Give back X with Y

Light The Pi and Add
```

Import selected exports:

```rockstar
Channel Math Module's Add and Multiply
Channel Add from Math Module
```

Use module-qualified lookups and calls after channeling:

```rockstar
Channel Math Module
Shout The Pi from Math Module
Shout Add from Math Module taking 3, 4
```

Scoped channeling temporarily imports a module only for a block or one statement:

```rockstar
Channeling Math Module
  Shout Add taking 3, 4
Yeah

Bringing Math Module shout Multiply taking 5, 5
```

`.rock` modules execute in isolated environments. Only exported names are visible to importers. Imports are cached, circular imports are rejected, and missing albums throw file-not-found errors.

Tracklists describe native interop albums or mixtapes:

```text
ALBUM gdiplus.dll

TRACK Create Image FEATURING GdipCreateBitmapFromFile
  TAKES string, sigil
  GIVES number

TRACK Dispose FEATURING GdipDisposeImage
  TAKES mysterious
  GIVES nothing
```

Use `ALBUM <native-library>` or `MIXTAPE <managed-library>`. Each `TRACK` maps a GlamRock name to a native function name with optional `TAKES` and `GIVES` lines. Supported tracklist types are `number`, `string`, `boolean`, `nothing` / `null`, and `mysterious`; umlaut spellings are also accepted by the parser. `sigil` is an out parameter.

Built-in albums are registered by the engine and load through the same commands:

| Album | Tracks |
| --- | --- |
| `Gates of Heaven` | `Command The Heavens` |
| `Tome of Power` | `Open The Tome`, `Read The Tome`, `Read The Line`, `Write The Tome`, `Write The Line`, `Seek The Tome`, `Tell The Tome`, `Tome Exhausted`, `Seal The Tome` |

`Command The Heavens` executes a shell command and returns an array `[stdout, stderr, exitCode]`:

```rockstar
Know Gates of Heaven
Let my result be Command The Heavens taking "echo hello"
Shout my result at 0
```

`Tome of Power` provides handle-based file I/O. File paths are resolved relative to the current source file when possible:

```rockstar
Know Tome of Power
Let the tome be Open The Tome taking "notes.txt", "w+"
Write The Tome taking the tome, "fire and thunder"
Seek The Tome taking the tome, 0, 0
Shout Read The Tome taking the tome
Seal The Tome taking the tome
```

## Keyword and Alias Index

| Concept | Keywords and aliases |
| --- | --- |
| Addition | `plus`, `with`, `+` |
| Arguments | `arguments`, `the world`, `the outside` |
| Array push | `rock`, `push` |
| Array pop | `roll`, `pop` |
| Block end | `end`, `oh`, `yeah`, `baby`, `ooh`, `oooh`, etc. |
| Break | `break` |
| Cast | `cast`, `burn` |
| Comments | `#`, `(...)`, `{...}`, `[...]` |
| Continue | `continue`, `take` |
| Debug | `debug`, `@dump` |
| Division | `divided by`, `between`, `over`, `/` |
| Else | `else`, `otherwise` |
| Empty string | `empty`, `silent`, `silence` |
| Equality | `is`, `was`, `are`, `were`, `am`, `'s`, `'re`, `=` |
| Exact identity | `exactly`, `totally`, `really` |
| Exit | `exit` |
| False | `false`, `lies`, `no`, `wrong` |
| For | `for`, `every`, `in`, `of` |
| Function call | `taking`, `call`, `with`, `into` |
| Function definition | `takes`, `wants` |
| Greater | `above`, `over`, `greater`, `higher`, `bigger`, `stronger`, `more` |
| If | `if`, `when`, `then` |
| Load/import album, module, or tracklist | `channel`, `bring`, `conjure`, `invoke`, `know`, `divine`, `channeling`, `bringing`, `from` |
| Index | `at` |
| Input | `listen`, `to` |
| Join | `join`, `unite`, `gather` |
| Less | `under`, `below`, `less`, `lower`, `smaller`, `weaker` |
| Module export | `light`, `ignite`, `shine`, `beacon` |
| Multiplication | `times`, `of`, `*` |
| Native interop tracklist declarations | `ALBUM`, `MIXTAPE`, `TRACK`, `FEATURING`, `TAKES`, `GIVES`, `sigil` |
| Not | `not`, `non`, `non-` |
| Null | `null`, `nothing`, `nowhere`, `nobody`, `gone` |
| Output newline | `print`, `shout`, `say`, `scream`, `whisper` |
| Output raw | `write` |
| Poetic number marker | `like`, `so` |
| Poetic string marker | `say`, `says`, `said` |
| Pronouns | `i`, `me`, `you`, `he`, `him`, `she`, `her`, `it`, `they`, `them`, `ze`, `hir`, `zie`, `zir`, `xe`, `xem`, `ve`, `ver` |
| Return | `return`, `giving`, `give`, `send`, optional `back` |
| Rounding/case | `turn`, `up`, `down`, `around`, `round` |
| Split | `split`, `cut`, `shatter` |
| Subtraction | `minus`, `without`, `-` |
| True | `true`, `yes`, `ok`, `right` |
| Variables | `a`, `an`, `the`, `my`, `your`, `our`, `their`, `his`, `her` |
| While loops | `while`, `until` |
