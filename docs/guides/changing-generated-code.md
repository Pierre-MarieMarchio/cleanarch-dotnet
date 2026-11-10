---
type: guide
title: Changing generated code
description: How this project changes the code a generator writes, through code templates of its own.
---

# Changing generated code

## When

Every feature `add feature` writes should carry something the tool's code does not: an attribute on
each record, a header, a test of a house rule, a different layout of a class. Rewriting it by hand
after each run is the change this guide replaces. At the end, `add feature` and `add field` write
those files from code templates this project holds and edits, and every other file as before.

## Steps

1. List the code templates of `add feature`, the path each file is written from:

   ```bash
   dotnet cleanarch templates copy add-feature --dry-run
   ```

   Each one lies at its path in the reference feature, `Placeholders` of the type `Placeholder`, with
   `.txt` appended: the controller's is the template that ends in `PlaceholdersController.cs.txt`.

2. Copy the ones to change, by their path or the end of it after a `/`; without a name, the command
   copies them all:

   ```bash
   dotnet cleanarch templates copy add-feature PlaceholdersController.cs PlaceholderRecord.cs
   ```

   Each copy lands at `code-templates/add-feature/<reference path>.txt`, and
   `code-templates/manifest.json` records the version of the tool it came from and the SHA-256 of
   that text. The command never overwrites a file: a template copied already is listed as kept.

3. Commit the copies untouched, then edit them in the language [below](#the-language). The first
   commit is the base a later merge needs, as
   [taking the tool's change](#when-the-tool-changes-a-template) says.

4. Run the generator, `--dry-run` first:

   ```bash
   dotnet cleanarch add feature <Features> <Feature> --access shared --dry-run
   ```

   It writes each file from this project's copy where there is one, and from the tool's text
   otherwise. A copy left as it was copied writes the same bytes as the tool's text.

A team that wants every file `add feature` writes from its own text copies them all; one that wants
a single file changed copies that one, and the others follow the tool from version to version.

## What a copy changes

A copy changes the text of one file, and nothing else:

| A copy decides | The tool decides |
|---|---|
| the text of the file it writes: its usings, members, attributes, comments, tests | which files a generation writes, their paths and their names |
| which lines each option keeps, through its directives | the options, their defaults and their refusals |
| how the fields are written, through its blocks | the fields' grammar and the code each token holds |
| | the edits of files the feature does not own: the composition, the context, the indexes of pages |
| | the feature's page, written from `docs/_page-templates/feature.md` |

The edits go into files no template writes, so a copy cannot move them; a copy that renames a type
or a namespace an edit names breaks the build `add feature` runs next, which names the line.

## The language

A copy is the C# of the reference feature with four additions, read in this order: directives, the
reference names, blocks over fields, and tokens. Text outside them is written as it stands.

### Directives

A line whose first non-blank characters are `#if <condition>`, `#elif <condition>`, `#else` or
`#endif` keeps or drops the lines up to the next directive, and is itself dropped; `#else` and
`#endif` take nothing after them, and an `#if` in the middle of a line is text. Directives nest, and
a directive in a branch that does not hold is still read, so a typo there is refused too.

A condition is read as C# reads one: a flag, `!`, `&&`, `||` and parentheses, `&&` binding tighter
than `||`, as `#if record && (create || update)`. A flag is one of the table below and nothing else,
so a generated file cannot carry a real `#if DEBUG`.

### The flags of add feature

`add feature` sets the flags its options give, and `add field` the same flags from the options the
feature's manifest records. A flag is the word of an option where an option decides it, and otherwise
says what the feature does:

| Flag | Holds when |
|---|---|
| `get` | `get` is among `--operations`: one is read by its id. |
| `list` | `list` is among `--operations`: a page of them is read. |
| `create` | `create` is among `--operations`. |
| `update` | `update` is among `--operations`. |
| `delete` | `delete` is among `--operations`. |
| `owner` | `--access owner`: each one belongs to the caller who created it, and the deletion of that account reaches it. |
| `shared` | `--access shared`: every caller reaches every one. |
| `record` | `--model record`: a service over a store, without a domain aggregate. |
| `aggregate` | `--model aggregate`: one use case per operation over a domain aggregate. |
| `etag` | `--concurrency etag`: a persisted version, an `ETag` and `If-Match`. |
| `erase` | `--deletion delete`: the rows of a deleted account are removed. |
| `anonymise` | `--deletion anonymise`: what identifies the owner of a deleted account is rewritten. |
| `prefixedTables` | The database is SQLite, which has no schemas, so a table's name starts with its feature's schema. |
| `writes` | Create, update or delete is among the operations, so the feature writes through a store or a repository. |
| `changesOne` | Update or delete is among the operations: each changes one that can be missing, or stale under `etag`. |
| `takesContent` | Create or update is among the operations: each takes the feature's content and validates it. |
| `returnsOne` | Get, create or update is among the operations: each answers with one in full. |
| `readsOne` | Get is among the operations, or a `record` creates or updates and reads back what it wrote: the queries port reads one by its id. |
| `reads` | The feature has a queries port: it reads one, lists, or is `owner`, whose account deletion lists the owners. |
| `store` | A `record` that writes or is `owner` stages its changes through a store. |
| `notFound` | An operation can answer that one is not found: get, update, delete, or a `record`'s create. |
| `stampsTime` | A `record` creates or updates, so its service reads the time it stamps. |
| `anonymisedText` | Under `anonymise`, a required text is rewritten to a fixed text: a field is a non-nullable string. |
| `trimsText` | Create or update takes a text the service trims: a field is a string. |
| `constants` | A limit of the content is declared as a constant: a field is a string, a text or a decimal, or has a bound. |

### Reference names

A copy names the reference feature, and each name becomes the generation's wherever it appears,
inside a word or in prose: `Placeholders` the feature's folder, `Placeholder` its type, `placeholder`
the type lowered, and the reference project's name, which a copy holds where this project's name
stands, in its namespaces and its paths. A copy that needs the word `placeholder` for itself cannot
have it.

### Blocks over fields

A block repeats its body once per field of the feature, in the order `--fields` gives them. On lines
of its own:

```text
//#each field [where <trait>] [separator "<text>"]
    <body>
//#end ["<tail>"]
```

Inside a line, the same block is `/*#each field ...*/<body>/*#end*/`. `where <trait>` keeps the
fields that have the trait; `separator "<text>"` joins them with the text, `\n` standing for a line
break, where a line block otherwise joins them with a line break and an inline one with nothing; a
quoted tail after `#end` follows the last field. A block that keeps no field writes nothing, its tail
included. Blocks do not nest, and a directive inside one is decided once per generation, not once
per field. This block writes a constructor's parameters, one per line:

```text
//#each field separator ",\n"
        %field.Type% %field.Parameter%
//#end ") : base(id)"
```

Inside a block, `%field.<Token>%` is a value of the field:

| Token | Writes |
|---|---|
| `Name` | the field's name, `DueDate` |
| `Type` | its C# type, `?` included when it is nullable |
| `Parameter` | its name as a parameter, `dueDate` |
| `RecordProperty` | its property on the persistence record |
| `Configuration` | its EF configuration: column, length, precision, index |
| `Stored` | the value the store writes into the record |
| `Trimmed` | the content's value, trimmed |
| `Required` | the call that checks it in the aggregate, or its parameter when it has no rule |
| `Validated` | the local that holds it once checked, or its parameter |
| `Require` | the aggregate's method that checks it |
| `Constants` | its limits, as constants |
| `Ceiling`, `IntegerDigits` | a decimal's ceiling constant and its digits before the point |
| `Anonymised` | the value an anonymised account leaves in it |
| `AnonymisedAccessors` | its accessors in the aggregate, settable when it is anonymised |
| `WireParameter`, `WireDocumentation` | its parameter on an HTTP request, and that parameter's documentation |
| `Sample` | a valid value for a test |
| `Sorting`, `CursorKey`, `KeysetMethod` | how the list sorts by it, its cursor's key, and the keyset method |

A trait selects fields with `where`:

| Trait | Holds for a field that |
|---|---|
| `string` | is a `string(n)` |
| `decimal` | is a `decimal` |
| `constants` | has a limit declared as a constant: a string, a text, a decimal, or a bound |
| `require` | has a rule its aggregate checks |
| `anonymised` | an anonymised account rewrites |
| `summary` | the list's summary carries, every field but a `text` |
| `sortable` | the list sorts by |
| `keyset` | the list pages through by cursor |

### Tokens

A `%Token%` outside a block is code the generator computes for the whole feature, in the middle of a
line, where a directive cannot stand: the arguments of a call, an owner's clause, a list's input. A
copy moves or drops the ones it holds, and may use any the tool's templates of `add feature` hold;
a token the generation does not set is refused rather than written as code.

A using marked `//?<Type>|<Type>` at the end of its line is kept only when the code after the
namespace names one of those types, so a branch can drop the last use of a namespace without
leaving an unused using.

## Refusals

`add feature` and `add field` read every copy before writing anything, and refuse, writing nothing,
with a message that starts with the copy's path, and the line's number where there is one:

- a file under `code-templates/add-feature` whose path names no template of `add feature`; a file
  whose name starts with a dot is not read;
- a condition naming a flag outside the table, empty, left dangling or unbalanced;
- an `#elif`, `#else` or `#endif` without its `#if`, an `#if` never closed, text after `#else` or
  `#endif`;
- a block over another list than `field`, naming an unknown token or trait, left open, or a
  `%field.<Token>%` outside any block;
- a `%Token%` the generation does not set.

Fix the copy, or delete it, and run the same command again.

## What the feature records

The feature's manifest, `.feature-manifest/<Features>.json`, records under `templates` each copy the
run wrote from, with the SHA-256 of its text, and the report of `add feature` lists them under
`Written from this project's code templates:`. A feature written from the tool's text alone records
none.

`add field` renders the feature before and after its new fields from the same copies, so the two
renders it compares never come from different texts. When a copy is not the text the manifest
records, changed, added or deleted since, it warns, starting with `warning: `, and still writes: a
member the difference reaches reads as changed by hand, and is refused as one. Once written, the
manifest records the copies that run used.

`remove feature` removes what the manifest records, whatever text it was written from, and leaves
`code-templates/` as it is.

## When the tool changes a template

A copy keeps its text when a newer version of the tool changes the template it was copied from.
`add feature` and `add field` then warn, on every run that writes from the copy, `--dry-run`
included, with `warning: <path> overrides a template the tool has changed since version <version>,
which it was copied from`, and still write from the copy: a warning, never a refusal, and the exit
code does not change. A `code-templates/manifest.json` the tool cannot read is one warning too. A copy with no record in `code-templates/manifest.json`, written by hand
rather than copied, is never compared.

To take the change, move the copy aside, copy the template again, and carry this project's changes
over. When the untouched copy was committed before it was edited, that commit holds the base of a
three-way merge: `git merge-file` merges this project's text and the tool's new one from it.

## Check

`add feature --dry-run` lists every copy it would write from under `Written from this project's code
templates:`, and refuses a broken copy naming its path and line. After a real run,
`dotnet cleanarch check` builds the feature and runs its tests, and the feature's manifest records
each copy under `templates`.

## Undo

Delete the copy, or the whole `code-templates/` folder. The next run writes from the tool's text;
the features written already keep their code, and the next `add field` on one of them warns once that
its copies changed. `code-templates/manifest.json` is the tool's file: an entry for a deleted copy
is harmless, and copying the template again replaces it.
