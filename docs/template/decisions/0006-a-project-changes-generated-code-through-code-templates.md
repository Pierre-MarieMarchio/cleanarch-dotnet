---
type: decision
title: 0006. A project changes generated code through code templates
description: Why a project changes the files a generator writes by overriding its code templates, and why their key, language and flags are a versioned contract.
status: Proposed
---

# 0006. A project changes generated code through code templates

Status: Proposed

## Context

A team that wants the generators to write another shape of code, a different base class, an extra
attribute on every record or another test style, has nowhere to say it.
[Decision 0003](0003-a-project-runs-its-tasks-from-a-pinned-dotnet-tool.md) does not let a project
eject `add` and `remove`, since they run on a library, Spectre.Console and Roslyn that a file-based
app does not carry, and it leaves the code they write to a depth of its own. `add feature` and
`add field` render that code from templates, through
[the template engine](../maintaining-the-template.md#the-template-engine). Once a project
holds a copy of one of those templates, the copy depends on three things the tool owns: where the
tool looks for it, the language it is written in, and the flags its conditions name. A project
pinned at one version of the tool and moved to the next by `dotnet tool update` must find all three
where it left them, or its copies break with no change of its own.

## Decision

- **The key is the reference path.** A project overrides one file a generator writes with
  `code-templates/<generator>/<reference path>.txt` at its root, where the reference path is the
  path the generator writes that file at in the reference project: product `AppTemplate`, project
  folders `AT.<Part>`, feature `Placeholders`, type `Placeholder`. The tool stores its own templates
  under the same keys, so copying one is a plain copy and no table maps one name to another. A path
  never holds a `%`: a file whose name a token would decide is split into one template per file.
- **An override is text only.** It replaces the text of a file the generator writes and nothing
  else: it never chooses a path, a type or code to run. The edits a generator makes to files that
  already exist, the registrations, the closed lists and the manifest, stay the tool's code.
- **The language is one, and strict.** Directives at the start of a line, `#if`, `#elif`, `#else`
  and `#endif`, over one condition grammar: `!`, `&&`, `||`, parentheses and the generator's known
  flags, as [directives and conditions](../maintaining-the-template.md#directives-and-conditions)
  gives it. Then `each` blocks over the generator's lists, the reference names, and `%Token%`s the
  generator computes. A `%Token%` left after rendering is refused, as is any line the grammar cannot
  read, and a refusal names the file and the line.
- **The vocabulary is readable and per generator.** Each generator that renders templates owns one
  set of flags, each named for what it means and documented with that meaning, as
  [the flags of add feature](../maintaining-the-template.md#the-flags-of-add-feature) are. A
  template names only its own generator's flags; another generator's flag is refused as a typo is.
- **The three are a public contract.** Renaming a flag, a token or a reference name, or moving a
  key, breaks the copies a project holds, so it takes a major version of the tool. Adding a flag, a
  token or a template does not.
- **`templates copy <generator>` copies once.** It writes the tool's templates of that generator, or
  those named, into `code-templates/`, never overwrites a file already there, and takes `--dry-run`
  and no `--check`, as `eject` and `adopt` do under rule 4 of decision 0003. It records each copy in
  `code-templates/manifest.json`, with the version of the tool and the hash of the source it copied.
- **A changed source warns, it never refuses.** When the tool's source of a template the project
  overrides has changed since the copy, a generator warns, names the file and the version it was
  copied from, and still writes from the project's text. The feature manifest records the hash of
  each override `add feature` used, so `add field` renders the feature's files from the same text,
  and warns when that text has changed since.
- **Without an override, the output is the tool's.** A project that holds no `code-templates/`
  folder gets what the tool's own templates write, byte for byte.

`ConditionTests` holds the grammar and `DirectivesTests` the directives, each refusal naming the
offending text and line.
`FeatureFlagsTests.TheGuideToCodeTemplates_ListsEachFlagOfAddFeature_WithItsMeaning_AndNoOther` holds the
vocabulary of `add feature` to its table, flag for flag. `NewFeatureTests.TemplateGuard` holds the
passes together, a leftover `%Token%` and another generator's flag refused, and
`NewFeatureTests.WithoutFields`, `NewFeatureTests.WithFields` and
`NewFeatureTests.FieldAdditionOutput` hold the output of a project without an override to its
digests. The override itself, the copy, its record and the warning each take a test of their own,
in the generator that reads them, seen failing once before this decision is accepted.

## Alternatives considered

- **Scriban or Liquid.** A template in either is not code the compiler or an editor reads, so the
  reference feature stops being C# that compiles, and the repository carries a second language that
  every project overriding a template must learn.
- **The Roslyn preprocessor, or a C# source generator.** The preprocessor evaluates only symbols
  defined for a compilation, not the flags of one generation, and a source generator writes into a
  compilation, not the files of a project a team then owns.
- **Microsoft.TemplateEngine.Core.** It evaluated only the `#if` lines, had quirks that each needed
  a guard, and let bad input through: an unknown flag or a typo in a dead branch was written out
  instead of refused.
- **The code kept as C# strings in the generators.** A team could change it only by ejecting the
  command, which decision 0003 does not offer for `add` and `remove`.
- **Ejecting `add` and `remove` as the way to change the output.** A team would fork the whole
  generator, its edits and its refusals, to change one file's text, and stop receiving the tool's
  fixes for all of it.

## Consequences

- A team changes the code a generator writes by editing a copy, and keeps every fix the tool makes
  to the edits, the refusals and the files it does not override.
- A copy freezes at the version it was taken from: the warning says so, and the team carries the
  tool's change over by hand, which committing a copy untouched before editing it makes a merge.
- The flags, tokens and reference names become names a project relies on: each is chosen to read
  well before it ships, and renaming one waits for a major version.
- A template's text is data the generator cannot check as C#: an override that renames a type or a
  namespace the tool's edits rely on breaks the build `add feature` runs next, which reports it.
- The engine's internals, its passes and their order, stay free to change while the language a
  template reads keeps its meaning.

## When this decision does not apply

- The edits a generator makes to existing files: they stay the tool's code, which a project changes
  only by replacing the whole command with a local one of the same name, as decision 0003 says.
- `remove feature` and `templates copy` themselves, which write no template's text.
- A project's own commands under `tools/`, which decision 0003's local commands govern.

## Revisit when

Teams come to override most of a generator's templates, which would argue for ejecting it instead;
the template engine of the .NET SDK comes to read conditions strictly over a declared vocabulary; or
a generator needs output an override cannot express as text.
