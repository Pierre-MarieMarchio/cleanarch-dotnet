---
type: guide
title: Maintaining the template
description: How to change the template and prove every generated shape still holds, from its gates to the rules of conditioned files.
---

# Maintaining the template

This repository builds, tests and runs as the default generation does: every condition is a comment
the compiler ignores. A change is done when the project's gate passes here and every generated shape
still holds.

## The template gates

`dotnet run --project launcher/CleanArch.Cli -- verify`, the project tool run from its source, runs the project's gate plus what only the template carries:

| Gate | Checks |
|---|---|
| `tools/CheckDocPaths.cs` | every repository path in a Markdown code span exists, and every `docs/` path a workflow, script or `.http` file names; `CHANGELOG.md` and the plans folder are exempt |
| `tools/CheckWorkflows.cs` | what a YAML parse misses: a dangling `needs:`, an action on a mutable tag, an image without a digest, a missing `permissions:`, an undeclared `$VAR`, a path absent from disk, a trigger filtered by `paths:` or `paths-ignore:`, a job-level `if:` on a pull request's matrix job, a job that runs `dotnet` on the runner image's SDK |
| `tools/CheckGitLabCi.cs` | what a YAML parse misses in a GitLab pipeline, the overlay's here and a generated project's at its root: a stage `stages:` does not declare, a dangling `needs:`, `dependencies:` or `extends:`, an image or a service without a digest, an include that is neither local nor GitLab's SAST or Secret Detection template, a job without `timeout:`, a job that runs `dotnet` on another image than the .NET SDK's, a path absent from disk |
| `tools/CheckComments.cs` | no comment outside the allowlist, one guidance line at a time in configuration files, no Markdown narrating the repository's history |
| `tools/WebLockfiles.cs --check` | each generated web shape's lockfile equals what the pinned pnpm writes for it; needs pnpm (`npm install --global corepack`, then `corepack enable`) |
| `openapi --check` | `src/Presentation/AT.Presentation.Client/Generated` equals what the pinned Kiota writes from the OpenAPI reference; needs the tools `dotnet tool restore` restores |

The launcher's own build is a gate too: its analysis row under [the launcher](#the-launcher) gives the rules it holds.

Each of the four `Check*.cs` gates runs its `--self-test` first, against fixtures that must make it
fire and fixtures that must leave it silent, and fails when it examined nothing. A rule added to one of
them comes with the fixture that fails without it. These four files and `tools/WebLockfiles.cs` are
excluded from generation, and `verify` runs them only
where `.template.config/template.json` exists, so one command serves both.

CI runs them in `.github/workflows/ci.yml`, under `templateMaintenance` blocks a generated project
does not receive. Its `Docs and workflows` job also runs
`dotnet run --project launcher/CleanArch.Cli -- docs-index --check`, since this repository's tool
manifest pins no `dotnet cleanarch`. Its end-to-end job skips its steps in this repository, whose stack includes web
apps no CLI has created yet, among them those that install the workspace and Chromium for the browser
smoke; the acceptance matrix runs the suite and the smoke instead.

A job that runs `dotnet` sets up the SDK `global.json` pins first, with `actions/setup-dotnet` and
`global-json-file: global.json`: the runner image's SDK changes under the workflow, and NuGet audit
judges the packages it carries. A job with no checkout has no `global.json` and names its SDK with
`dotnet-version`. `tools/CheckWorkflows.cs` reads a `run:` line as running `dotnet` when its first
word, past `VAR=value` assignments, is `dotnet`; a comment or `docker run <image> dotnet` starts
with another word, and a job with a `container:` runs its image's SDK, which the digest rule pins.

### The published package each week

`.github/workflows/template-published-package.yml` runs every Monday at 05:41 UTC, off the top of the
hour, and on demand. It installs the latest stable `CleanArch.WebApi.Templates` nuget.org serves,
in a scratch hive and without a checkout, generates the default project, bootstraps it, which restores
the project's local tools, `CleanArch.Cli` among them once the template pins it, from nuget.org, and builds it with warnings as errors, so NuGetAudit
judges the dependencies users receive today. Nothing else builds the published package between
releases: the acceptance matrix packs the checkout. A red run is an advisory published since the last
release, or a published package that no longer builds; the fix is a release that raises the
dependency, as [the project's policy](../guides/build-and-ci.md#an-advisory-fails-the-build) says.
A nuget.org holding no stable version fails the run too, since there is nothing for a user to install.
Like the nightly acceptance run, GitHub announces a red run only to the person who last changed the
cron line, by mail if their settings send workflow failures, and shows it under the Actions tab; look
there on Mondays. The workflow is excluded from generation and from the package, and
`rules.json` routes it to no leg.

## Migrations in this checkout

The template ships no migration, so this checkout has none until `dotnet ef migrations add
InitialCreate` creates them, once with `src/Infrastructure/AT.Infrastructure.Auth` and once with
`src/Infrastructure/AT.Infrastructure.Persistence` as both `--project` and `--startup-project`, as
`ci.yml` does; `bootstrap` would also create the web apps through their CLIs and rewrite the OpenAPI
reference here. The tests, `verify` and the drift check need them. They are git-ignored here only, by a `templateMaintenance` block of `.gitignore`, and
both `template.json` and `.template.config/TemplatePackage.csproj` exclude them, so a project
generated from this checkout or from its package starts without them; `TemplatePackagingTests` holds
the three. After a model change, delete each context's `Migrations` folder and run the command again. The
CI and release workflows run it before building, under `templateMaintenance` blocks.

## The acceptance matrix

`.github/workflows/template-acceptance.yml` packs the template package and `CleanArch.Cli` once, chooses the legs the
change can affect, and per leg installs that `.nupkg`, generates under another name, records the using directives it wrote, asserts the source name leaked nowhere, checks
its `cleanarch.json` against the leg's options with `.template.config/acceptance/CheckProjectFile.cs`, bootstraps
(the project's tool pinned from that packed `CleanArch.Cli`, which `dotnet cleanarch` then names
the project with, the formatting, which fails the leg if it removes one of those usings, as
[usings under a marker](#usings-under-a-marker) says, then `bootstrap` after asserting no migration was generated, and once more to
prove it creates nothing), builds with warnings as errors, runs `format` for what the build does not check, runs the Docker-free tests, checks every generated context for
pending model changes, runs the generated project's `docs-index --check`, and checks every path its pages cite
with this checkout's `tools/CheckDocPaths.cs`. The legs that set `integration` also run the
integration suites, on the runner's Docker through Testcontainers, and the defaults, SQL Server and
SQLite defaults legs then run the generated project's `dotnet cleanarch end-to-end` against
its Compose stack. The leg that sets `coverage`, the worker, MCP and Blazor WebAssembly leg with
external sign-in and every example, then runs the generated project's `coverage`, every suite
again under coverage, against the `coverage.minimum` it carries, which every shape shares and nothing
lowers to pass; its suites take under a minute, so the step adds about two minutes to a leg of about
seven and a half. The same leg generates `--code-host gitlab` and checks the GitLab files it writes,
as [code hosts](#code-hosts) says. The defaults leg, the local-auth leg without examples and the external-auth leg
without examples also run the generator round trip: `add feature` writes an owner feature, a shared one, an aggregate one and an
aggregate one on `get,list,create` in turn, the first and the third with `--fields` naming a field of
every type, nullable ones and bounds among them, the aggregates after `add use-case` adds a command, a
query, a command without input, a query with typed inputs and a command with the id and a body to
each while refusing the others. Each command makes its own edits, the account-deletion wiring of a
project's first owner feature among them, adds the feature's migration and rewrites the OpenAPI
reference and the client; the leg fails when a manifest records no edit or no migration was added.
The whole solution then builds with warnings as errors, the architecture tests pass, the feature's own
tests pass in every Docker-free test project, and the business context has no pending model change.
The next case starts from the commit generation made. Then all four are written together, `add field`
adds a required string and a required int, each with its default, and a nullable decimal to the
record and to the aggregate written with `--fields`, and the leg fails when it does not name the
breaking change, record the addition in the manifest, or give the stored rows each default in the
migration it adds; the two runs add about a minute and a half. `add setting` then adds a section with
a default to Invoices and `add event` an event nothing consumes, each recording its files and edits in
that feature's manifest. The four are proved the same way, and on the defaults leg, which has Docker,
the integration tests `--fields` and `add field` wrote run against PostgreSQL, about half a minute
more; `verify` passes. The tree is committed, and `remove feature --apply` takes the four out last in
first out, each from a committed tree, its edits undone and `Remove<Features>` added after
`Add<Features>`; the leg fails unless the tree then differs from the commit generation made by those
migrations and the `Add<Field>To<Features>` ones of `add field` alone, each with its designer file,
so the model snapshot, the references and every file an edit touched read as they did. The solution
then builds with warnings as errors and the model holds no pending change. Each removal checks the
model, builds once and adds its migration, under half a minute once the first build is warm, so the
four removals and the build after them add about seven minutes to the leg. A web leg also checks its
lockfile, adds a second app and a package with the item templates, scaffolds, runs the workspace's
install, lint, test and build, fails when an app with a build script has no built page, checks a
a second scaffold changes nothing, starts the whole stack and asks each app for its page, its
headers and an answer only the API gives; it fails when a served page holds an event-handler
attribute or an inline script the served `script-src` allows neither by its hash nor by the
script's nonce, or when two responses share a nonce, or when a server-rendered app with accounts
relays a `POST` without `X-CSRF` instead of refusing it itself, and reports its inline styles; a page that
loads the WebAssembly authentication script must carry the origin of `EXTERNAL_AUTH_AUTHORITY` in
its served `connect-src` and `frame-src`. The leg then installs Chromium, cached by `actions/cache` under the
Playwright release the lockfile resolves, and runs `packages/browser-smoke` over every app the stack
serves: it fails on a Content-Security-Policy violation, a console error, an uncaught exception or a
call through the `/api/` relay the API does not answer, which no `curl` above can see. `web.yml` never runs
it, since that job starts no stack.
The every-starter leg adds
each framework starter its first app is not, so one workspace proves them all together. Every app,
package and presentation project a leg adds with an item template is recorded by the project's
`add web-app`, `add web-package` or `add presentation`, and a web leg runs `ProjectFileTests` once more after its
last addition. Every leg's
`bootstrap` creates the presentation projects before the formatting, a step that does nothing without a presentation
project, and a web leg creates the WebAssembly app of `--web blazor-wasm` there too, so the build
compiles it and its tests run with the others. After the build, every leg checks that bootstrap left
whitespace and encoding unchanged; the `defaults` leg alone runs `format --check`, whose rules need a
compilation, as `ci.yml` runs it on the template, and, once the initial migrations and the OpenAPI
reference are written, the project's `upgrade-report` against this checkout packed again under the
next patch version, which may name no file but the version in `cleanarch.json` and in
`docs/getting-started/overview.md`; that is the one proof against a project that went through
bootstrap, since `IDE0055` can rewrite more than the usings, and it adds about half a minute. The three web legs that pass `--presentation
blazor-server`, one per authentication mode, create the presentation project, build and test it, and
ask it in the stack for its page or its redirect to the sign-in page, for the sign-in form under local accounts,
and for `/health`. The leg that adds a first app adds the presentation project too, with
`cleanarch-presentation`, makes the edits its `README.md` lists with the generated project's
`add presentation`, and from there runs as those three do. The legs that pass `--presentation worker`
build and test the worker with the rest; the generator leg among them writes the deleted-account sweep
into the worker, and the two that start the stack fail unless the worker, still running, logs a
completed pass within two minutes of the stack being up, and the API starts no job loop. Each of
those checks greps the logs captured whole, since `grep -q` closing a pipe early fails the pipeline
under `pipefail` and reads a logged pass as none. The legs that pass `--presentation mcp` with the API start the stack and
call the MCP server over raw JSON-RPC: it answers `/health` and refuses a call without a credential; under
an external provider its `401` names its resource metadata, which names the provider; under local
accounts the leg signs the seeded administrator in, creates an access key that writes and one that
only reads through the API, creates a note and reads it back through the tools, is refused a write
with the read-only key, revokes the key and is refused with it. The several-hosts leg starts the API,
the Blazor portal, the MCP server and the worker in one stack, and the leg without the API generates,
builds and tests a portal, an MCP server and a worker with no HTTP API at all. The several-hosts leg
also passes `--aspire true` and runs `LocalStackTests`, which starts the app host on the runner's
Docker and waits for each of its resources and the API's `/health`; the step adds about three minutes,
the build of the app host and its tests, the Aspire CLI that `dnx` fetches, and the start of four
hosts.

| Leg | Options |
|---|---|
| defaults | none |
| several hosts, local auth, notes | `--presentation blazor-server --presentation mcp --presentation worker --examples notes --files false --aspire true`, the API brought by the MCP server's access keys |
| local auth, no files, no ci, no sonar | `--auth local --examples none --files false --ci false --sonar false --presentation api --presentation worker` |
| worker, mcp and blazor wasm, external auth, all examples | `--presentation api --presentation mcp --presentation worker --auth external --examples all --files true --web blazor-wasm --code-host gitlab` |
| sql server, avalonia, external auth, all examples | `--database sqlserver --auth external --examples all --files true --presentation avalonia` |
| no api, blazor server, mcp and worker, external auth, no agent files | `--presentation blazor-server --presentation mcp --presentation worker --auth external --examples notes --files false --agent-files false` |
| external auth, no examples, no files | `--auth external --examples none --files false`, then `cleanarch-presentation --framework avalonia` |
| sqlite, external auth, all examples | `--database sqlite --auth external --examples all --files true`, then `cleanarch-presentation --framework mcp` |
| sqlite, defaults | `--database sqlite`, then `cleanarch-presentation --framework wpf` |
| web placeholder, sqlite, smallest project | `--web placeholder --database sqlite --auth none --examples none --files false --presentation blazor-server`, then `cleanarch-webapp --framework angular --rendering ssr` |
| web react back office, local auth | `--web vite-react --web-audience admin --presentation blazor-server --agent-files-example true`, then `cleanarch-webapp --framework blazor-wasm` and `add web-app` before the build |
| web angular, external auth, notes | `--web angular --web-rendering ssr --auth external --examples notes --presentation blazor-server`, then `cleanarch-webapp --framework blazor-wasm --auth external` and `add web-app` before the build |
| no web, then a react app, its workspace and an angular back office | `--code-host none`, then `cleanarch-webapp --framework vite-react --workspace`, `cleanarch-presentation --framework avalonia`, `cleanarch-webapp --framework blazor-wasm --has-client true` and `cleanarch-webapp --framework angular --audience admin --rendering ssr` |
| web, every starter | `--web blazor-wasm --examples none --files false`, then `cleanarch-presentation --framework avalonia --has-client true` and `cleanarch-webapp` once per pnpm framework starter, each on its own `--port`, and once more for React Router with `--rendering ssr` |
| windows, wpf, local auth | `--presentation wpf`, every other option at its default, on `windows-latest` |
| windows, wpf, external auth, notes | `--presentation wpf --auth external --examples notes`, on `windows-latest` |

A leg is one generated shape that carries as many guarantees as its options allow: every value of
every option, every item template and every check the workflow runs only for some legs is proved by
at least one leg, and two shapes share a leg unless one turns off a part the other must prove. An
item template adds a presentation project only to a project generated without one, since it writes
`docs/features/presentation.md`.
A leg stays under about twelve minutes, since the slowest one sets the run's length, and the account
runs at most twenty jobs at once, which the acceptance legs share with the other workflows.

Every leg also greps its project for the other database providers' packages and types, reading its own
`database`; the SQLite legs run their integration suites on a file and start no database container, and
the SQL Server leg starts one large image, so it has the longest timeout. A leg without a web app asserts no workflow, Dependabot entry, Compose file or ignore file names a
workspace. The leg that adds a first app passes `--code-host none`, so every join it runs lands in a
project without a code host's files and must write none there; it runs that assertion, then `cleanarch-webapp --workspace` and
`add web-app`, which makes the lines `src/Web/README.md` and the app's `README.md` list, and is a web leg
from there. `--workspace` only adds files, and
the engine refuses to overwrite one, so everything a web app needs from the API ships with every
project and the web jobs are `web.yml`, not jobs of `ci.yml`. A separate job asserts each refused combination exits non-zero and writes no file. The legs
are the shapes the options produce, not their cross product, and run with `fail-fast: false`.

A leg whose `runner` is `windows-latest` runs in the `template-windows` job instead, as a Windows
user runs the project ([which legs a change runs](#which-legs-a-change-runs)):
from PowerShell, one command a step, it installs the package, generates, checks its `cleanarch.json`,
asserts the source name leaked nowhere, then runs `bootstrap`, `build` with warnings as errors,
`test --no-integration`, `verify` and the citation check. A Windows runner runs no Linux image, so
the job starts no container and sets up no web workspace. It checks out the root files, `tools` and
`.template.config/acceptance` with the files beside it alone, since the package brings the template and a starter's deepest path exceeds the 260 characters
git for Windows allows by default; their `.gitattributes` keeps them LF whatever the runner's
`core.autocrlf`.

The legs are the entries of `.template.config/acceptance/matrix.json`: the fields the workflow's
steps read, and the `tags` that name what the leg generates. The two web legs with external
authentication add to their stack's `.env` the provider variables the API needs to start; no
provider runs in Compose, and the API fetches nothing from the one it names until a token arrives.

### Which legs a change runs

A pull request runs the legs its change can affect, and every other leg still reports
([template decision 0002](decisions/0002-a-pull-request-runs-the-legs-its-change-can-affect.md)).
The workflow's `Select the acceptance legs` job runs `.template.config/acceptance/SelectLegs.cs`
after its `--self-test`, on the paths `git diff --name-only --no-renames HEAD^1 HEAD` lists for the
pull request's merge commit. Each rule of `.template.config/acceptance/rules.json` maps path globs to
tags, to `none` (no leg reads the path) or to `all`; the first rule a path matches decides, a path no
rule matches runs every leg, and a leg runs when it carries a tag a changed path names. The
`template` job takes the Linux legs to run as its matrix and `template-windows` the Windows ones;
the `skipped` job reports each other leg green, whichever its runner, with one echo step, under the
same name, `Template acceptance (<leg>)`, the check the `develop` ruleset requires.

| A change to | Runs |
|---|---|
| `launcher/CleanArch.Cli`, `launcher/CleanArch.Cli.Commands`, `launcher/CleanArch.Tooling`: the project tool every leg runs | the `defaults` leg |
| `docs/template`, the plans, the rest of `launcher`, `README.md`, the package's readme, `.github/FUNDING.yml`, `CHANGELOG.md`, the template-only gates and tests, the release, pull request title and web lockfiles workflows, the release configuration | no leg |
| `src/Web`, `.github/workflows/web.yml` and `web-node-version.yml`, the `cleanarch-webapp` and `cleanarch-webpackage` item templates, the placeholder's lockfile | the six web legs |
| a framework starter under `.template.config/starters` | the legs that generate it: the every-starter leg alone for Vite and React Router |
| `.template.config/starters/blazor-server`, `.template.config/starters/avalonia`, `.template.config/starters/wpf`, `.template.config/starters/mcp`, the `cleanarch-presentation` item template | the legs that pass that `--presentation`, or add the project with the item template |
| `.template.config/starters/worker` | the legs that pass `--presentation worker` |
| a `.template.config/variants/database-<name>` overlay | that provider's legs |
| the `.template.config/variants/code-host-gitlab` overlay | the leg that passes `--code-host gitlab` |
| `src/AppHost`, its integration tests and `LocalStackParityTests` | the leg that passes `--aspire true` |
| anything else: `src`, `tests`, the shared pages, `tools`, the props, `template.json`, the other variants | every leg, the Windows ones included |

The `windows, wpf, local auth` leg generates the default shape with the WPF app added and nothing
else changed, so it proves the default project's tasks on Windows too. A team on Windows runs those
tasks from PowerShell, where paths, line endings and the processes a task starts differ from Linux,
so a run on `ubuntu-latest` proves none of them there; one leg holding both the default shape and the
Windows-only one spends the Windows runner minutes once, and it is required, so a break is found on
the pull request rather than by a nightly run after the merge. Its steps are PowerShell, one `dotnet`
command each, since that is what a Windows user types. Should Windows runner time come to weigh on
the pace of pull requests, this is the leg to revisit. The two WPF legs carry the
WPF starter's tag, as the Linux leg that adds the app with `cleanarch-presentation` does: a change to
the starter builds the app and its tests on Linux, where the leg fails unless `test --no-integration`
names the app's test project as skipped, and builds, tests and checks it on Windows under both kinds
of sign-in.

Every leg runs on a push to `main`, on a manual run, on the nightly run of `develop`, under the
`full-acceptance` label, on an empty or unreadable diff, and on a change to the selector, its rules,
the matrix or the workflow. A push to `develop` starts no acceptance run: its pull request ran the
legs its change could affect, and the nightly run proves the merged tree whole.

The nightly run starts at 02:17 UTC, off the top of the hour when GitHub delays scheduled runs, and
runs only when `develop` has moved: the `Has develop moved since the last scheduled run` step
compares the commit with the one of the last scheduled run that finished green or red, a cancelled
one not counting, and passes `--moved false` to the selector when they are the same; a list it
cannot read counts as a move. A red night is announced by GitHub alone: it emails the person who
last changed the cron line, if their
notification settings send workflow failures, and shows the run under the Actions tab of
`develop`; look there after a merge when no mail came. GitHub disables a scheduled workflow after 60
days without repository activity, which a `develop` that moves rarely can reach.

Labelling a pull request starts a run, so `full-acceptance` applies without a push. Any other label
starts a run that chooses no leg and reports none, in a concurrency group of its own, so it cancels
no run in progress; only `Pack the template package` and `Template refuses unsupported options` run
again, on the same tree. The jobs it skips report under the name `Template acceptance
(${{ matrix.name }})`, which no ruleset requires, so no required leg turns green unproved.

`AcceptanceSelectionTests`, a template-only rule, keeps the two files honest: the legs are the
checks the ruleset requires plus the every-starter leg, which it does not; every folder under
`.template.config/starters` and `.template.config/variants`, and each item template's, has a rule of
its own; every tag a rule names is a leg's, and every leg's tag a rule's; each leg's tags are the ones
its options choose; each leg's runner is one a job serves, a Windows leg sets only the fields
`template-windows` reads and generates no web app, and one Windows leg passes `--presentation wpf`
and nothing else; and every `none` path is left out of the template
package. `tools/CheckWorkflows.cs`
refuses a `paths:` filter, which leaves a required check pending, and a job-level `if:` on a pull
request's matrix job, which reports none of its legs when it skips them all, unless its matrix is read
from an earlier job and another job of the same name reports the legs it leaves out.

### Adding a leg

A new guarantee first looks for a leg whose shape it fits: options that turn off nothing the leg
proves, and a leg that stays under about twelve minutes. It gets a leg of its own only when none does.

1. Add its entry to `.template.config/acceptance/matrix.json`, with the fields its steps read and
   the tags its options choose, and `"runner": "windows-latest"` for a shape a Windows user must
   see run; `AcceptanceSelectionTests` fails until they agree.
2. Add the leg's name to the required list in `AcceptanceSelectionTests`, and once that pull request
   merges, `Template acceptance (<leg>)` to the required checks of the `develop` ruleset, which
   would otherwise hold every other pull request pending; a leg left optional, as the every-starter
   leg is, goes to that test's other list.
3. Give a folder the leg is the first to generate a rule in `.template.config/acceptance/rules.json`,
   and the leg its row in the table of legs above.

## Database providers

A project carries one provider ([decision 0001](decisions/0001-a-project-carries-one-database-provider.md)),
and none ships a migration: `bootstrap` creates each context's `InitialCreate` from the project's own
model. PostgreSQL is in-tree: compiled, tested and covered in this checkout.
Every other provider is a whole-file overlay under `.template.config/variants/database-<name>/`: each
file replaces the PostgreSQL file at the same path, with the same type names and public members, and
`template.json` excludes the PostgreSQL file under `(!postgreSql)`. The rest of the code, the domain
and the application never name a provider.

| Overlaid file | Supplies |
|---|---|
| `src/Infrastructure/AT.Infrastructure.Core/Common/Contexts/DatabaseProvider.cs` | `DesignTimeConnectionString`, `VersionColumnName`, both `UseDatabase` overloads (design time, and run time with `DatabaseOptions` and `ContextMigrations`), `ApplyDatabaseConventions`, `IsVersion`, `IncludingColumns`, `WhereColumnEquals`, `IsUniqueViolation`, `DedicatedConnectionString` |
| `src/Infrastructure/AT.Infrastructure.Persistence/Common/Leases/LeaderLease.cs` | `LeaderLease`, the `ILeaderLease` adapter, constructed from `IConfiguration` and a logger |
| `src/Infrastructure/AT.Infrastructure.Persistence/Features/Files/Adapters/Locks/OwnerQuotaLock.cs` | `OwnerQuotaLock`, constructed from the connection string and a logger, throwing `OwnerQuotaLockTimeoutException` after `WaitLimit` |
| `src/Presentation/AT.Api/Common/Observability/DatabaseInstrumentation.cs` | the provider's tracing and metrics, or none |
| `docker-compose.database.yml` | the `db` service and the `database-client` service whose environment gives the api `ConnectionStrings__Default`; SQLite declares only `database-client`, which mounts the `db-data` volume |
| `tests/Integration/AT.Api.IntegrationTests/Infrastructure/DatabaseServer.cs`, `tests/Integration/AT.Infrastructure.Auth.IntegrationTests/Fixtures/DatabaseServer.cs` | the test server: `Image` (one `docker-compose.database.yml` runs), `ConnectionString`, `StartAsync`, `DisposeAsync`, and `Table(schema, table)`, the name a test's raw SQL uses; the API one adds `TablesSql`, `ResetSql`, `SchemasSql`, `HistorySchemasSql(table)`, and `Value` and `Literal` for a `Guid` or a `DateTimeOffset` as the provider stores it; the Auth one adds `TransientFault` |
| `tests/Infrastructure/AT.Infrastructure.Core.UnitTests/Common/Contexts/DatabaseProviderTests.cs`, `tests/Infrastructure/AT.Infrastructure.Auth.UnitTests/Fixtures/DatabaseFaults.cs`, `tests/Infrastructure/AT.Infrastructure.Persistence.UnitTests/Features/Files/Adapters/Locks/OwnerQuotaLockTests.cs`, `tests/Infrastructure/AT.Infrastructure.Persistence.UnitTests/PersistenceModuleConnectionStringTests.cs` | the provider's own unit tests and faults; an overlay may leave one out by a `template.json` exclusion |

A provider without `xmin` registers `VersionStampingSaveChangesInterceptor` from its run-time
`UseDatabase`, which the modules call after the saving interceptors; `IsVersion` makes `Version` a
plain concurrency token. A provider without schemas adds `SchemaAsTablePrefixConvention` in
`ApplyDatabaseConventions` and names each history table with its `TableName`. Both are in-tree and
tested on SQLite here, and shipped only under `versionCounter` and `tablePrefixes`.

The shared files carry the lines of each provider this version generates: the package references of
`src/Infrastructure/AT.Infrastructure.Core/AppTemplate.Infrastructure.Core.csproj` and of the
two integration projects, the versions in `Directory.Packages.props`, the variables of `.env.example`,
and the `developmentConnectionString` switch that rewrites `appsettings.Development.json`. An overlay's
compose file uses the variable names `.env.example` gives it.

This version generates PostgreSQL, SQL Server and SQLite. MySQL is not a value of `--database`,
because the stable EF Core 10 providers cannot map the owned JSON entity Identity's passkeys use; no
page names a MySQL branch, and the `tablePrefixes` symbol covers SQLite alone.

A version that generates a provider adds its value to `template.json`: the choice, a computed symbol,
a source copying the folder under that symbol, its lines in the shared files above, and its place in
`refusedOptions` wherever a combination excludes it. While its overlay is unfinished the value
stays out of the choices, so no project is generated without it. `TemplatePackagingTests` fails until
the choice, the symbol and the source agree with the folder. Its legs in the acceptance workflow are
tagged `database-<name>` with the overlay's rule in `.template.config/acceptance/rules.json`, and each
leg's provider-leak check reads the leg's `database`.

An overlay file starts as a copy of its PostgreSQL counterpart, so `git log` on both paths finds a
change one of them missed.

## Code hosts

A project carries the files of one code host, or none
([decision 0005](decisions/0005-a-project-carries-one-code-hosts-files.md)). GitHub is in-tree: the
`.github` folder at the root, and the project's own issue forms and pull request template in
`.template.config/variants/project/.github`, run by this repository's CI and held by
`WorkflowPinningTests`, `IssueFormTests` and `PullRequestTemplateTests`. Every source that writes
under `.github` carries `githubHost`, or `githubCi` for a workflow, so `--code-host gitlab` and
`none` write no `.github` path, and the web app's workflows follow the project's `--code-host` and `--ci`, which
`add web-app` and `add presentation` pass from `cleanarch.json`.

| Symbol | Reads |
|---|---|
| `codeHost` | the host chosen: `github` by default, `gitlab`, or `none` |
| `githubHost` | `codeHost == "github"`: the `.github` folder, Dependabot, the issue forms, the pull request template |
| `gitlabHost` | `codeHost == "gitlab"`: the overlay under `.template.config/variants/code-host-gitlab/`, Renovate, the merge request and issue templates |
| `anyCodeHost` | `codeHost != "none"`: a host whose pipelines `--ci` can write |
| `githubCi` | `ci && githubHost`: the GitHub Actions workflows |
| `sonarWorkflow` | `githubCi && sonar`: the Sonar steps of `ci.yml` |
| `gitlabCi` | `ci && gitlabHost`: the GitLab pipeline, `.gitlab-ci.yml` in the overlay |
| `sonarPipeline` | `gitlabCi && sonar`: the Sonar steps of the GitLab pipeline |

A page conditions a line that names a host's file on that host's symbol, and gives `none` a whole
sentence that holds without it. Another host comes as a whole-file overlay under
`.template.config/variants/code-host-<value>/`, its value kept out of the choices until the overlay is
complete; `TemplatePackagingTests` fails until every choice is GitHub's tree, `none`, or a folder of
its own, and until every overlay folder is written once, under a `<value>Host` symbol that reads
`codeHost == "<value>"`. An overlay whose value is not a choice yet is unfinished: no project can
receive it, yet the package carries it, since `dotnet new` refuses a template whose source folder is
missing. Its rule in `rules.json` runs the `defaults` leg, whose project a condition gone wrong would
change, until the change that offers the value gives it the tag of the leg that generates it. Its pipeline holds the same gate as `ci.yml`, and is proved by the options of a leg that
fits it rather than a leg of its own. GitLab's overlay is complete and `gitlab` a choice: its rule
runs the leg "worker, mcp and blazor wasm, external auth, all examples", which generates
`--code-host gitlab`.

GitLab's overlay holds the merge request and issue templates under `.gitlab`:
`merge_request_templates/Default.md`, which GitLab fills a new merge request's description with, asks
for the sections of the pull request template; `issue_templates/Bug.md` and `Feature.md` turn each
field of the issue forms into a section, point to `SECURITY.md` as the forms do, and label the issue
with a `/label` quick action. GitLab reads no form and no required field from a file, and no file
refuses a blank issue there as `config.yml` does on GitHub, so the templates say nothing of it.
`MergeRequestTemplateTests` and `IssueTemplateTests` hold them in a GitLab project, which alone
receives them (`template.json` excludes both under `(!gitlabHost)`), and read the overlay in this
checkout; `TemplatePackagingTests` holds them to the GitHub files, the sections of the pull
request template and every field and label of the forms, so the two hosts cannot drift. A quick action
adds a label only where the project has it: GitLab creates none, as GitHub does `bug` and
`enhancement`.

The overlay's `.template.config/variants/code-host-gitlab/.gitlab-ci.yml` is GitLab's pipeline: a
modifier of the overlay's source leaves it out unless `gitlabCi`, and `template.json` excludes
`GitLabPipelinePinningTests` under `(!gitlabCi)`. It holds the gate of `ci.yml`: the `build-test`,
`hygiene` and `vulnerable-packages` jobs, on the .NET SDK image the Dockerfiles build with, the
Sonar steps under `sonarPipeline`, and GitLab's SAST and Secret Detection templates in place of
CodeQL, the only includes allowed to float. Under `api` it holds the other jobs of `ci.yml` too:
`docker`, `load`, `end-to-end`, which needs `compose`, and `compose` in every shape. Every job that
runs containers extends the hidden `.docker`: the `docker:<version>-dind` service over TLS, with
`TESTCONTAINERS_HOST_OVERRIDE`, `STACK_PUBLISH_ADDRESS` set to `0.0.0.0` and `STACK_HOST` set to
`docker`. The Compose files publish every port on `${STACK_PUBLISH_ADDRESS:-127.0.0.1}` and the
end-to-end suite and the browser smoke reach the stack on `STACK_HOST`, `localhost` and `127.0.0.1`
without it, so a developer's stack is unchanged and the service's is reached through its host name;
`StackAddressTests` holds both in every project, and the browser smoke's own test the host it reads.
`load` and `end-to-end`, which run the Docker CLI on the SDK image, extend `.docker-client`, which
installs the CLI, the Compose plugin and the Buildx plugin Compose builds with, from Docker's static
release, each pinned by version and SHA-256. `docker` and `web-image` run on the
`docker:<version>-cli` image. Under `files`, `end-to-end` sets `STORAGE_PUBLIC_ENDPOINT` to the
service's host, since the object store signs the host it is reached on, and `.dockerignore` leaves
out the `.cache` folder the browser is cached in, which GitLab's cache keeps inside the project.

`.template.config/variants/code-host-gitlab/.gitlab/ci/web.yml` is the counterpart of `web.yml`: the
`web` job, on the SDK image with Node.js from its release archive at the version
`src/Web/.node-version` names and pnpm from corepack, as the web images get it; `web-image`, which
finds the apps by folder as `web-apps` does and builds them one after the other, since a pipeline
cannot fan a job out over the folders it finds; and the part of `end-to-end` a web app adds, the
workspace and Chromium for the browser smoke, which GitLab merges into the root file's job of the
same name. The root file includes it under `rules: exists`, so a project without a web app holds
none and the item template adds it, under `(workspace && gitlabCi)`, without editing the root file;
the overlay's source leaves it out under `(!anyWeb)`, as `template.json` does `web.yml`. The item
templates define `gitlabHost` and `gitlabCi` as the project template does, so offering the value adds
the choice alone.

`.template.config/variants/code-host-gitlab/.gitlab/ci/release.yml` is the counterpart of
`release.yml`, present where it is: the overlay's source leaves it out under `(!serverDatabase)`, and
the root file includes it under the same marker, with a workflow rule that lets a `v*.*.*` tag run.
Its jobs run on a tag alone, pushed or picked in Run pipeline, which stands for the workflow's `tag`
input; every job of the gate extends the hidden `.gate`, whose rules keep it off a tag, as `ci.yml`
runs on none, so a release runs the gate once, in `verify`. `migration-bundle` keeps the bundles as
artifacts for the 90 days `retention-days` keeps them, and `container-image` fans out over a
`parallel: matrix:` of `COMPONENT` and `DOCKERFILE`, as the workflow's matrix does, which the worker's
join edits as it edits `release.yml`. It builds on the Docker-in-Docker service with the binfmt,
BuildKit and SBOM scanner images `release.yml` pins, pushes to `$CI_REGISTRY_IMAGE/<component>` with
the tags and labels `docker/metadata-action` computes (but the licence, which GitLab does not expose),
and stands for `attest-build-provenance` with a keyless `cosign sign` of the index digest, which
carries BuildKit's SBOM and provenance, through Sigstore and the job's `SIGSTORE_ID_TOKEN`. The
workflow's GitHub Actions cache has no counterpart: a release builds without one rather than push a
cache image beside the release's.

`.template.config/variants/code-host-gitlab/.gitlab/renovate.jsonc` is the counterpart of
`.github/dependabot.yml` and of `web-node-version.yml`: Renovate reads `.gitlab/renovate.jsonc` on
GitLab alone, and a `.jsonc` file takes the `//` comments and the trailing commas the template markers
leave. Each Dependabot ecosystem is a manager, `gitlabci` standing for `github-actions`; each group is
a `packageRules` entry with its `groupName` and its patterns, then its exclusions negated, in
`matchPackageNames`; the folders Dependabot reads alone, those of the Dockerfiles and of the Compose
files, are a rule that disables the manager and one that enables it in `matchFileNames`, which the
worker's and the web workspace's README edits extend as they extend `dependabot.yml`. The schedule
opens Monday at 06:00 Paris time and stays open until 10:00, for a run that starts late; the limits
are added up into `prConcurrentLimit`, since Renovate counts every open merge request against one;
`separateMajorMinor` is off, as Dependabot opens one pull request per dependency or group at its
newest version; `prHourlyLimit` is off and the dashboard too. `applies-to: version-updates` needs no
counterpart: Renovate leaves a vulnerability fix out of every group by default, and on GitLab it gets
none, since its vulnerability alerts read GitHub's and `osvVulnerabilityAlerts` is experimental. The
`templateMaintenance` lines of `dependabot.yml`, `target-branch: develop`, have none either, since no
GitLab project is this repository. Two rules go past Dependabot, each because CI fails without it: the
`node` group moves the web images' `node`, `src/Web/.node-version` through the `nodenv` manager, and
`@types/node`, within the major and across it in another merge request, which is the job
`web-node-version.yml` does on GitHub; and the .NET SDK image moves in every Dockerfile and the
pipeline together, since `GitLabPipelinePinningTests` holds them to one image. Three take less:
`global.json`'s SDK band, pnpm's `packageManager` and the `overrides` of `pnpm-workspace.yaml` stay
by hand, and so does every image or archive a pipeline script names, as on GitHub. Renovate cannot
update a checksum it has not computed, so the Node.js archive of `.gitlab/ci/web.yml` is bumped by hand
in the `node` merge request, which `GitLabPipelinePinningTests` fails until it is.
`RenovateParityTests`, template-only, resolves the markers of both files for every combination of the
symbols they read and fails until each ecosystem, folder, group, pattern, exclusion, label, commit
prefix, schedule and limit of `dependabot.yml` has its counterpart and nothing more, through a reader
of `packageRules` that applies them in order, as Renovate does; it also holds the README edits to the
blocks the worker and the web workspace write. `RenovateConfigurationTests`, which a GitLab project
receives under `(gitlabHost)`, holds the `node` group and the SDK image to the files the project
holds. `renovate-config-validator`, run on a generated project's file, is the schema's check; the
project runs Renovate through the Mend-hosted app on GitLab.com or the `renovate-runner` template from
a project of its own, so its pipeline holds no Renovate job.

`AGitLabProject_RunsEveryCommandOfTheGitHubGate`, in `TemplatePackagingTests`, reads the `dotnet`
commands of the three gate jobs of `ci.yml`, past its `templateMaintenance` blocks, and fails until
the GitLab job of the same name runs each one, so the two gates cannot drift.
`AGitLabProject_RunsEveryCommandOfTheGitHubStackWebAndReleaseJobs` does the same for `docker`, `load`,
`end-to-end` and `compose` of `ci.yml`, every job of `web.yml` and every job of `release.yml`: each command of a `run:` step whose
program it pairs, its literal assignments, such as k6's version and checksum, `continue-on-error` and
`needs`, a matrix's entries, and, for an action, what it stands for: the SDK image for `setup-dotnet`,
the Node.js of `.node-version` for `setup-node`, corepack for `pnpm/action-setup`, the cached folder
for `actions/cache`, the `docker build` that `build-push-action` runs, or the `docker buildx build`
with its platforms, SBOM and provenance when it pushes, the binfmt image for `setup-qemu-action`, the
BuildKit image of a builder that pushes for `setup-buildx-action`, a registry login for
`login-action`, each tag and label for `metadata-action`, the artifact's name, paths and retention
for `upload-artifact`, and a checked cosign and its `sign` under a Sigstore ID token for
`attest-build-provenance`; an action it does not know fails it.
`AGitLabProject_RunsOnATag_WhatTheGitHubWorkflowsRunOnOne` reads the tags each workflow's `on:` names
and fails until the root file's workflow rules let each one run, as a regular expression, and a run
from Run pipeline for a workflow with `workflow_dispatch`, until every job paired with `release.yml`
runs on a tag alone, and until every job paired with `ci.yml` or `web.yml` runs on none, its own
`rules:` read before those it extends, as GitLab reads them. `GitLabPipelinePinningTests` holds the
images, services and includes of a GitLab project's pipeline and of every file under its `.gitlab/ci`
folder, an image a script runs with `docker run` or names in an `image=` or `generator=` option among
them, its local includes to a file or an `exists:` guard, and its SDK image to the Dockerfiles',
reading the overlay in this checkout; `tools/CheckGitLabCi.cs` checks the rest, the included files
merged into the root file as GitLab merges them, and that the job that signs keylessly with cosign,
and it alone, takes a `SIGSTORE_ID_TOKEN` for the `sigstore` audience. This repository's CI never runs the pipeline, so the leg that generates
`--code-host gitlab` checks the GitLab files it writes instead: `tools/CheckGitLabCi.cs` on the
project, `check-jsonschema` against the schema GitLab publishes for its pipeline files,
`renovate-config-validator` on `.gitlab/renovate.jsonc`, and `gitlab-ci-local --preview` and `--list`,
which merge every include and job as GitLab does, on a committed copy of the project. Its rule in
`rules.json` covers `tools/CheckGitLabCi.cs` too. Under `--aspire true`, GitLab's `build-test`
removes the app host's integration test project from the solution before `dotnet test`: the app host
reaches the containers it starts through the Docker host's loopback, which a Docker-in-Docker service
keeps to itself, and a class filter would leave the project with no test, which the test platform
fails. A project's `cleanarch.json` records `codeHost` as it was chosen, and
`adopt` reads `gitlab` back from `.gitlab-ci.yml` or a `.gitlab` folder at the root.

Two limits stay with the project: a GitLab project creates its `bug` and `enhancement` labels itself,
as `docs/getting-started/first-run.md` says, and a keyless `cosign sign` needs a Sigstore instance
that trusts the issuer of the job's ID token, which the public one does for GitLab.com alone, as
`docs/guides/deployment.md` says.

## Adding an option

An option removes, it never disables: what it leaves out is absent from the generated project, its
code, tests, settings, pages and ADRs, so a project has no run-time flag to discover and no dead
branch to delete. The cost is that every option multiplies the shapes to prove, which the legs and
the refusal job pay. The `dotnet new` engine knows no constraint between parameters, so a
combination that cannot hold is refused by a hidden parameter named after the rule
(`authNoneNeedsExamplesNone`, `mcpNeedsAccounts`): its only choice is `refused`, its `isRequired`
is the combination, and its description names the option to change, which `dotnet new` prints when
it stops. The launcher evaluates that same `isRequired`, so a refusal keeps this shape and
`refusedOptions` stays the union of the conditions; the launcher's tests fail on any combination
where the two disagree. An engine that gains constraints between parameters would replace the
hidden parameters.

An option that names parts takes several values with `allowMultipleValues`, as `--presentation`
does: the CLI repeats it once per value (`--presentation api --presentation mcp`; `api|mcp` and
`api,mcp` are refused), and a condition reads `presentation == "mcp"` as "mcp is among the values"
and `!=` as "not among them", in source modifiers, markers, generated switches and `isRequired`
alike. One multi-valued option answers "which hosts" at once, where one boolean option per host
would ask the same question six times.

1. Declare the parameter in `.template.config/template.json` with its `displayName`, and a computed
   symbol when pages and code need a name for a combination. A hidden option or a long name goes in
   `dotnetcli.host.json`. A visible option gets its label in `ide.host.json`, which Visual Studio
   reads and which hides every symbol it does not list; `TemplatePackagingTests` refuses a refusal
   shown or an option missing there. Its place there is the order the launcher asks in: what the
   project holds, then how callers sign in, then its data, then its tooling, so that an answer
   leaves out what it rules out in the questions after it.
2. Exclude whole paths with a source modifier under a condition; condition content inside a file in
   that file's own comment syntax.
3. A combination that cannot hold together is a hidden required parameter, and the same condition in
   `refusedOptions`, so the `#error` variant catches a hand-passed bypass.
4. Add the leg that generates the new shape, as [adding a leg](#adding-a-leg) says, and a refusal
   to the refusal job.
5. Condition every page whose truth changes, and judge each in every form it can take.
6. An option that is not a part joins `cleanarch.json`, as [the project file](#the-project-file)
   says.

## Writing docs that condition

Project pages reach every generated project, so each is written for each shape. Inside Markdown:

```markdown
<!--#if (files)-->
The object store keeps the bytes.
<!--#endif-->
<!--#if (!files)-->
No object store runs.
<!--#endif-->
```

- A page opens with its frontmatter, which holds in every generated form: no marker inside it, and
  none between it and the page's `# ` heading. A marker goes after the heading. The pages under
  `docs/template/` take `type: guide`, its decisions `type: decision`, and
  `DocumentationFrontmatterTests` holds both.
- The index of each folder, `docs/<folder>/index.md`, is what `docs-index` writes from the pages a
  tree holds, so it needs no marker and a page's `title` and `description` hold none. In this
  checkout it lists every branch's pages and the template's; `template.json` and the package leave
  it out, which `TheCheckoutsDocumentationIndex_ReachesNeitherAProjectNorTheTemplatePackage` holds,
  and bootstrap writes a project's own from the pages its form keeps.
- Use only real symbols of `template.json`, negated with `!` for the other branch; never a bare
  else, elseif or a marker inline in a sentence.
- A branch holds whole sentences, whole list items or whole table rows; a table's header stays outside
  any branch that can leave rows without it.
- A marker between two rows of a table, the `|---|` line counted as a row, is written as a row of
  one cell, `| <!--#if (x)--> |`: a marker on a line of its own there ends the table GitHub renders,
  and every row after it shows as text. Generation removes the row whole, as it removes the line;
  GitHub shows an empty row in its place. One marker per row, never an `endif` and an `if` on the
  same one. A marker before the header or after the table keeps its own line.
  `NoMarkerOnALineOfItsOwn_EndsAMarkdownTable` holds it.
- A marker inside a fence indented under a list item is indented to the column of that fence: written
  at column 0, it ends the list item on GitHub, the fence closes there and the rest of the page
  renders out of place. Generation removes the indented line whole, as it removes any marker line.
  `NoMarkerInAnIndentedFence_SitsLeftOfTheFence` holds it.
- Every form keeps exactly one blank line between two blocks of text: put the blank line inside the
  branch that needs it, at its start, rather than around its markers. `ConditionedMarkdownTests`
  fails on a page that keeps two in a row in any form.
- No marker inside a mermaid fence. A diagram shows the full shape, unconditioned, and says so.
- A page about an absent part is excluded by a path condition, not emptied by markers; its links from
  shared pages are conditioned the same way.
- `AGENTS.md` is the only agent file, and `--agent-files false` leaves it out: a page that points to
  it conditions that sentence or row on `agentFiles`, and points to the page that owns the rule
  otherwise. Keep `AGENTS.md` concise and pragmatic, and link to a page rather than restate it: it
  renders at most 125 lines in every form, and `src/Web/AGENTS.md` at most 60, which
  `AgentFileLengthTests` holds. Its closing section, `## This project`, belongs to the project's team:
  the template ships prompts there, or example lines under `--agent-files-example true`, and writes
  nothing else in it. No join edits the root `AGENTS.md`, so a line of it never depends on a part a
  project can gain later; a join edits `src/Web/AGENTS.md` alone.
- A link from a shared file (`CONTRIBUTING.md`, `AGENTS.md`, `SECURITY.md`) to `docs/template/` sits
  in a `templateMaintenance` block, which is always false at generation.
- A project ADR the template supersedes stays here, with its `Superseded by` status, and reaches no
  project: `template.json` excludes it under `(!templateMaintenance)`, the package project leaves it
  out, and the index lists it in this checkout alone, where the file is. The ADR that replaces it
  names it in prose, never by a link, since a project lacks it.
- A shared file conditions a line on a part a project can gain later (a web app, a presentation host,
  the worker) only when the item template that adds the part rewrites the file or its join edits it;
  otherwise the project that gains the part reads otherwise than one generated with it, and
  `launcher/CleanArch.Create.Tests/EndToEndTests.cs` fails. The issue forms and the pull request
  template under `.github` stay true in every form that has them instead.
- Text that names a token (`AppTemplate`, `app-template`, the web app tokens) is replaced in a
  generated project; write a token only where its replacement reads right.
- Project pages say "this project"; only `docs/template/` and the repository `README.md` say "the
  template".

Read a page in the default form, in `--auth none --examples none --files false`, and in each form its
markers distinguish: no broken link or anchor, no marker left, no orphan punctuation, no page about an
absent module.

## Usings under a marker

A using stands under the condition of the lines that read what it brings in, no wider: a using kept
in a form where nothing reads it is IDE0005, which fails that form's build, warnings being errors.
The legs build some forms only, and bootstrap's formatting would remove such a using before their
build sees it, so two checks hold the rule.

| Check | What it judges | Where |
|---|---|---|
| `tests/Architecture/AT.Architecture.Tests/Rules/ConditionedUsingTests.cs` | every form `template.json` can generate, a leg's or not: each combination of options it does not refuse, the files its sources and modifiers write for it, and the lines of each C# file its markers keep, `//#if` and `/*#if ... #endif*/` alike; a using of a project namespace no kept line reads fails, naming the file, the namespace and the options of a form that keeps it | template-only, in the architecture tests |
| `Record the using directives the template wrote`, then `Bootstrap` | the forms the legs generate, every using alike: a using bootstrap's IDE0005 removes from a file the template wrote fails the leg | `.github/workflows/template-acceptance.yml` |

The test judges a namespace by the names the template's own C# files declare in it, types, their
attributes' short names and extension methods, and a line as reading a name it spells. It reads the
files under `.template.config/variants` and `.template.config/starters` as the forms that write them,
and the client Kiota writes in `src/Presentation/AT.Presentation.Client/Generated` for the names it declares only. It
leaves out what it cannot know: a namespace from a package, and the projects an official CLI creates,
whose `scaffold.json` has the kind `dotnet`; the acceptance check covers those in the shapes the legs
build.
A form the test reports runs with the options it names: generate it and build it to see IDE0005.

## Web sources

Two folders under `src/Web/apps` are the source of both `--web` and `cleanarch-webapp`:
`PlaceholderSite`, the static page, and `FrameworkSite`, the wiring every framework app shares (its
`Dockerfile`, `nginx.conf`, `compose.yml` and `README.md`). The project template keeps the one the
choice needs and renames it from `--web-name`, the item template renames it from `-n`. Each name is a
token in three spellings (`PlaceholderSite`, `placeholder-site`, `PLACEHOLDER_SITE`, and the same for
`FrameworkSite`). Where one framework differs, the wiring says so by a marker on its own symbol:
Angular's inline styles in `nginx.conf`, its `NG_CLI_ANALYTICS` in the `Dockerfile`, React Router's
hash step and client folder in the `Dockerfile`, and the sentences of the `README.md` that name the
CLI.

The app itself comes from a starter, one folder per `--web` value under `.template.config/starters`
([template decision 0004](decisions/0004-a-web-app-comes-from-a-starter-static-or-server-rendered.md)): `angular`,
`vite-react`, `vite-vue`, `vite-svelte` and `react-router`, each holding
`FrameworkSite/scaffold.json` and `FrameworkSite/overlay/`, which both templates copy into the app's
folder by the framework chosen and the scaffold step consumes at bootstrap. Neither template copies anything else from there. A
framework has one value: React and Vue come from Vite's CLI, so `vite-react` and `vite-vue` hold
them, and no second value creates the same framework another way.

A starter whose generator also renders on the server holds that form in `ssr/FrameworkSite/`, its
own `scaffold.json` and `overlay/` ([template decision 0004](decisions/0004-a-web-app-comes-from-a-starter-static-or-server-rendered.md)):
`angular` does, with `ng new --ssr`, and `react-router`, with its `node-custom-server` template. Both templates copy the starter's folder without `ssr/`, leave
its `FrameworkSite/` out under `webServerRendering`, and copy `ssr/` under the starter's symbol and
`webServerRendering`; the shared `Dockerfile` then ends on Node, and `nginx.conf` is left out.
`--web-rendering ssr`, and `cleanarch-webapp --rendering ssr`, are refused for every other starter
by `serverRenderingNeedsAServerStarter`.

Adding a starter, for a CLI that keeps the contract of [template decision 0004](decisions/0004-a-web-app-comes-from-a-starter-static-or-server-rendered.md):

1. Write `.template.config/starters/<value>/FrameworkSite/scaffold.json`: the CLI's package, its
   exact version, its arguments (`{name}` and `{directory}` are replaced; the CLI runs with its input
   closed, so every answer is an argument), the JSON merges that make it build into `dist/`, relay
   `/api/` and `/health`, name the package `framework-site` and give it the
   `@app-template/api-client` dependency and a `test` script; and `overlay/` for a file the CLI's own
   settings cannot reach, a Vite config being one. The workspace's scripts run once and rewrite
   nothing: a `test` that watches or a `lint` that fixes is merged into one that does not, and Vitest
   is pinned to the version `src/Web/packages/api-client/package.json` uses. Import the client from
   a scratch file of the created app once and run its type-check: the workspace packages export
   their sources and import each other by `.ts` path, so a compiler setting the CLI leaves off, such
   as `allowImportingTsExtensions`, is a merge too. A file the starter does not need, such as the
   CLI's own `Dockerfile`, is named in `remove`. List the inline scripts of the built page: a page
   with none keeps the shared policy; a page with some needs the hash step of the `Dockerfile`, which
   React Router has, extended to the new symbol (and to its page's folder if the builder does not
   write it at the root of `dist/`). Never `'unsafe-inline'` for scripts, never a nonce.
2. Add `<value>` to the choices of `web` in `.template.config/template.json` and of `framework` in
   `.template.config/webapp/.template.config/template.json`, each description naming the generator
   and its template, with its computed symbol, a source copying the starter folder into
   `./src/Web/apps/` under that symbol, and the symbol reached from `webFramework`: through
   `webVite` for a Vite-based starter, which also gives it port 5173 in `webDefaultPort` and
   `defaultPort`; directly otherwise, with its own port in both. `TemplatePackagingTests` fails until
   both templates agree with the folder. A framework that already has a value gets no second
   one.
3. Condition the wiring only where the new framework differs, and the README sentences that name
   its CLI and list what the starter adds. Give the value its row in
   `docs/getting-started/overview.md` and `docs/template/using-the-template.md`, and its name where
   `docs/features/web-frontend.md`, `src/Web/README.md` and the repository `README.md` list the
   values.
4. Give it an accent colour in `launcher/CleanArch.Tooling/Prompts/GuidedConversation.cs`; its
   question comes from the manifests.
5. To offer its server form, write `ssr/FrameworkSite/scaffold.json` and its `overlay/`, whose server
   relays through `relayToApi` of `@app-template/api-client/server`, sends the page headers, and
   renders with a nonce per response; add the folder's source and the two modifiers of its main
   source to both `template.json` files, and its value to the condition of
   `serverRenderingNeedsAServerStarter` and `refusedOptions`. `TemplatePackagingTests` fails until
   they agree with the folder, and a web leg generates the server form.
6. Add it to the every-starter leg of the acceptance matrix, with a port of its own, which
   scaffolds, builds and serves it beside the others and refuses an inline script its served policy
   does not allow by hash; a starter that differs in the wiring gets a leg of its own as well, unless
   the every-starter leg already proves what differs, as it does for React Router's hash step. Its
   folder's rule in `.template.config/acceptance/rules.json` names the tag `starter-<value>`, which
   each leg that generates it carries in `.template.config/acceptance/matrix.json`.

## Presentation starters

`--presentation` names every host a project holds, one or several, `api` by default
([adding an option](#adding-an-option)).
Each value but `api` is a starter, of kind `dotnet` or, for WPF, the
MCP server and the worker, `written`, copied under its value's symbol
(`presentationBlazorServer`, `presentationMcp`, `presentationAvalonia`, `presentationWpf`,
`jobsWorker`, which `needsWorker` also sets). `needsApi` brings the API with what calls it, a web app, a desktop client or the MCP
server under local accounts, and `api`, the API chosen or brought, conditions every part of it: its
project and tests, the end-to-end suite, the load smoke, the `.http` file, its Compose service, the
tasks around the OpenAPI document and the stack, the architecture rules and pages about the HTTP
surface. `needsWorker` brings the worker to a project whose hosts hold neither the API nor the
worker, so that some host runs the jobs; `workerNeedsServerDatabase` refuses the worker, chosen or
brought, with SQLite, and `desktopIsOneFramework` Avalonia and WPF together.
`.template.config/starters/blazor-server` holds the project's own files at their paths in a project:
`.template.config/starters/blazor-server/src/Presentation/AT.PresentationAppPortal` (its `scaffold.json`, project file,
`Program.cs`, settings, `Dockerfile` and wiring), and its test project beside them under `tests/`; the
project template copies the folder into the project's root under `presentationBlazorServer`.
`.template.config/starters/avalonia` holds a desktop client's the same way, copied under
`presentationAvalonia`, which sets `dotnetClient`: it keeps the files `dotnet new avalonia.mvvm`
writes at its root and in `Assets`, `ViewModels` and `Views`, owns the project file, `Program.cs`,
`App.axaml.cs`, the main view model and window, and references the .NET client alone.
`.template.config/starters/mcp` holds an MCP server written whole,
copied under `presentationMcp`: its `scaffold.json` is of kind `written`, in the shape of
`dotnet new mcpserver --transport remote`, so the scaffold step only adds it to the solution;
it owns the project file, `Program.cs`, the settings, the image, the caller's authentication (an
access key under `localAuth`, the provider's token under `externalAuth`) and one tools class per
example feature, each excluded with its feature. Under local accounts the access keys its callers
present come with it: `accessKeys` (`localAuth && presentationMcp`) keeps their files in the Auth
context and the API and their tests, leaves them out otherwise, and conditions the lines
they add to shared files; `cleanarch-presentation` brings the same files and its `README.md` lists
those lines for `add presentation`.
`.template.config/starters/wpf`, copied under `presentationWpf`, which sets `dotnetClient` too, is
written whole: the WPF app is one project on `net10.0-windows`, with `EnableWindowsTargeting` so that it builds on
every system, laid out as the Avalonia starter's: `App.xaml`, `MainWindow.xaml` and their code-behind,
`ViewModels/`, the settings and the host under `Common/Hosting` and the session under
`Common/Sessions`, referencing the .NET client alone. It adds `System.IO` and `System.Net.Http` to
its usings, which `UseWPF` takes out of the implicit ones, so the files it shares with the Avalonia
starter compile unchanged. Its one test project, on `net10.0-windows` too, runs on Windows alone,
and the tasks name it as skipped elsewhere.
Its hosting, session, settings and tests are a copy of the Avalonia starter's, at the same paths and in the same
namespace: `TheWpfStartersSharedCode_IsTheAvaloniaStartersOwn_ByteForByte` fails until a
change to one copy is made to the other, and a third desktop client is the time to move them into a
project of their own.
Each starter carries a token of its own, so that two hosts never meet on a path, a service or a port:
`PresentationAppPortal` for Blazor, `PresentationAppMcp` for the MCP server, `PresentationAppDesktop`
for Avalonia and WPF, each with its kebab-case (`presentation-app-portal`) and upper-case
(`PRESENTATION_APP_PORTAL`) forms. The project template replaces them with the hosts' names, `Portal`,
`Mcp` and `Desktop`, by constant symbols, and `cleanarch-presentation` with `-n`. The portal answers
on 8090 and the MCP server on 8091; both read the settings every host shares from
`x-host-environment` in `docker-compose.yml`. A starter's C# is compiled only in a generated project,
so a change to it is proved by generating one: Blazor's by the acceptance legs that pass `--presentation blazor-server`,
Avalonia's by the leg that adds it with `cleanarch-presentation`, WPF's by the two Windows legs and
the SQLite leg that adds it, the MCP server's by the legs that pass `--presentation mcp` and
the SQLite external-auth leg that adds it with `cleanarch-presentation`.

A starter's kind follows what its generator writes. A `dotnet` starter runs the template package its
`scaffold.json` pins, at that exact version, in a hive of its own under the system's temporary
folder, with the SDK's own templates disabled: the SDK bundles its own copy of Microsoft's templates,
which is not the pinned version and refuses another beside it, and the engine names a lock after the
hive's path. The starter still owns the project file, the composition root, the settings and the
image, since editing the generator's XML would add back what the starter already knows. A starter is
`written` when its generator writes nothing the project would keep, or cannot be pinned: running it
would cost the network and a package for an output thrown away. A written starter follows the shape
its generator gives at the version its descriptor names, needs no network and gives the same project
on every machine; a change in the generator's output is noticed only by generating with the new
version and comparing. The checkout's solution never names a starter's project, which the checkout
cannot build. `TemplatePackagingTests` holds that each folder under `.template.config/starters` is a
web starter or a presentation one, that a presentation starter is of kind `dotnet`, with the template
it runs, or `written`, with the generator it follows and a project file of its own, and
`EveryPresentationChoice_IsAStarterTheTemplateCopiesByIt` that each value but `api` is one. A value
names its generator and never changes meaning: `blazor-server` is `dotnet new blazor` with server
interactivity and no authentication of its own.

| Starter | Kind | Generator | What the project keeps of its output |
|---|---|---|---|
| `blazor-server` | `dotnet` | `dotnet new blazor` | `Components` and `wwwroot` |
| `avalonia` | `dotnet` | `dotnet new avalonia.mvvm` | the files at its root and in `Assets`, `ViewModels` and `Views` |
| `mcp` | `written` | `dotnet new mcpserver --transport remote` | nothing: the starter owns the project, `Program.cs`, the settings and the tools, and the sample tool goes |
| `wpf` | `written` | `dotnet new wpf`, which ships with the SDK alone, nuget.org stopping at 3.0.0, so no hive can pin it | nothing: the starter owns the project, the window and the code-behind, and `App.xaml.cs` starts the host before any window |
| `worker` | `written` | `dotnet new worker` | nothing: the starter owns the project, `Program.cs` and the settings, and the sample worker goes |

The MCP server needs an authenticated caller: `mcpNeedsAccounts` refuses `--auth none` in both
templates, since every tool acts for the caller its credential names and `--auth none` names nobody;
a server acting for nobody would be refused by every owner-scoped use case and answer anyone who
finds the address otherwise. `cleanarch-presentation` keeps `Portal` as `-n`'s default, since the
engine fixes a default before it reads `--framework`, so its README passes `-n Mcp`.

The WPF app is one project rather than view models in a `net10.0` sibling whose tests Linux could
run: that would shape every WPF project around the template's runners, which its users do not have,
and give the scaffold, the item template and the architecture rules a case of their own. Off Windows
the tasks leave its test project out and print `Skipped <folder>: it targets <framework> and runs on
Windows alone.`, rather than setting `IsTestProject` to `false`, which would leave it out in
silence; they read the target framework from the test project's own file, which
`WindowsTestProjectTests` holds, rather than evaluating every test project with MSBuild on each
`check`.

`cleanarch-presentation`, in `.template.config/presentation/.template.config`, copies the same
folder by `--framework`, with `docs/features/presentation.md`; for `avalonia` and `wpf` it also brings
`src/Presentation/AT.Presentation.Client`, `tests/Presentation/AT.Presentation.Client.UnitTests`, which a project without a client lacks. For `worker`
it brings no page, refuses every `-n` but `Worker` (`workerIsNamedWorker`), a
project without the API (`workerTakesTheJobsOfTheApi`) and SQLite (`workerNeedsServerDatabase`), and
the project's `add presentation` moves the jobs, as [the worker](#the-worker) says. `--has-api`, true by default, says whether the project holds the
API: its computed `api` conditions the README's lines, and `presentationNeedsTheApi` refuses a
desktop client, or the MCP server under local accounts, in a project without it. It cannot read the project it runs
in, so it declares what the starter's markers read as parameters and computed symbols of the same
names, values, defaults and refusals as `template.json`; `-n` replaces the starter's token and
`--project-name` replaces `AppTemplate` in the two spellings the source name gives it, the name and
its lower-case namespace, and its initials replace the folders' token, as [project folders](#project-folders)
says. `ThePresentationItem_ReadsTheProjectsOptionsAsTheProjectTemplateDeclaresThem`
fails when a symbol the starters read is missing or differs, or when the two templates leave out
different files. What `--presentation` writes outside the project's folders, an item template
cannot edit: the starter's `README.md` lists each of those lines under "Joining a project generated
without one", as an edit placed after or before the lines around it, or in their place, and
`TheStarterReadme_ListsEveryLineItsStarterWritesOutsideItsFolders` fails, for each
starter, until a line a branch of its symbols writes is there: `presentationBlazorServer` or
`anyPresentation` for Blazor, `presentationAvalonia`, `dotnetClient` or `anyPresentation` for
Avalonia, `presentationWpf`, `dotnetClient` or `anyPresentation` for WPF, `presentationMcp` or `anyPresentation` for the MCP server, `jobsWorker` for the worker but the lines of a project without the API, outside the files the item template brings whole and the solution, which
the scaffold step of `add presentation` completes. The Blazor WebAssembly starter's `README.md` answers to the same
rule for `webBlazorWasm` and `dotnetClient`, read by the project's `add web-app`, and
`TheWebReadme_ListsEveryLineItsAppOrItsWorkspaceWritesOutsideItsFolders` holds it for
`src/Web/apps/FrameworkSite/README.md` (the framework symbols and `webServerRendering`),
`src/Web/apps/PlaceholderSite/README.md` (`webPlaceholder`) and `src/Web/README.md` (`anyWeb`, the
lines of a first app's workspace, which `add web-app` makes before the app's while `cleanarch.json`
records no workspace), each leaving to the others the lines their symbols write. For an app added to
a workspace, the overview, the first run and the web front end page stay as they are, since they
describe the app the project was generated with; a first app's workspace brings the web front end
page and the end-to-end tests of the web apps whole.
Under `externalAuth` the WebAssembly app has code of its own, and its unit tests come from
`.template.config/variants/blazor-wasm-sign-in/`, which both templates copy to the project's root,
since the starter's folder lands in `src/Web/apps/`; the scaffold step adds them to the solution with
the app. A shared file that gains such
a branch gains its edit in the README, with an anchor that is unique in every shape. The project's
`add presentation` reads that section and makes the edits, every one or none, so a bullet keeps
the form it reads: ``- in `<path>`, after|before|replace|remove ...:``, then the anchor's fenced
lines, then the added ones. A fence of four backticks or more holds lines that contain a fenced
block of their own, and closes at a fence of at least its length, as CommonMark has it. A page a part brings, its feature page or its ADR, needs no edit to be
listed: `add presentation` and `add web-app` run `docs-index` once their edits are made, and the
folder's index lists the page as it lists those of a project generated with the part, so no shared
file under `docs/` conditions a listing of pages on a part.

the scaffold step of `add presentation` installs the template package `scaffold.json` pins into a hive of its own,
runs `dotnet new` with its input closed and, when the template offers `--allow-scripts`, its
post-action scripts declined: `dotnet new` asks again for ever on a closed input, and accepts the
option only from a template that has such a script, so one that would ask fails instead of waiting
for an answer. It keeps the folders and files the descriptor names, a file the starter holds
winning, and refuses a generated project file that declares a package or a property the starter's
lacks, but a package the descriptor's `drop` names: one only a file the starter replaces uses. It adds the project and its tests to the solution, each in the solution folder its path gives (`src/Presentation`, `tests/Presentation`). A project that references the .NET client
brings the client and its tests into the solution with it, and, once the OpenAPI reference exists, `openapi` writes the client. The package is pinned by hand: bump its
`version` with the ASP.NET Core runtime image of the `Dockerfile`s, generate a project with
`--presentation api --presentation blazor-server`, scaffold it, and commit what the scaffold now requires: the
starter keeps its own `Components/App.razor`, a copy of the one the template writes without its `<ImportMap />`, so
resynchronise it with the new version's, and drop that component again.

`presentationNameIsTaken` refuses each `-n` of `cleanarch-presentation` that would make
`<Project>.<Name>` or its test project one of the template's own projects, but `Worker` for the
worker itself;
`ThePresentationItemsNameRefusal_NamesEveryProjectTheTemplateWrites` lists the projects under `src`
and `tests` and fails until a new one is refused.

## The app host

`--aspire` adds `src/AppHost/AT.AppHost`, an Aspire app host, beside Docker Compose, which stays the
reference ([when](../guides/running-the-app-host.md#when)). Its source is in-tree, built
and tested in this checkout like `src/Presentation/AT.Presentation.Client`, and `template.json` leaves it out with its
integration tests, `LocalStackParityTests`, and its page under `!aspire`; `ProjectFileTests`
reads it as absent here. It starts the hosts and the web apps `cleanarch.json` records rather than
a list of its own, so `add presentation` and `add web-app` need no edit of it, and its C# conditions
only on the options a project cannot change: the database, the authentication, Files and the
examples. A host's settings are the ones `docker-compose.yml` gives it, and its secrets Aspire
parameters.

| Rule | Held by |
|---|---|
| Every Compose service is a resource under the same name, and the reverse; an exception is listed with its reason | `LocalStackParityTests.EveryComposeService_IsAnAppHostResourceOfTheSameName_AndEveryResourceAComposeService` |
| Each container runs the image its service pins, which the app host reads from the Compose files | `LocalStackParityTests.EveryContainerTheAppHostPulls_RunsTheImageItsComposeServicePins` |
| Each `Section__Key` a Compose file sets, the app host sets | `LocalStackParityTests.EverySettingComposeGivesAHost_TheAppHostGivesToo` |
| Each web app listens on the port its `compose.yml` publishes, which `cleanarch.json` records | `LocalStackParityTests.EveryWebAppTheAppHostStarts_ListensOnThePortItsComposeServicePublishes` |
| `global.json` pins the SDK under `msbuild-sdks` at the version of every `Aspire.*` package | `LocalStackParityTests.TheAppHostSdk_IsPinnedInGlobalJson_AtTheVersionOfEveryAspirePackage` |
| The whole stack starts, healthy | `LocalStackTests`, Docker, in the several-hosts leg |

In this checkout the parity test reads the Compose services of the shapes the defaults leave out as
absent, in a `templateMaintenance` block, since the checkout's `docker-compose.yml` holds every branch.
The dashboard and the orchestrator come from the Aspire CLI that the SDK's version pins, which `dnx`
fetches on the first build (`AspireUseCliBundle` with `DnxPinned`): Aspire warns on the NuGet
packages it used before, and this project keeps no such silence. The app host names
`Aspire.AppHost.Sdk` without a version and `global.json` pins it under `msbuild-sdks`, the form
Dependabot's NuGet updater rewrites, so its `aspire` group raises the SDK and the packages in one pull
request; a project generated without the app host receives `.template.config/variants/no-aspire/global.json`,
which `TheGlobalJsonWithoutTheAppHost_IsTheCheckoutsMinusItsMsBuildSdks` holds to the checkout's.
`aspireNeedsServerDatabase`
refuses SQLite, whose file the app host's processes cannot share with Compose's volume.

## The worker

`--presentation worker` moves the jobs out of the API, and without the API
it is the host that runs them. The worker's own files
are a starter written whole, `.template.config/starters/worker`, as [presentation starters](#presentation-starters)
says: its project file, `Program.cs`, settings, `Dockerfile`, caller identity and the compositions of the
directories its passes read, with its test project and `WorkerCompositionTests`. Its `scaffold.json`
is of kind `written`, so the scaffold step only adds the two projects to the solution and
formats them. The jobs themselves stay where they are in this checkout, the API's `Common/Jobs` and
`Features/*/Jobs` with their unit tests: under `jobsWorker` the API's source leaves them out, or the
API is left out whole, and two
sources of `template.json` copy them under the worker and its tests. Each namespace they declare has
a generated switch symbol, `jobsNamespace<Folder>`, whose value is the API's or the worker's; a job
folder a new feature brings needs one, and `TemplatePackagingTests` fails until it has it. The API's
own files condition what composes the jobs (`AddBackgroundJobs`, `AddAuthMaintenanceJobs`, the
sweep's registration and the `Jobs` settings) on `!jobsWorker`. The renaming goes through switch
symbols because a replacement given as a custom operation is not applied to the files a glob selects;
an engine that applied a replacement to one source's files alone would make them unnecessary. The
jobs live once, in the API, rather than copied into the starter, where the two copies would drift,
or in a library both hosts reference, a project every generated project would carry only to move it.

The worker has a fixed name, `<Project>.Worker`: `cleanarch-presentation --framework worker` takes no
`-n` but `Worker` (`workerIsNamedWorker`), and `presentationNameIsTaken` refuses `Worker` as the name
of any other presentation project. It needs a server database (`workerNeedsServerDatabase`): an
in-process lease cannot keep the API's maintenance routes out of the worker's pass, and a SQLite file
serves one process.

A project generated without the worker takes it later: `cleanarch-presentation --framework worker
-n Worker` writes the starter, and the project's `add presentation` does what the
template's sources and symbols do at generation. It moves the API's `Common/Jobs` and
`Features/*/Jobs`, and the `Features/*/Jobs` of its unit tests, into the worker and its tests;
renames each namespace they declare, in every file of the project, from `<Project>.Api.` to
`<Project>.Worker.`, the rule `EveryNamespaceTheWorkerTakes_HasItsSwitchSymbol` holds the switch
symbols to; and makes the edits the starter's `README.md` lists for the lines conditioned on
`jobsWorker`, which `TheStarterReadme_ListsEveryLineItsStarterWritesOutsideItsFolders`
holds. It refuses before writing while git reports a change to a file it would move, edit or remove,
builds the solution and the worker's tests in a copy of the project with the join made, and writes
nothing when that build fails, naming what a job uses that only the API declares. A line no option of
the item can tell apart stays out of that branch: the API-alone sentence of the background jobs page
is conditioned on `!jobsWorker` alone, so that no presentation project's README needs it, and a test
literal that names a job namespace names the worker's, which the rename leaves alone. A launcher test
generates a project with `--presentation api --presentation worker` and the same project without the
worker, adds it with the item template and the join, and requires the two trees to be the same once
`docs-index` has written each tree's index as bootstrap would.

## Web lockfiles

The repository's `src/Web/pnpm-lock.yaml` locks every app at once, so it is excluded from generation.
`dotnet run tools/WebLockfiles.cs` writes the lockfile of each shape holding a `package.json`
into `.template.config/variants/web-<App>/src/Web/`; a framework app gets none, since the scaffold step
writes it where the app is created. Run it after any change to the union lockfile; `--check`
fails until you do.

On every pull request, the `Web lockfiles` job of `.github/workflows/template-web-lockfiles.yml` runs
it, uploads the lockfiles it changed and fails, naming the command. On a Dependabot pull request
of the web workspace, `.github/workflows/template-web-lockfiles-commit.yml` then commits them to the
pull request's branch:

| Facet | How |
|---|---|
| Trigger | `workflow_run` on the failed run, kept only for a `pull_request` run Dependabot started on a `dependabot/npm_and_yarn/` branch of this repository |
| What runs with the write token | nothing from the pull request: no checkout, only `gh api` over the uploaded files, each a regular file at a lockfile path the branch already holds |
| Commit | made through the API on the commit the lockfiles were written for; the branch moves only if it still points there, so a newer push wins and its own run writes them |
| Checks | a commit made with `GITHUB_TOKEN` starts no workflow, so the checks run on the next event: update the branch with `develop`, which a merge requires anyway, or close and reopen the pull request |

The run that writes the lockfiles has a read-only token and runs the pull request's code, as any pull
request's checks do; the run that writes to the repository runs none of it, which `pull_request_target`
checking out the branch would. A personal token or an app would start the checks at once, at the cost
of a secret that can push, which this repository does not hold.

## The .NET client

`src/Presentation/AT.Presentation.Client` and its tests are built and tested in this checkout, and reach a
generated project only when `dotnetClient` is true, which `--presentation avalonia` and
`--presentation wpf` set, or through `cleanarch-presentation --framework avalonia` or `wpf`. Its session is written under `localAuth`, its
validation problem under `localAuth || anyFeature`, the two shapes whose document holds them.
`dotnetClient` also brings the Kiota entry of the tool manifest; the `openapi` command, the CI step
that runs `openapi --check`, the Dependabot group and the `Generated` exclusions are in every
project and act once `src/Presentation/AT.Presentation.Client` holds the client. `Generated/` is copied as Kiota wrote it:
`template.json` gives `**/Generated/**.cs` a conditional syntax no line uses, so Kiota's own `#if`
lines survive and only the names are replaced. The tool manifest therefore comes in five shapes:
`.config/dotnet-tools.json` with every tool but the project's own, which this checkout runs from its
source, `project` under `.template.config/variants/`, which replaces it in a generated project and
adds `cleanarch.cli` at `0.0.0-checkout`, the version `templateVersion` replaces at generation, and
`no-sonar`, `no-client` and `no-sonar-no-client` there too, each the `project` manifest minus its
tools, which `EachToolManifest_IsTheGeneratedProjectsMinusItsTools` holds. A Kiota bump changes the
checkout's manifest, `project` and the `no-sonar` one, and its pull request runs `dotnet cleanarch openapi`
and commits what it writes.

## The project file

`cleanarch.json` records what a project was generated with: the template package and version, the
project's name, every option that is not a part under its value, `client` when `src/Presentation/AT.Presentation.Client`
holds the .NET client, the web workspace with its apps and packages, and the presentation project
([template decision 0003](decisions/0003-a-project-runs-its-tasks-from-a-pinned-dotnet-tool.md)).
This checkout holds the file the defaults generate; `dotnet cleanarch` finds the root by it and reads
the options there rather than through markers.

| Facet | Where |
|---|---|
| Values | one generated symbol per recorded value, whose `replaces` names the key with the checkout's value, such as `"auth": "local"`, so that no other file holds the token; the name, the version, the scope and the part names follow the tokens the rest of the tree uses |
| Parts | `.template.config/variants/project-file-web`, the checkout's file plus the web workspace, copied in its place by `anyWeb` |
| Additions | `add presentation` and `add web-app` record the part they join, `client` with it, and `add web-package` a package of the workspace; every writer keeps the key order and any key it does not know |
| Tests | `ProjectFileTests` holds the file to the tree in every project; `TemplatePackagingTests` holds the checkout's file to the defaults, each variant to it, and every value to a symbol that replaces it; `CheckProjectFile.cs` holds each leg's file to its options |

In this checkout `src/Web`, `src/Presentation/AT.Presentation.Client` and the API's external-identity mapping are the sources of
shapes the defaults leave out, so `ProjectFileTests` reads them as absent, in a `templateMaintenance`
block. A new option that is not a part gets its key in `cleanarch.json` and in the variant,
a generated symbol that replaces it, its place in the list of `.template.config/acceptance/CheckProjectFile.cs`, and a check in
`ProjectFileTests` when the tree shows it.

## Project folders

A project's folder carries the project's initials in place of its name
([the folder map](../reference/project-layout.md#the-folder-map)), so
this checkout's folders start with `AT.` and its project files with `AppTemplate.`. The two
generated symbols `projectFolderPrefixAnyCase` and `projectFolderPrefix` compute the initials from the
name and replace `AT.` in every path and file; `cleanarch-presentation` and `cleanarch-webapp`
declare the same two from `--project-name`. The tasks, the generators, the launcher and the
architecture tests each compute a folder with a `SegmentInitials` of their own, since a file-based
program shares no code.

| Rule | Test |
|---|---|
| The three templates compute the same initials as the tools, for names with and without capitals | `TemplatePackagingTests.TheFolderSymbol_GivesTheProductsInitials_AsTheToolsComputeThem` |
| Every `SegmentInitials` reads as the architecture tests' own | `TemplatePackagingTests.EveryCopyOfTheInitials_ReadsAsTheArchitectureTestsOwn` |
| The token stands only before a project's folder, at the start of a word, in every path and file a template writes | `TemplatePackagingTests.TheFolderToken_NamesNothingButAProjectFolder` |
| Each template refuses exactly the names under two characters, which hold every name whose folders differ from its projects in case alone, and exactly the names that do not start with a letter from A to Z, which hold every name without initials | `TemplatePackagingTests.TheNameRefusals_RefuseEveryNameWhoseFoldersTheSolutionCannotHoldForItsProjects` |
| No path under `src` and `tests` passes 170 characters under the default name | `TemplatePackagingTests.TheLongestPathUnderSrcAndTests_StaysWithinItsBudget_UnderTheDefaultNameAppTemplate` |

## The generators' shared code

The generators of the project tool, `launcher/CleanArch.Cli/Generators`, share one copy of what they
hold in common under `launcher/CleanArch.Cli/Generators/Shared`: `TextEdits.cs`, the edits in files a
generator does not own, placed beside a line or computed on the whole text, and the write of every
file or none; `FeatureManifest.cs`, a feature's manifest read in either schema version, its files,
hashes, edits and the fields `add field` added merged, and written; `CSharpNames.cs`, a name in
PascalCase and the C# keywords it must not become once lowered; and `FieldGrammar.cs`, the field
model with the subset of each field profile; `TemplateOverrides.cs`, which text renders each template
of one generator in one project. A change to a shared unit is made there once.

`add setting --keys` reads its own grammar: a key is a bounded configuration value with a default,
`duration` and `secret` among its types, which no field has; its names follow `CSharpNames`.

### The template engine

The templates of `add feature` and `add field` are the files of the reference feature: each lies
under `launcher/CleanArch.Cli/code-templates/add-feature` at the path `add feature` writes it at in
a project named `AppTemplate`, whose project folders are `AT.<Part>`, for the feature `Placeholders`
of the type `Placeholder`, with `.txt` appended; a file written into each host lies at the path of
`AT.Api`. One file of a feature comes from one template, and a template writes one file. The tool
embeds the folder under its own name, `ToolTemplates.Of` reads a generator's templates by that path,
and the plan in `launcher/CleanArch.Cli/Generators/Shared/FeatureCode.cs` names each template by it:
`FeatureCode.PathOf` derives the file's path in the project, the project's folder for `AT.<Part>`
and the generation's names for the reference names, and never the reverse.

A project overrides a template with a file at the same path under its own `code-templates` folder,
`code-templates/add-feature/<reference path>.txt` at its root. `TemplateOverrides.Read` reads that
folder once per run, its files ending in `.txt` and not starting with a dot, and `Render` renders the
project's text where there is one and the tool's otherwise: an override replaces text, and never
chooses a path, a flag or code to run. Its BOM, its line endings and one final line break are
dropped as `ToolTemplates.Normalize` drops the tool's, so an unchanged copy writes the same bytes.
A file naming no template of the generator, a file that cannot be read, and an override the engine
refuses are each a problem that starts with the override's path, and the generator refuses and
writes nothing; the same refusal from the tool's own text is a defect of the tool, and throws. The
manifest of a feature written from an override records it under `templates`, each path with the
SHA-256 of its normalised text, and the report lists it under `Written from this project's code
templates:`; a feature written from the tool's text alone has neither, so its manifest and report
are those of a project without overrides.

`add field` reads the overrides once and renders from that one reading both the feature its manifest
records and the feature with the fields added, so the two renders it compares never come from
different texts. An override whose digest is not the one the manifest records under `templates`,
changed, added or removed since, is a warning on the error output, starting with `warning: `, and
not a refusal: the files are compared with what `add field` writes today, so a member the difference
reaches reads as changed by hand. Once written, the manifest records the overrides that run used.
`remove feature` reads nothing under `templates` and leaves `code-templates` as it is.

`Template.Render`, in `launcher/CleanArch.Cli/Generators/Shared/Template.cs`, renders the templates
of `add feature` and `add field` in five passes, in this order:

1. `Directives` keeps the lines of the `#if`, `#elif` and `#else` branches the generation's flags
   select, and drops the directive lines.
2. The reference names `AppTemplate`, `Placeholders`, `Placeholder` and `placeholder` become the
   project, feature, aggregate and lowered aggregate names, wherever they appear, inside a word or
   in prose: a template that needs the word `placeholder` for itself cannot have it. They are
   replaced before the blocks, so a field named after one keeps its name.
3. `TemplateBlocks` repeats each block once per field of the feature.
4. Each `%Token%` becomes the code the generator computed for it. A `%Token%` still in the text
   afterwards, one the generation does not set, is refused with the template's name, as
   `'%Plural%' is a token this generation does not set`, instead of being written as code.
5. A using marked `//?<Types>` is pruned when no line after the namespace names one of its types,
   the usings are sorted, and the blank lines are tidied.

The BOM and the line endings are fixed afterwards, by `TextEdits`. The directives and the
conditions read lines and flags and nothing of C#.

A `%Token%` carries code the generator computes, in the middle of a line, where a directive cannot
stand. A template without a token, written with `#if`, the reference names and C# alone, is C# that
a compiler and an editor read, as `PlaceholderErrors.cs.txt` is; a template that carries a token is not. This
is why the syntax is the C# preprocessor's and not a template language such as Scriban or Liquid,
whose syntax no compiler or editor understands.

### Directives and conditions

A directive is a line whose first non-blank characters are `#if <condition>`, `#elif <condition>`,
`#else` or `#endif`; `#else` and `#endif` take nothing after them. An `#if` anywhere else in a line
is text. Directives nest, and an `#if` in a branch that does not hold is still read, so a typo in a
dead branch is refused as well. `Condition` reads a condition with this grammar, where a flag is one
of the vocabulary of the generator that renders the template:

```text
or    := and ('||' and)*
and   := unary ('&&' unary)*
unary := '!' unary | '(' or ')' | flag
```

`&&` binds tighter than `||`, as in C#, and parentheses group without being required:
`#if get || list` and `#if (get) && (list)` both read. The `when` of each file in the plan of
`launcher/CleanArch.Cli/Generators/Shared/FeatureCode.cs`, such as `"record && takesContent"`, is read
by the same `Condition`, over the same vocabulary.

`add feature` and `add field` refuse a template that holds one of these, with a message that starts
with the template's path, `code-templates/add-feature/<reference path>.txt`, and the line's number, and names
the offending text:

- a condition naming a flag outside the generator's vocabulary, another generator's included, so a
  generated file cannot carry a real `#if DEBUG`;
- an empty condition, an operator without its operand, a parenthesis left open or never opened, and
  any other text, such as a single `&` or `|`, or `==`;
- an `#elif`, `#else` or `#endif` that belongs to no `#if`, an `#elif` or `#else` after the `#else`
  of its `#if`, an `#if` never closed, and text after `#else` or `#endif`.

### The flags of add feature

Each generator that renders templates owns the vocabulary of their conditions: a `FlagSet`, which
names each flag with its meaning, and which `Template.Render` and `Condition` take from the
generator. A template names only the flags of its own generator; any other is refused as a typo is.
The vocabulary of `add feature`, which `add field` renders again, is `FeatureFlags.Vocabulary` in
`launcher/CleanArch.Cli/Generators/Shared/FeatureFlags.cs`, and `FeatureCode.Flags` sets the flags
that hold for a generation. Its flags are the words of the options where an option decides one,
and otherwise say what the feature does:

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

The table is the vocabulary, row for row: a flag renamed, added, removed or reworded changes both.

### Blocks over fields

A block repeats its body once per field of the feature, so the loops, separators and indentation
of the code written per field are in the template. A block on lines of its own reads:

```text
//#each field [where <trait>] [separator "<text>"]
        <body>
//#end ["<tail>"]
```

The same block inside a line is written `/*#each field ...*/<body>/*#end*/`. `where <trait>` keeps
the fields that have the trait, `separator "<text>"` joins the fields with the text, `\n` standing
for a line break, where a line block otherwise joins them with a line break and an inline one with
nothing, and a quoted tail after `#end` follows the last field. A selection with no field writes
nothing, its tail included. Inside a block, `%field.<Token>%` is a value of the field; the tokens
and the traits are those of `FieldBlocks`, and a block naming another, or one left open, is refused.
Blocks do not nest, and since the directives are read first, an `#if` inside a block is decided once
per generation, not once per field. A field token may write a `%Token%` of the feature, such as
`%Limits%`, which the fourth pass replaces.

```text
//#each field separator ",\n"
        %field.Type% %field.Parameter%
//#end ") : base(id)"
```

| Guarantee | Test |
|---|---|
| A condition reads as C# reads it, `&&` binding tighter than `||`, with or without parentheses | `ConditionTests.ACondition_ReadsAsCSharpDoes_AndBindsAndTighterThanOr` |
| A condition naming a flag outside its vocabulary, empty, left dangling, unbalanced, or holding any other text is refused, the message naming the offending text | `ConditionTests.ACondition_RefusesWhatItCannotRead_NamingTheOffendingText` |
| Each of `#if`, `#elif` and `#else` keeps only its own lines, at every depth | `DirectivesTests.ABranch_KeepsItsOwnLines_AtEveryDepth` |
| A line that does not start with a directive is text, an `#if` in the middle of a line included | `DirectivesTests.ADirective_StandsAtTheStartOfALine_AndAnythingElseIsText` |
| A directive without its `#if`, after its `#else`, never closed, followed by text, or naming an unknown flag, even in a dead branch, is refused with its line's number and text | `DirectivesTests.ADirective_RefusesAMisplacedOrUnreadableLine_NamingItsNumberAndText` |
| `Template.Render` runs the passes together: the branches and the reference names, an unknown flag refused with its line, a block written once per field it keeps, joined by its separator and followed by its tail, a block that keeps no field writing nothing, its tail included, and a block naming an unknown token, an unknown trait or an item other than `field`, a block left open, and a `%field.<Token>%` outside any block, each refused, the message naming the offending text | `NewFeatureTests.TemplateGuard` |
| Each template that holds no `%` token parses as C#, as the compiler reads its `#if` lines, under each combination of the flags it names, and `PlaceholderErrors.cs.txt` is one of them | `NewFeatureTests.TemplateGuard` |
| A template naming the flag of another generator is refused, and renders under that generator's vocabulary | `NewFeatureTests.TemplateGuard` |
| A `%Token%` the generation does not set is refused, not written as code | `NewFeatureTests.TemplateGuard` |
| The table of the flags of `add feature` on this page is its vocabulary, each flag with its meaning, and nothing else | `FeatureFlagsTests.ThePage_ListsEachFlagOfAddFeature_WithItsMeaning_AndNoOther` |
| The output of `add feature` and `add field` does not change when a template changes its form | `NewFeatureTests.WithoutFields`, `NewFeatureTests.WithFields`, `NewFeatureTests.FieldAdditionOutput` |
| A project's override of a template writes its own file, and changes no other | `NewFeatureTests.OverrideWinsForItsFileOnly` |
| A copy of every template, unchanged, a BOM and CRLF line endings among them, writes the bytes the tool's text writes, and the manifest records it with the digest of the tool's text | `NewFeatureTests.AnUnchangedCopyWritesTheSameBytes` |
| A file under `code-templates/add-feature` naming no template is refused, and nothing is written | `NewFeatureTests.AnOverrideNamingNoTemplateIsRefused` |
| An override with an unknown flag, an unknown field token, an unknown token or an `#if` never closed is refused, the message starting with its path and naming the line where the engine names one, and nothing is written | `NewFeatureTests.ABrokenOverrideIsRefusedNamingIt` |
| The manifest records each override used, by path and digest, in path order, and a feature written without one records no `templates` | `NewFeatureTests.TheManifestRecordsEachOverride` |
| A dot file under `code-templates/add-feature` is not read as a template | `NewFeatureTests.DotFilesAreIgnored` |
| `add field` renders the feature before and after its fields from the same override, an override's block writing the added field as `add feature` writes it | `NewFeatureTests.AddFieldUsesTheOverrideForBothRenders` |
| `add field` warns, and still writes, when an override is not the text the manifest records, then records the text it wrote from | `NewFeatureTests.AddFieldWarnsWhenTheOverrideChangedSinceAddFeature` |
| `remove feature` removes a feature written from an override as its manifest records it, and leaves `code-templates` as it was | `RemoveFeatureTests.AFeatureWrittenFromAnOverride_IsRemovedAsRecorded_AndTheOverrideStays` |
| Every template the tool embeds for `add feature` is written by one of the configurations the digests with `--fields` cover, or by an owner feature in a tree whose callers sign in with an external provider | `NewFeatureTests.EveryTemplateIsWritten` |
| Over the configurations the digests cover, every C# file `add feature` writes or edits, and every one `add field` writes or changes, parses without a syntax error; an edited file is parsed as the edit leaves it in the tests' tree, whose shared files are cut down to the code around the anchors, and the message names each group and path that wrote the same broken text | `NewFeatureTests.WithoutFields`, `NewFeatureTests.WithFields`, `NewFeatureTests.FieldAdditionOutput` |

## The package

`.template.config/TemplatePackage.csproj` packs the four templates into one NuGet package,
`CleanArch.WebApi.Templates`; its `PackageId` is the one place that names it. nuget.org is the feed
every page names, since GitHub's NuGet feed asks for a token to install even from a public
repository, which a template's first command cannot ask for; the release mirrors the packages to
GitHub Packages only so that the repository page's Packages panel lists them, skipping a version
already there and pushing no symbol package, which GitHub does not take.

```bash
dotnet pack .template.config/TemplatePackage.csproj --output artifacts/package
```

| Facet | Where |
|---|---|
| Content | every file of the checkout under `content/`, minus what generation always leaves out, the scratch folders (`.git`, `.claude`, `bin`, `obj`, `node_modules`, `TestResults`, `artifacts`), the plans and the package project; `TemplatePackagingTests` holds the list against `template.json` |
| Version | the `templateVersion` symbol of `.template.config/template.json`, which release-please raises, and nowhere else; it replaces `0.0.0-checkout` in a generated project's overview and its `cleanarch.json`, as [what a project receives](using-the-template.md#what-a-project-receives-and-what-stays-here) allows |
| Metadata | id, authors, description, tags, licence, `CHANGELOG.md`, `.template.config/icon.png` and `.template.config/package/README.md`, all in the package project, at the package's root and outside `content/`, so no project receives them |
| Readme | `.template.config/package/README.md`, not the repository's: nuget.org renders Markdown without HTML, and neither a relative link or image nor an image from a host it does not allow; `TemplatePackagingTests` holds each package's readme, the launcher's and the project tool's among them, to that |

`dotnet pack` warns NU5123 on every entry under `content/` longer than 167 characters. This package
accepts those entries: the database overlays under `.template.config/variants`, the Auth unit tests
and the `ExternalIdentity` folders of the Blazor starter, the longest at 207 characters. The template
engine reads the package, not a Windows checkout, and the generated paths keep their own budget.
`TemplatePackagingTests.TheLongestEntryUnderContent_StaysWithinItsBudget` fails when an entry
passes 207 characters.

## Branches and releases

| Branch | Holds | Moves by |
|---|---|---|
| `develop` | the next version; the default branch | a pull request from a `<type>/<subject>` branch, up to date with `develop` and rebased, the history kept linear |
| `main` | the released commits only | `template-release.yml` fast-forwarding it to each release; nobody pushes to it |
| `net<major>.0` | the last release built on an earlier .NET major, for its fixes | a pull request, cut from that major's last tag when `develop` moves to the next .NET |

Every commit is a [conventional commit](https://www.conventionalcommits.org/en/v1.0.0/): a `feat` or a
`fix` lands in the changelog, a `!` or a `BREAKING CHANGE` footer makes the next version a major one,
and the other types stay out of it, so a wrong type is a wrong release note and the type is part of
the review. A working branch is deleted once merged: its commits are on `develop` and its pull
request keeps the discussion and the checks, so the branches left are the work in progress.

`main` follows the releases rather than being the one trunk with versions as tags, where the branch
people land on would hold unreleased commits beside released ones; and it never takes a merge, unlike
Git Flow's, so no version bump travels back to `develop`. Versions and notes come from the commits
rather than from someone's memory, and nuget.org trusts the workflow's identity rather than a stored
key that could leak and publish without a reviewer. A pull request release-please opens with
`GITHUB_TOKEN` starts no workflow, so no check runs on it; it changes `CHANGELOG.md`,
`templateVersion` and the release manifest only. Release branches of their own become worth having
once several maintainers release at once, or once a fix must reach an earlier minor version, which a
`net<major>.0` branch does not cover.

A squashed pull request lands under its title, which release-please skips in silence unless it is a
conventional commit. The `Conventional Commits title` job of
`.github/workflows/template-pull-request-title.yml` checks the title of each pull request to
`develop`, again whenever it is edited: `<type>[(<scope>)][!]: <subject>`, the type one of the
changelog sections of `.github/release-please/config.json` (`feat`, `fix`, `perf`, `revert`, `docs`,
`refactor`, `test`, `build`, `ci`, `chore`, `style`), the scope in lower case. Its step first proves
the pattern accepts the titles release-please and Dependabot write and refuses a few that are not.

The checks run on the pull request, which must be up to date with `develop`, so the merged tree is
the one they passed. A push to `develop` runs CI, CodeQL and the release workflow; the acceptance
matrix runs in full each night `develop` has moved, as its pull request ran only the legs its change
could affect. The SonarQube steps of `ci.yml` analyse a push to the default branch only, as
SonarCloud's free plan analyses one branch, so the SonarCloud project names `develop` as its main
branch; they skip a Dependabot pull request, which gets no repository secret. Dependabot reads its configuration on the
default branch, and `.github/dependabot.yml` names `develop` for each ecosystem in this repository
only.

`.github/workflows/template-release.yml` runs release-please on every push to `develop`, from
`.github/release-please/config.json`; both reach neither a project nor the package. It keeps one
pull request open that raises `templateVersion` and adds the version's `CHANGELOG.md` entry from the
commits since the last release. Merging that pull request is the release: release-please tags
`v<version>` and creates the GitHub Release, and the same workflow then checks out the tag, refuses
it if it does not name `templateVersion`, packs the template package, the launcher and
`CleanArch.Cli`, which a project's bootstrap pins at its template's version, installs the
template package alone in a scratch hive, generates and builds the defaults, generates them again
through the launcher, attests the packages' provenance and attaches them to the release. Its
`publish` job waits in the `nuget` environment for a reviewer, exchanges the workflow's identity
for a short-lived nuget.org key (Trusted Publishing, with `NUGET_USER` the account's name as a
repository variable) and pushes the three packages; a version already on nuget.org fails the push. Its
`mirror` job then pushes the same packages to GitHub Packages, so the repository page lists them
([the package](#the-package)). Last,
`promote` fast-forwards `main` to the release commit, and refuses if `main` is not its ancestor.

## The launcher

`launcher/CleanArch.Create` is the .NET tool `dnx CleanArch.Create` runs, with its tests in
`launcher/CleanArch.Create.Tests`. Its `Hosting` folder starts it, `Commands` holds its arguments, its usage and the one command it has, `new`, in `Commands/NewProject`, and `Setup` one class per step of a new project's setup. The folder is its own solution, `launcher/CleanArch.Create.slnx`,
which holds five more projects. Generation and the template package leave the folder out, and
`TemplatePackagingTests` holds both.

| Project | What it is |
|---|---|
| `launcher/CleanArch.Tooling` | the library both tools share, never packed on its own: running a process and its verbatim output, the terminal, the templates' manifests with the evaluation of their refusals, and the questions asked from them; its tests are in `launcher/CleanArch.Tooling.Tests` |
| `launcher/CleanArch.Cli` | the .NET tool `dotnet cleanarch`, packed as `CleanArch.Cli` at the same `templateVersion`, which finds a project by the project file at its root: `BuiltInCommands.cs` lists every command, and the ones that stay the tool's each have a folder, `launcher/CleanArch.Cli/Eject` with the source it ships and `launcher/CleanArch.Cli/UpgradeReport` reading template packages through `CleanArch.Tooling`; `launcher/CleanArch.Cli/Joins` holds the join steps `add` runs, which are not commands. `launcher/CleanArch.Cli/Generators` holds one folder per generator, its command, request, plan, edits and report, and the templates of `add use-case` in `launcher/CleanArch.Cli/Generators/AddUseCase/Templates`, while `launcher/CleanArch.Cli/code-templates/add-feature` holds those of `add feature` and `add field` at their path in the reference feature, over `launcher/CleanArch.Cli/Generators/Shared`, the pipeline every generator runs and the code they share, which references no generator; its tests are in `launcher/CleanArch.Cli.Tests`, whose `launcher/CleanArch.Cli.Tests/Architecture` holds the launcher's boundaries: no command or generator references another, `Kernel` and `Shared` reference none, `CleanArch.Cli.Commands` references System.CommandLine alone, and only the two process launchers call `Process.Start` |
| `launcher/CleanArch.Cli.Commands` | the commands `eject` can copy into a project, never packed on its own: it references System.CommandLine alone, since an ejected command is a file-based app that declares nothing else. Each command has a folder and a namespace of its own and references no other; `launcher/CleanArch.Cli.Commands/Hosting` holds the root command, the registration and the context, and `launcher/CleanArch.Cli.Commands/Kernel` what the commands share, the process launcher the context carries and every external call among it, the documentation index and the scaffold steps `bootstrap` runs, referencing neither the commands nor `Hosting`. `CleanArch.Cli` composes them with the ones that stay the tool's, and ships the source of every folder, which eject merges into one namespace |

[Template decision 0003](decisions/0003-a-project-runs-its-tasks-from-a-pinned-dotnet-tool.md) says
what each tool is for.
The tests that read the manifests through the launcher's own settings stay in
`launcher/CleanArch.Create.Tests`.

The launcher creates a project and nothing else, and ends by running `dotnet cleanarch bootstrap` in
it; `add web-app`, `add web-package` and `add presentation` are the project tool's, which runs the
item template at the version `cleanarch.json` records, so an addition always matches the project's
template version whatever launcher a user runs, and answers every question about the project from
that file, never from its other files. Both tools ask from the manifests rather than from a copy of
the options, which would drift at the first option added: each visible option of `ide.host.json`, in
its order and under its label, with the choices, defaults and descriptions of `template.json` and the
long names of `dotnetcli.host.json`, embedded from the commit the tool is packed from, or read from
a checkout or a `.nupkg` with the launcher's `--template-source`. A refusal is evaluated, never
restated: the tool evaluates its `isRequired`, and the computed symbols it names, on the answers,
shows its description and asks again the options it reads. Before each question it evaluates the
refusals over every value of the options not yet answered and leaves out a choice none of them saves,
such as `--examples all` after `--auth none`; a question left with one choice is answered with it,
naming the answer that leaves it, and so is an option under `--yes`, while without `--yes` a choice
with no default is still asked. What the manifests cannot say stays in the tool and is tested against
them: the web app's name, audience and rendering are asked only with `--web`, and a refusal that
reads an item's `-n` is checked on the name asked. An option visible in `ide.host.json` thus reaches
both tools with no change to them.

| Facet | Where |
|---|---|
| Questions | read from the manifests under `.template.config/`, embedded at build; [adding an option](#adding-an-option) gives the rules an option and a refusal keep |
| Version | the same `templateVersion`; the launcher installs `CleanArch.WebApi.Templates` at its own version |
| Thread | drawn with the glyphs every console font carries, as the paragraphs below the table say |
| Packages | `Spectre.Console` and `System.CommandLine`, pinned in `launcher/Directory.Packages.props`, which imports the repository's |
| Analysis | `SonarAnalyzer.CSharp`, pinned beside them, runs its default rules on all seven projects, and the build fails on any of them; a method of the four projects that ship holds a cognitive complexity (S3776) of 15 at most, set in `launcher/.editorconfig`. The measure counts nesting and breaks in flow rather than branches, so a flat `switch` stays cheap and a method that nests is split. A finding is fixed in the code, never silenced |
| Gate | `verify` restores, formats, builds and tests the launcher's solution, all seven projects, after the project's gate; CI's `launcher` job does the same |
| Release | `template-release.yml` packs it beside the template package, with its symbol package, installs it as a tool, generates the defaults with it, and publishes it with the template package |

Above Latin-1, the thread draws WGL4 glyphs only, the set Consolas, Cascadia Mono, Lucida Console,
DejaVu Sans Mono and Menlo all carry: `♦` for the question asked, `◊` for an answer, `▲` for a
warning, `●` for a note, `■` and `□` for the steps, square box-drawing borders, and a spinner
turning through box-drawing glyphs (`launcher/CleanArch.Tooling/Prompts/RailSpinner.cs`). A terminal
draws a glyph its font lacks from a fallback font, at that font's width and offset, so the marker
lands off the rail and the columns after it shift; no console tells a program its font, so the
glyphs cannot be chosen per terminal. The diamonds, rounded corners and braille spinner other
prompt libraries draw stay out, and so does every emoji.
`GuidedFlowTests.EveryGlyphDrawn_IsOneConsoleFontsCarry_SoTheThreadStaysAligned` runs a whole new
project on the thread and fails on any glyph above Latin-1 outside the WGL4 list it holds.

`dotnet cleanarch adopt` writes the project file of a project generated before the tool by reading the options the project was generated with from its files, and the launcher reads a project's name from them to say it is inside one; `GeneratedProjectTests` holds each reading on a tree of its own:

| Option | Read from |
|---|---|
| `-n` | the one solution file beside `cleanarch.json` |
| `--auth` | `local` with the `Application.Auth` project, `external` with the JWT bearer package in `Directory.Packages.props`, `none` otherwise |
| `--examples` | `all` with `Features/TodoLists` in the application project, `notes` with `Features/Notes` alone, `none` otherwise |
| `--files` | the `Infrastructure.Storage` project |
| `--database` | the provider package `Infrastructure.Core` references: SQL Server, SQLite, PostgreSQL otherwise |
| `--code-host` | `github` with a `.github` folder at the root, `gitlab` with `.gitlab-ci.yml` or a `.gitlab` folder there, `none` otherwise |
| `--ci`, `--agent-files` | `.github/workflows/ci.yml` or `.gitlab-ci.yml`, `AGENTS.md` |
| `--agent-files-example` | nothing: recorded `false`, the lines it writes being the team's to replace |
| `--has-api` | the `src/Presentation/<Initials>.Api` folder |
| `--scope`, `--workspace` | the name in `src/Web/package.json`, the project's name in kebab case without one; `src/Web` |

Its tests read this checkout's manifests: the options are the visible ones, each refusal fires for
some combination, the launcher refuses exactly where `refusedOptions` does, and answering only what
it offers never ends in a refusal. The terminal thread is tested on Spectre's `TestConsole`, by its
markers and their order rather than its colours, with the processes it starts replaced. One test packs the
templates, runs the launcher without a terminal for a project and compares the tree with what
`dotnet new` writes from the same answers. Another generates a
project with `--web`, once as a React single-page app and once as an Angular app rendered on the
server under an external provider, and the same project without it, to which `cleanarch-webapp
--workspace` and `add web-app` add the app: the two trees must match but for the options the overview
records, so an edit of `src/Web/README.md` or of the app's `README.md` that is missing, wrong or
placed elsewhere than the project template writes its lines fails it. A third adds two
server-rendered apps to a project generated with a single-page app and requires the ports
`cleanarch.json` records, the authentication page and the decisions index to be what
`--web-rendering ssr` writes. Each of
these runs `docs-index` in the tree generated with the part, as its bootstrap would, so the indexes
the joins write are compared too, and a fourth requires the indexes of a project that added the MCP
server to be those of one generated with it.

A multi-valued option, `--presentation`, is one multi-select that keeps at least one value and
reaches `dotnet new` as the option repeated once per value; the launcher's conditions read `==` as
"among the values" and `!=` as "not among them", splitting an answer on `|`, which no free-text
answer a condition compares may hold. When a web app, a desktop client or the MCP server under
local accounts calls the API, the launcher shows the API as answered for you, naming the caller,
and passes it; `TheCallersTheLauncherNames_AreExactlyWhatMakesTheTemplateAddTheApi` holds the
callers against `needsApi`. When the hosts chosen hold neither the API nor the worker, it shows the
worker as answered for you and passes it; `TheWorkerTheLauncherAdds_IsExactlyWhatMakesTheTemplateAddIt`
holds that rule against `needsWorker`.

```bash
dotnet run --project launcher/CleanArch.Create -- --template-source . --hive /tmp/cleanarch-hive
```

runs it from the checkout; pass `--template-source` a `.nupkg` to try a packed one.

## The upgrade report

A project's `upgrade-report` command generates the project from
the package at the version `cleanarch.json` records and at a newer one, then compares both with the
project's files;
`docs/guides/upgrading.md` is its page. It generates both versions, from the name, options and parts
`cleanarch.json` records, because a hash of each generated file would tell what the project changed
but not what the target changes, and a three-way merge as Copier or Cruft apply would write conflict
markers into generated code and lockfiles before anyone reviewed them. It writes nothing in the
project and runs nothing from a package: no `dotnet new` post-action beyond printing instructions is
accepted, and the parts' joins are not replayed, so a file a join edits reads as one to merge. An
option is never mapped: one the two versions do not share, or a recorded value the target no longer
offers, is the team's decision, and an item template's option renamed in the target is reported, not
followed. It holds these things in step with the rest of this repository:

| What | Kept in step with |
|---|---|
| `_writtenByBootstrap` and `_editedByBootstrap` | the files `bootstrap` writes or edits |
| `PlanOf`, which turns `cleanarch.json` into a generation and item templates | the options the project file records, the names generation gives each host, and the item templates' options |
| the changelog range | the version headings release-please writes in `CHANGELOG.md`, which the package carries |
| the packages of the released versions | each stays on a NuGet source a project reaches: the report installs the version `cleanarch.json` records from the sources, and a version no source holds cannot be generated again |

Its `--self-test`, which `verify` runs, judges the comparison on fixtures. The `defaults` leg of
the acceptance runs the real path on a bootstrapped project, with a version as the only change. A
change that touches more runs by hand: pack a copy of the checkout under two `templateVersion`
values, generate a project from the first in a scratch hive, and run
`upgrade-report --source <folder>` with the folder holding both packages.

## Changelog

`CHANGELOG.md` follows Keep a Changelog and stays here, and the package carries it at its root for
the upgrade report. Record what changes a generated project: a layer's shape, a rule the tests
hold, a setting, a default that affects security or deployment. A major version means adopting the
change in an existing project takes manual work.

## Decisions

[The index of the template's decisions](decisions/index.md) lists each one by number, with its
status.
