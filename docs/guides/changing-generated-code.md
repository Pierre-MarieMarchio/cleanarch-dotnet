---
type: guide
title: Changing generated code
description: How this project changes the code a generator writes, through code templates of its own.
---

# Changing generated code

## When

<!--#if (canAddFeature)-->
Every feature `add feature` writes should carry something the tool's code does not: an attribute on
each record, a header, a test of a house rule, a different layout of a class. Rewriting it by hand
after each run is the change this guide replaces. At the end, `add feature` and `add field` write
those files from code templates this project holds and edits, and every other file as before.
`add use-case`, `add event` and `add setting` take code templates the same way, each from its own
folder.
<!--#endif-->
<!--#if (!canAddFeature)-->
Every use case `add use-case` writes should carry something the tool's code does not: an attribute
on each message, a header, a test of a house rule, a different layout of a class. Rewriting it by
hand after each run is the change this guide replaces. At the end, `add use-case` writes those files
from code templates this project holds and edits, and every other file as before. `add event` and
`add setting` take code templates the same way, each from its own folder.
<!--#endif-->

## Steps

1. List the code templates of the generator, the path each file is written from:
   <!--#if (canAddFeature)-->

   ```bash
   dotnet cleanarch templates copy add-feature --dry-run
   ```

   Each one lies at its path in the reference feature, `Placeholders` of the type `Placeholder`, with
   `.txt` appended: the controller's is the template that ends in `PlaceholdersController.cs.txt`.
   <!--#endif-->
   <!--#if (!canAddFeature)-->

   ```bash
   dotnet cleanarch templates copy add-use-case --dry-run
   ```

   Each one lies at its path for the reference command `ApprovePlaceholder` or query
   `GetPlaceholderTotals` of the feature `Placeholders`, with `.txt` appended: the command's is the
   template that ends in `ApprovePlaceholder.cs.txt`.
   <!--#endif-->

2. Copy the ones to change, by their path or the end of it after a `/`; without a name, the command
   copies them all:
   <!--#if (canAddFeature)-->

   ```bash
   dotnet cleanarch templates copy add-feature PlaceholdersController.cs PlaceholderRecord.cs
   ```
   <!--#endif-->
   <!--#if (!canAddFeature)-->

   ```bash
   dotnet cleanarch templates copy add-use-case ApprovePlaceholder.cs
   ```
   <!--#endif-->

   Each copy lands at `code-templates/<generator>/<reference path>.txt`, and
   `code-templates/manifest.json` records the version of the tool it came from and the SHA-256 of
   that text. The command never overwrites a file: a template copied already is listed as kept.

3. Commit the copies untouched, then edit them in the language [below](#the-language). The first
   commit is the base a later merge needs, as
   [taking the tool's change](#when-the-tool-changes-a-template) says.

4. Run the generator, `--dry-run` first:
   <!--#if (canAddFeature)-->

   ```bash
   dotnet cleanarch add feature <Features> <Feature> --access shared --dry-run
   ```
   <!--#endif-->
   <!--#if (!canAddFeature)-->

   ```bash
   dotnet cleanarch add use-case <Features> <UseCase> --kind command --dry-run
   ```
   <!--#endif-->

   It writes each file from this project's copy where there is one, and from the tool's text
   otherwise. A copy left as it was copied writes the same bytes as the tool's text.

A team that wants every file a generator writes from its own text copies them all; one that wants a
single file changed copies that one, and the others follow the tool from version to version.

## What a copy changes

A copy changes the text of one file, or of a report, and nothing else:

| A copy decides | The tool decides |
|---|---|
| the text of the file it writes: its usings, members, attributes, comments, tests | which files a generation writes, their paths and their names |
| which lines each option keeps, through its directives | the options, their defaults and their refusals |
| how the fields, parameters or keys are written, through its blocks | their grammar and the code each token holds |
| | the edits of files no template writes: the composition, the context, the indexes of pages |
| the text of the report, through its directives and tokens | the facts it reports, and the refusals, warnings and steps of a run |
| <!--#if (canAddFeature)--> |
| | the feature's page, written from `docs/_page-templates/feature.md` |
| <!--#endif--> |

The edits go into files no template writes, so a copy cannot move them; a copy that renames a type
or a namespace an edit names breaks the next build, which names the line.

## The language

A copy is the C# of the reference feature with four additions, read in this order: directives, the
reference names, blocks, and tokens. Text outside them is written as it stands.

### Directives

A line whose first non-blank characters are `#if <condition>`, `#elif <condition>`, `#else` or
`#endif` keeps or drops the lines up to the next directive, and is itself dropped; `#else` and
`#endif` take nothing after them, and an `#if` in the middle of a line is text. Directives nest, and
a directive in a branch that does not hold is still read, so a typo there is refused too.

A condition is read as C# reads one: a flag, `!`, `&&`, `||` and parentheses, `&&` binding tighter
than `||`, as `#if configured && !required` in a template of `add setting`. A flag is one of its
generator's table below and nothing else, so a generated file cannot carry a real `#if DEBUG`.

### Reference names

A copy names its generator's reference, and each name becomes the generation's wherever it appears,
inside a word or in prose; the reference project's name, which a copy holds where this project's
name stands in its namespaces and its paths, becomes this project's. Each generator's section below
lists its reference names.

### Blocks

A block repeats its body once per item of its generator's list, `field`, `parameter` or `key`, in the
order the command gives them. On lines of their own:

```text
//#each <list> [where <trait>] [separator "<text>"]
    <body>
//#end ["<tail>"]
```

Inside a line, the same block is `/*#each <list> ...*/<body>/*#end*/`. `where <trait>` keeps the
items that have the trait; `separator "<text>"` joins them with the text, `\n` standing for a line
break, where a line block otherwise joins them with a line break and an inline one with nothing; a
quoted tail after `#end` follows the last item. A block that keeps no item writes nothing, its tail
included. Blocks do not nest, and a directive inside one is decided once per generation, not once
per item. Inside a block, `%<list>.<Token>%` is a value of the item, from its generator's table.

### Tokens

A `%Token%` outside a block is code the generator computes for the whole generation, in the middle
of a line, where a directive cannot stand: the arguments of a call, the key a test breaks. A copy
moves or drops the ones it holds, and may use any its generator's templates hold; a token the
generation does not set is refused rather than written as code.

A using marked `//?<Type>|<Type>` at the end of its line is kept only when the code after the
namespace names one of those types, so a branch can drop the last use of a namespace without
leaving an unused using.
<!--#if (canAddFeature)-->

## The templates of add feature

`add feature` and `add field` write a feature's files from the templates under
`code-templates/add-feature`, each at its path in the reference feature.

### Reference names of add feature

`Placeholders` becomes the feature's folder, `Placeholder` its type, and `placeholder` the type
lowered. A copy that needs the word `placeholder` for itself cannot have it.

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

### Blocks over fields

A block of `add feature` repeats over `field`, the fields of the feature in the order `--fields`
gives them. This block writes a constructor's parameters, one per line:

```text
//#each field separator ",\n"
        %field.Type% %field.Parameter%
//#end ") : base(id)"
```

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
<!--#endif-->

## The templates of add use-case

`add use-case` writes its files from templates under `code-templates/add-use-case`. A command's are
those of the reference command `ApprovePlaceholder` of the feature `Placeholders`, a query's those of
the reference query `GetPlaceholderTotals`, which answers `PlaceholderTotalsDto`: the use case, its
validator, the validator's tests, the handler's test and the request contract, and for the query its
DTO and its response contract. The tool decides which of them a run writes: the validator and its
tests only with a rule, the handler's test only where the application's tests hold the
`ValidatedHandler` double, the DTO unless the feature declares it, the contracts only where the API
serves the feature. The controller's action and the route's row are edits, which no template writes.

### Reference names of add use-case

`ApprovePlaceholder` and `GetPlaceholderTotals` become the use case's name, `PlaceholderTotals` the
name its DTO and its response start with, `Placeholders` the feature's folder, and `Placeholder` the
aggregate where the use case carries its id. Prose takes three tokens instead, since a name written
in words is not a reference name: `%Spoken%` is the use case's name in words, `approve invoice`;
`%SpokenAggregate%` the aggregate's, where the use case carries its id; `%SpokenAnswer%` the
answer's, in a query.

### The flags of add use-case

| Flag | Holds when |
|---|---|
| `id` | `id` is in `--input`, as by default: the message carries the id of the feature's aggregate, first. |
| `parameters` | The message carries a value: the id, an input, or both. |

### Blocks over parameters

A block of `add use-case` repeats over `parameter`, the values the message carries in the order
`--input` gives them, the aggregate's id first. This block writes a message's members, one per line:

```text
//#each parameter separator ",\n"
    %parameter.Type% %parameter.Name%
//#end ") : ICommand<Result>;"
```

| Token | Writes |
|---|---|
| `Name` | the parameter's name, `Reason` |
| `Type` | its C# type, as `--input` gives it |
| `Required` | the message its rule refuses an empty value with, `Reason is required.` |
| `Valid` | a valid value for a test, `default!` for a type the tool knows no value of |
| `Empty` | the empty value its rule refuses, where it has a rule |

| Trait | Holds for a parameter that |
|---|---|
| `input` | is an input, not the aggregate's id, so a request contract carries it |
| `ruled` | has a rule: a known type that is not a `bool` nor nullable |
| `malformed` | is the first with a rule, the one a handler's test empties |

## The templates of add event

`add event` writes its files from templates under `code-templates/add-event`, each at its path for
the event `PlaceholderNotedDomainEvent` of `Placeholders`, consumed by `Listeners` whose reaction is
`NotifyListeners`:

| Template | Written |
|---|---|
| `PlaceholderNotedDomainEvent.cs.txt` | always: the event |
| `PlaceholderNotedDomainEventTests.cs.txt` | when the domain's unit test project exists |
| `NotifyListenersOnPlaceholderNotedConsumer.cs.txt` | with `--consumer`: the consumer |
| `NotifyListenersOnPlaceholderNotedConsumerTests.cs.txt` | with `--consumer`, when the application's unit test project exists |

Its reference names are `PlaceholderNoted`, the event's name without `DomainEvent`, `Placeholders`
the folder of the feature that raises it, `Listeners` the folder of the feature that consumes it,
and `NotifyListeners` the reaction. The registration in `ApplicationModule.cs`, the entry of an
unconsumed event in `DomainEventTests.cs` stay the tool's; its report is a template, as
[reports](#reports) says.

### The flags of add event

| Flag | Holds when |
|---|---|
| `configured` | The feature of `--consumer` takes the configuration: its `Add<Features>` method has an `IConfiguration` parameter. |

### The fields of an event

A block repeats over `field`, the fields of `--fields` in their order, or the aggregate's id by
default. Its tokens are `Name`, the field's name, `Type`, its C# type, and `Sample`, the value the
event's test gives it: a local `guid<n>` for a `Guid`, its index among the fields after `guid`, and
a literal otherwise. Its one trait, `guid`, holds for a field of type `Guid`, whose test declares
its local.

## The templates of add setting

`add setting` writes its two files from templates under `code-templates/add-setting`: the options
class with its validator, whose template ends in `PlaceholderLimitsOptions.cs.txt`, and their
tests, whose template ends in `PlaceholderLimitsOptionsTests.cs.txt` and which it writes only where
the application's unit test project exists. Its edits stay the tool's: the registration, each
host's `appsettings.json`, the configuration reference, Compose and `.env.example`.

The reference names are the section `PlaceholderLimits` and the feature's folder `Placeholders`.
Keep `SectionName` set to the section's name: `add setting` and `remove feature` find a section by
that constant in a file whose name ends in Options.cs. A `%Token%` outside a block is `%Broken%`,
the key the test of a section out of range breaks, the first validated one, and `%BrokenValue%`, the
value it breaks it with.

### The flags of add setting

| Flag | Holds when |
|---|---|
| `configured` | The feature's `Add<Features>` method takes the configuration, so the tests pass it the one they build. |
| `required` | A key is required: a `secret`, or a `string` without a default, so a valid section gives it a value. |

### Blocks over keys

A block of `add setting` repeats over `key`, the keys in the order `--keys` gives them. A key's code
differs by its type, and a directive is decided once per generation, so that code is a token:
`%key.<Token>%` is

| Token | Writes |
|---|---|
| `Name` | the key's name, `MaxItems` |
| `Type` | its C# type |
| `Default` | its property's initial value |
| `Condition`, `Message` | the validator's check that fails, and what the failure says after the key |
| `PassingCases`, `Passing`, `PassingParameter`, `PassingValue` | the attributes of the test of a valid value, the end of its name, its parameter and the value it sets |
| `FailingCases`, `Failing`, `Parameter`, `Value` | the cases of the test of an invalid value, the end of its name, its parameter and the value it sets |
| `Valid` | the value a valid section gives a required key |
| `Setting`, `Expected` | a value in range other than the default, as configuration text and as the bound property's value |

| Trait | Holds for a key that |
|---|---|
| `validated` | the validator checks: every type but `bool` |
| `required` | is a `secret`, or a `string` without a default |
| `supplied` | is required and is not `%Broken%`, so the test of a section out of range gives it a value |

## Reports

What a generator prints after a run, its report, comes from a template too:
`code-templates/<generator>/report.txt`, which `templates copy <generator>` copies with the
generator's files.
<!--#if (canAddFeature)-->
`add field` writes its files from the templates of `add feature`, so its report alone lies under
`code-templates/add-field`, which `templates copy add-field` copies.
<!--#endif-->
A copy changes the text of the report and nothing a run does.

A report is prose, not C#: its directives and reference names read as a file's do, and its usings
and blank lines are kept as written. Its flags are those of its generator, the flags of the report's
mode below, of which one holds, and its own, listed with each generator. A `%Token%` is a fact the
tool computes, such as the files written or the steps a run takes: a line that is exactly a
`%Token%` becomes its lines, and is dropped when the fact is empty. A report repeats no block, since
each list it shows is a token. A copy names only the tokens the tool's own `report.txt` names.

The tool keeps its own text for the rest of what a run prints: its refusals, errors and warnings,
the `--dry-run` line, the steps it runs and what they print, its exit code, and the copies it wrote
its files from, listed after the report under `Written from this project's code templates:`.
`remove feature` and `templates copy` have no report template.

A generator reads its copy of `report.txt` before writing anything, and refuses a broken one as
[refusals](#refusals) lists, though the report itself prints after the run.

### The flags of every report

| Flag | Holds when |
|---|---|
| `applied` | The run wrote its files and made its edits. |
| `dryrun` | `--dry-run`: the run wrote nothing, and says what it would write. |
| `refused` | An edit found no line to stand beside, so the run wrote nothing, and says what to make by hand. |
<!--#if (canAddFeature)-->

### The report of add feature

It takes the flags of add feature, those of every report, and these:

| Flag | Holds when |
|---|---|
| `api` | The project has the API, so the feature's routes and its refusal join `docs/reference/api.md`. |
| `reaction` | The project had no feature that owns user data, so the account-deleted reaction ships with this one, in five more edits. |
| `fieldTests` | The integration test of the fields is written, and the database fixture truncates the feature's schema. |
| `sorted` | A field sorts the list. |
| `kept` | Under `anonymise`, a field is kept as it was: one that is not a string, a text or nullable. |
| `page` | The feature's page is written. |
| `inPlace` | An edit was in place already, so it was left as it was and not recorded. |

Its tokens: `%Count%` and `%Files%`, the files written; `%Options%`, the options in words;
`%Manifest%`, the feature's manifest; `%Skipped%`, the test projects not in the tree; `%Edits%`, the
numbered edits; `%TestDatabase%`, the database fixture of the integration tests; `%Fields%`,
`%Sortable%` and `%Kept%`, the fields as declared, those the list sorts by and those anonymising
keeps; `%Steps%`, the commands a run takes; `%Page%`, `%PageTemplate%` and `%Unwritten%`, the
feature's page, its template, and why it was not written; `%Routes%` and `%ErrorRow%`, the route
section and the refusal's row of `docs/reference/api.md`; `%InPlace%`, the edits found in place.

### The report of add field

It takes the flags of add feature, those of every report, and this one:

| Flag | Holds when |
|---|---|
| `sorted` | The list sorts by a field added: `list` is among the operations, and a field added is sortable. |

Its tokens: `%Added%`, the fields as declared; `%Options%`, the feature's options in words;
`%Count%` and `%Changes%`, the files changed with each member; `%Manifest%`, the feature's manifest;
`%Compatibility%`, what each field changes for the callers; `%Sortable%`, the fields added the list
sorts by; `%Steps%`, the commands a run takes.
<!--#endif-->

### The report of add use-case

It takes the flags of add use-case, those of every report, and these:

| Flag | Holds when |
|---|---|
| `command` | `--kind command`; a query otherwise. |
| `api` | The API serves the feature, so the run edits its controller and writes its contracts. |
| `request` | The message carries an input, so a request contract carries it over HTTP. |
| `validator` | An input has a rule, so a validator is written. |
| `validatorTests` | The validator's tests are written. |
| `handlerTests` | The handler's test is written: the application's tests hold the `ValidatedHandler` double. |
| `skipped` | The application's test project is not in the tree, so no test is written. |
| `unknown` | An input is of a type the command knows no rule and no test value for. |
| `dtoDeclared` | The query answers a DTO the feature declares already. |
| `manifest` | A feature manifest names the feature, and records the files and the edits. |
| `page` | The feature has its page under `docs/features`. |
| `fullRoute` | The route's row is written in full, from `/api`, not relative to its table's prefix. |

Its tokens: `%Count%` and `%Files%`, the files written; `%Kind%`, `command` or `query`; `%Input%`,
the message's type; `%Manifest%`, the feature's manifest; `%Skipped%`, the test project not in the
tree; `%Unknown%`, the inputs of an unknown type; `%Controller%`, `%Usings%` and `%Action%`, the
controller, the usings it lacks and the action; `%RequestPath%`, `%Request%`, `%ResponsePath%` and
`%Response%`, the contracts and their code; `%Steps%`, the commands a run takes; `%RouteRow%`, the
route's row; `%Page%`, the feature's page; `%Construction%` and `%Hosts%`, the message to send and
the hosts that serve the feature, without the API.

### The report of add event

It takes the flags of add event, those of every report, and this one:

| Flag | Holds when |
|---|---|
| `consumed` | `--consumer`: a feature reacts to the event, and its consumer is written. |

Its tokens: `%Count%` and `%Files%`, the files written; `%Edited%`, the files edited.

### The report of add setting

It takes the flags of add setting and those of every report, and no other.

Its tokens: `%Count%` and `%Files%`, the files written; `%Edited%`, the files edited; `%Hosts%`, the
hosts whose `appsettings.json` carries the section; `%Unset%`, what to do for each key the file
gives no value.

## Refusals

A generator reads every copy of its own before writing anything, and refuses, writing nothing, with
a message that starts with the copy's path, and the line's number where there is one:

- a file under `code-templates/<generator>` whose path names no template of that generator; a file
  whose name starts with a dot is not read;
- a condition naming a flag outside its generator's table, empty, left dangling or unbalanced;
- an `#elif`, `#else` or `#endif` without its `#if`, an `#if` never closed, text after `#else` or
  `#endif`;
- a block over another list than the generator's, naming an unknown token or trait, left open, or a
  `%<list>.<Token>%` outside any block;
- a `%Token%` the generation does not set;
- in `report.txt`, a block, or a `%Token%` the tool's own report does not name.

Fix the copy, or delete it, and run the same command again.

## What the feature records

A generator's report lists each copy the run wrote from under `Written from this project's code
templates:`, under `--dry-run` too, and lists none when it refuses.
<!--#if (canAddFeature)-->

The feature's manifest, `.feature-manifest/<Features>.json`, records under `templates` each copy
`add feature` wrote from, with the SHA-256 of its text. A feature written from the tool's text alone
records none.

`add field` renders the feature before and after its new fields from the same copies, so the two
renders it compares never come from different texts. When a copy is not the text the manifest
records, changed, added or deleted since, it warns, starting with `warning: `, and still writes: a
member the difference reaches reads as changed by hand, and is refused as one. Once written, the
manifest records the copies that run used.

`add use-case`, `add event` and `add setting` record no copy in the feature's manifest, since only
`add field` reads that record back, to render the files of `add feature` again.
<!--#endif-->
<!--#if (!canAddFeature)-->

`add use-case`, `add event` and `add setting` record no copy in a feature's manifest, since no later
run renders their files again: `code-templates/manifest.json` alone records the copies.
<!--#endif-->

`remove feature` removes what a feature's manifest records, whatever text it was written from, and
leaves `code-templates/` as it is.

## When the tool changes a template

A copy keeps its text when a newer version of the tool changes the template it was copied from. The
generator then warns, on every run that writes from the copy, `--dry-run` included, with
`warning: <path> overrides a template the tool has changed since version <version>, which it was
copied from`, and still writes from the copy: a warning, never a refusal, and the exit code does not
change. A `code-templates/manifest.json` the tool cannot read is one warning too. A copy with no
record in `code-templates/manifest.json`, written by hand rather than copied, is never compared.

To take the change, move the copy aside, copy the template again, and carry this project's changes
over. When the untouched copy was committed before it was edited, that commit holds the base of a
three-way merge, and `git merge-file` merges this project's text and the tool's new one from it:

```bash
mv <path> <aside>
dotnet cleanarch templates copy <generator> <template>
git commit -m "<message>" -- <path> code-templates/manifest.json
git show <commit>:<path> > <base>
git merge-file <aside> <base> <path>
mv <aside> <path>
```

`<path>` is the copy's path and `<commit>` the commit that holds it untouched. Copying again records
the newer version in `code-templates/manifest.json`, so the warning stops, and committing the new
copy untouched keeps the base of the next merge. `git merge-file` writes the merge into `<aside>`
and marks each conflict; resolve them before moving it back.

## Check

A generator's `--dry-run` lists every copy it would write from under `Written from this project's
code templates:`, and refuses a broken copy naming its path and line. After a real run,
`dotnet cleanarch check` builds what it wrote and runs its tests.
<!--#if (canAddFeature)-->
The manifest of a feature `add feature` wrote records each copy under `templates`.
<!--#endif-->

## Undo

Delete the copy, or the whole `code-templates/` folder. The next run writes from the tool's text,
and the code written already keeps its text.
<!--#if (canAddFeature)-->
The next `add field` on a feature written from a copy warns once that its copies changed.
<!--#endif-->
`code-templates/manifest.json` is the tool's file: an entry for a deleted copy is harmless, and
copying the template again replaces it.
