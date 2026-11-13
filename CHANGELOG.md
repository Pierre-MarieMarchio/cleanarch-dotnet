# Changelog

All notable changes to this template are recorded here. Versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

From 1.0.0 on, release-please writes each version's entry from the conventional commits merged into
`develop`: a `feat` is a feature, a `fix` a fix, and a `!` or a `BREAKING CHANGE` footer makes the
next version a major one. The section after the first entry describes 1.0.0 as a whole.

**What counts as notable in a template**, since it is cloned rather than referenced: anything that
changes the *generated* project — a layer's shape, a convention the architecture tests enforce, a
configuration key, a default that affects security or deployment. A change invisible to someone
starting a new project is a `refactor`, `test`, `build` or `ci` commit, and stays out of this file.

**What "breaking" means here**: `Src/` is a starting point, not an API, so it cannot break a caller.
A major version signals that adopting the change in an existing project derived from this template
requires manual work — a renamed configuration section, a moved namespace, a new mandatory setting.

## [2.0.0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/compare/v1.0.0...v2.0.0) (2026-11-13)


### ⚠ BREAKING CHANGES

* a generated project ships five decision records, numbered 0001 to 0005, in place of the forty-one it carried; links to the removed records now point at the pages that hold their reasons.
* **template:** `dotnet new cleanarch-presentation` refuses `--agent-files`; leave it out.
* a generated project's root folders are src, tests and tools. A project generated from 1.x moves its own Src, Tests and Tools, and rewrites the paths that name them, as docs/guides/upgrading.md says.
* a generated project no longer ships Tools/Tasks.cs nor its generators; run dotnet cleanarch <command>.
* a generated project's first step is `dotnet tool restore` then `dotnet cleanarch bootstrap`, no longer `dotnet run Tools/Tasks.cs bootstrap`, and that bootstrap no longer installs CleanArch.Cli.
* **web:** `pnpm api` needs `dotnet cleanarch` on the path and no longer writes packages/api-client/openapi/document.json.
* **launcher:** `dnx CleanArch.Create -- web-app`, `-- web-package` and `-- presentation` no longer exist. Run `dotnet cleanarch add web-app`, `add web-package` or `add presentation` in the project instead.
* **template:** a project generated before keeps AppTemplate.sln, which a newer Tools/Tasks.cs no longer finds. Run `dotnet sln <Name>.sln migrate`, delete the .sln, and move every reference to it, such as a workflow's SOLUTION, to <Name>.slnx.
* **tasks:** new-feature no longer adds a row to docs/README.md and remove-feature no longer expects one; a join README edit "at the rank of its number" is refused, and the project's docs/README.md and docs/decisions/README.md no longer list pages: run `dotnet run Tools/Tasks.cs docs-index` and link its index.
* a project's Markdown files must be saved without a BOM; `dotnet run Tools/Tasks.cs format-fix` rewrites them.
* **wpf:** a project generated with --presentation wpf keeps its <Project>.Desktop.Core project. To take the new shape, move its Common/ and ViewModels/ folders into <Project>.Desktop and drop .Core from their namespaces, move its tests into <Project>.Desktop.UnitTests on net10.0-windows with EnableWindowsTargeting, give the app the .Core project's package and client references, its InternalsVisibleTo and <Using> items for System.IO and System.Net.Http, and remove both .Core projects from the solution.
* **web-join:** cleanarch-webapp --workspace requires --project-name, since the workspace it creates brings the web apps' end-to-end tests, which are named after the project.
* ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* **tests:** ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* **auth:** ISecurityEventLog.Record(SecurityEvent) is now RecordAsync(SecurityEvent), returning a Task; a generated project that calls or implements it must await it.
* ITodoListRepository, IReminderRepository and IStoredFileRepository take the owner in GetAsync(UserId ownerId, Guid id, CancellationToken), IStoredFileRepository gains GetReadableAsync, and OwnedAggregate is removed from the application core. A feature generated with --access owner --model aggregate gets the same GetAsync signature.
* IDateTimeProvider and FixedDateTimeProvider no longer exist in a generated project; inject TimeProvider and call GetUtcNow().
* **application:** use cases are Mediator messages sent through ISender; IUseCase<,> and the I<UseCase>UseCase interfaces are removed.
* **template:** project folders under Src/ and Tests/ are <Initials>.<Layer>[.<Module>]; project and assembly names are unchanged.
* **template:** --presentation none, --jobs and --presentation-name are gone; pass each host with --presentation, the worker as --presentation worker.
* **template:** --web react and --web vue, and cleanarch-webapp --framework react and --framework vue, are refused; pass vite-react or vite-vue.

### Features

* **application:** send use cases through Mediator, with validation, tracing and logging behaviours ([#244](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/244)) ([a79c5b0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a79c5b0c27064463b0f034f3292bbb1fef390d47))
* **auth:** accept the previous signing keys while one rotates ([80c190a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/80c190a903c52689b7e4fe8ba4bb0a6d80d517db)), closes [#212](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/212)
* **cli:** add and remove features through the project tool ([736a4b1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/736a4b19468837b4651c41ef35b3d15529335f1c)), closes [#346](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/346)
* **cli:** add dotnet cleanarch format with its --check ([bde7570](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/bde75702248d84140bda9bc6b45355a4d7ab5604)), closes [#342](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/342)
* **cli:** add fields through the code templates a feature was written from ([e9c28be](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e9c28be1e4fe53ed94ff39e3a7736912df283d6f))
* **cli:** add web-app, add web-package and add presentation ([828867b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/828867b40357d6cb7f1d7b909741d044e7216206)), closes [#347](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/347)
* **cli:** adopt, eject and the project's own commands ([384da69](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/384da69ed3b050c7394665d9a6097476f58df43b)), closes [#349](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/349)
* **cli:** bootstrap, run, compose, end-to-end and sonar commands ([16f6b51](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/16f6b51c6e324f2d3c9cb735972d2462cb17f5fa)), closes [#348](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/348)
* **cli:** build, test and coverage run the project's steps ([e37f1ae](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e37f1ae231ec11dfac7b8ea5a87d84e470a12284)), closes [#341](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/341)
* **cli:** cap and configure build, test, coverage and verify ([21727b9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/21727b9b58318feda3a965fd2f8297123567e6fa)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** check what a change touches ([6ead303](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6ead3034040d8cfbbff114860d056af71cbc424b)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** close a launched process's input when a step asks it ([145cadf](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/145cadf63ca7b2242878fd4093775bb05352ae68)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** copy a generator's templates into the project ([ed860f4](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ed860f453cd42164087012d55e89c04ea419292c))
* **cli:** copy add use-case's code templates with templates copy ([7645a2a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/7645a2a3d09b3311d80ff88dfa718c7945e62368))
* **cli:** copy the code templates of add event ([15344ed](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/15344ede2f9d27f1bc170681cc364df83179fb94))
* **cli:** copy the code templates of add setting ([5471e94](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5471e94d7b0ea2a67623571f1a2cd6ced27a7c26))
* **cli:** create the projects and apps a scaffold.json describes ([5fe297b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5fe297bc2a1f3a6775af732c9a8fe0dfb85a9c90)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** dotnet cleanarch verify runs the whole gate as one chain ([4857d59](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4857d598c7a0cd4051655c5fc527b0ea327e80e0)), closes [#343](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/343)
* **client:** a .NET client of the API generated by Kiota ([#137](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/137)) ([bf64cff](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/bf64cffbb9b8bfeec58d4e5f2cf67d5ad5bc6e29))
* **cli:** hold the coverage floor inside the tool ([87906e1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/87906e167a7fd2081ff3686840b6ea55b1348a1f)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** join presentation projects and web apps as their README lists ([7b6950e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/7b6950e8e652777466971357c865e56274768f35)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** leave the web schema out of openapi with --no-web ([c377abb](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c377abb51f3a008ad49ce9c64df7d735abf5b706)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** let a project override a generator's report ([5c232d6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5c232d697273dcf26604368b1d30ab3392c06825))
* **cli:** let a project override add use-case's code templates ([cb491dc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cb491dc357ec340ac6bac7b905e9c538eb5ff14d))
* **cli:** let a project override the code templates of add event ([6aa3986](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6aa398623d29bc3473563267c9b9353c322abcad))
* **cli:** let a project override the code templates of add setting ([3d7b9b5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/3d7b9b5ff7d8bd72de12368cca132e19d995793c))
* **cli:** let a project without authentication copy code templates ([e3ce0b5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e3ce0b53b4a2fb3930b7de67346dd829ce7fb37b))
* **cli:** let a starter drop a package its template declares ([ffdeb98](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ffdeb98423b17ad0c8db17410368a4551b1e49ba)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **cli:** migrations and database commands wrap dotnet ef ([a452cea](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a452ceaba78158e5c5e17bddcbfd3684b5fae29a)), closes [#344](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/344)
* **cli:** openapi writes the reference then every client ([2fb9063](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2fb906335bf8c72c9fa300ca9980cc037b6af6f2)), closes [#345](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/345)
* **cli:** pair root folders cased otherwise in upgrade-report ([146b21b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/146b21b23ce1daadedaa7c2d3483823525624daa)), closes [#364](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/364)
* **cli:** report what a newer template version changes ([9866d62](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9866d621b62dc5b9db79fc1fc01d94d10264d8b0)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** run an addition's join and scaffold steps in the tool ([6c592c1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6c592c1387fae908fdf66185c7064894d85b68f9))
* **cli:** run no script of Tools/ in verify outside the template ([5de990a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5de990a52b91b570a18e9035f11615f44264c65a))
* **cli:** run the generators' steps as the tool's own commands ([ca19a7d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ca19a7de8ea4463ab6ca8310de65ebf5a05fd57b))
* **cli:** the dotnet cleanarch tool and the library it shares with the launcher ([#173](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/173)) ([3ad1faf](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/3ad1fafc9ceff291bbe0ddf71b60321b66dd1612))
* **cli:** the dotnet cleanarch tool answers for the project it finds ([1c13806](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1c138064a590881da92b046d5891e2af8cf21444)), closes [#338](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/338)
* **cli:** total the test reports with test --summary and --reports-only ([da4972b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/da4972bdb8376ffb0a8fcc02b19600ce9f7dd0a5))
* **cli:** warn when the tool changes a template the project overrides ([714bd2d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/714bd2d57c6de5704ad4f2b9a8463f79c019d8f1))
* **cli:** write a use case's parameters one per line ([c5232c5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c5232c58c505dc947028d7c5b8ec81ddd9e2e31b))
* **cli:** write add feature's files from the project's code templates ([915ebb5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/915ebb573660362d623f7d390622c192bd08fd3b))
* **cli:** write and check the documentation index ([49ec1d8](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/49ec1d862dc90cea0f0655ce60e70fa6643f93ef)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **community:** give each project its issue forms and pull request template ([d6880d9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d6880d93f95a586eeb81ab1dc9579d581bec4376)), closes [#273](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/273)
* **items:** complete an added item with dotnet cleanarch add ([55067ca](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/55067caffe299ec31b91f148efa31a46b88323ea)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **launcher:** a guided, styled prompt flow in the terminal ([d55e8a3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d55e8a3177ffb41624f6ff5825505db4bff2d0d3))
* **launcher:** create projects only, leave additions to the tool ([15ac4e5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/15ac4e50d7eab0a24ce7143243864c7439affd31)), closes [#360](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/360) [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **launcher:** finish setting up the generated project ([8608221](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8608221bd2c228bced2ade9b20679cc38060c769))
* **launcher:** go back to the previous question ([d85ba86](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d85ba86cf6190b1bbb12e85e48a9fcb913c84bae))
* **launcher:** name the project after its folder and generate there ([9bd92dd](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9bd92dd5e3fe824965d8399543118ec193b7517b)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **launcher:** presentation adds a presentation project with the options read from the project ([#135](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/135)) ([cd126fa](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cd126fafe804d9140df4636f92dd465a77f39499))
* **new-feature:** apply the edits it printed, all or none ([c9ec9e1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c9ec9e18d997b77b6072d0b33f1b961ce4252851)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222)
* **new-feature:** generate the fields --fields names in every layer ([ea57bba](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ea57bbaf79ea1f59aab57d58f5d667f775376a01)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222)
* **new-use-case:** take typed inputs, and build the generated handler test in one place ([4aa532a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4aa532a21b1c4871741d31f3428c34e66f74a1bd)), closes [#205](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/205)
* **observability:** export the logs over OTLP with traces and metrics ([8465b1a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8465b1ad68f0c991fb530b7289dfa14f32dac15c)), closes [#228](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/228)
* pin dotnet cleanarch at generation and restore it first ([ee6d759](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ee6d75995abde27eb77dc4cebd7cc0582281ac1d))
* **presentation:** add the worker to a project generated without it ([4173286](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/417328665662af7fa0409be0424f727fbffaed39)), closes [#225](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/225)
* print the openapi and client steps from every generator, and fail verify on a stale schema.ts ([501058b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/501058b9a13aa2bcde8c22fac1073bea7b3f4354)), closes [#203](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/203)
* remove Tools/Tasks.cs and its generators from the project ([f0d02a3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f0d02a37c3d4e1bdaa0cd5e910089f6261a578b0))
* **remove-feature:** remove an edited feature and report what names it ([83464a5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/83464a5f90253ce03d36518a2c1971d6f9283eee)), closes [#226](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/226)
* **tasks:** add a check task over what changed, and name what format-fix cannot rename ([187068b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/187068bd14c87a7a71eb2bfe4595713dfc309619)), closes [#217](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/217)
* **tasks:** add new-event, which writes a domain event and its consumer ([c7cfc3d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c7cfc3d50b40a18e05722f2fbcd8df2ee5c55128)), closes [#224](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/224)
* **tasks:** add new-field, which adds a field to a generated feature ([655c468](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/655c468545e6e78e1f74b68bb0d70aa99eeabbc9)), closes [#223](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/223)
* **tasks:** add new-setting, which writes a validated options section ([1276ae3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1276ae3c6d6dc5572a50b0582767e4829d7e6791)), closes [#224](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/224)
* **tasks:** add upgrade-report, to say what a newer template changes ([4631f45](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4631f452493db4971fc6ebad986941eab1ea111d)), closes [#227](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/227)
* **tasks:** list pages in the generated index, never by a row edit ([90fef34](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/90fef34df71cac9dc46a18106efa1dd0c24bc3b5)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* **tasks:** skip and name Windows-only test projects off Windows ([ff14ac5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ff14ac54f8b582a673333c468239e5d58a3c3dfe)), closes [#293](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/293)
* **tasks:** write each docs folder's index from its pages' frontmatter ([e937ddc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e937ddc83dd161c81d7beed1b9d0899dbc94c139)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* **template:** --has-client lets a project holding the .NET client take the other client app ([#144](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/144)) ([20ab6da](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/20ab6da6f81cda16837ab274f53df7a1645a96fc))
* **template:** --jobs worker runs the background jobs in a worker of their own ([#157](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/157)) ([4ebd146](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4ebd1466abdf7559f4fd713b31dc2b50c59f26fb))
* **template:** --presentation avalonia, a desktop client of the API through the .NET client ([#140](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/140)) ([38c3fa5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/38c3fa5efac0b423661f1930d2c5a527b7bc8192))
* **template:** --presentation blazor-server, a Blazor Server project beside the API ([#131](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/131)) ([62e9d5d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/62e9d5d17a9abd45a45481a177fce68ee21333fa))
* **template:** --presentation mcp, an MCP server over the use cases, with access keys for local accounts ([#162](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/162)) ([db0e77a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/db0e77ac9dec801395ca0e6caa8049505e1f2e94))
* **template:** --presentation wpf, a Windows desktop client of the API through the .NET client ([#160](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/160)) ([9d9f146](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9d9f146c81dfdee81f153596c9b59a6b28c689e9))
* **template:** --web blazor-wasm, a Blazor WebAssembly app calling the API through the .NET client ([#142](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/142)) ([b1380ca](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b1380cac6f176d4fcbfae9d4554cfa35529ff946))
* **template:** --web-rendering ssr renders the Angular app on its own Node server ([#149](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/149)) ([ec5a722](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ec5a72212350c89f4454ff621d4e7d4536149296))
* **template:** --web-rendering ssr renders the React Router app on its own Node server ([#159](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/159)) ([4d46c5e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4d46c5e2bf37ede09d7dbf0a6308313b9f7f6a36)), closes [#128](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/128)
* **template:** a server-rendered app keeps the local session on its Node server ([#155](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/155)) ([08f0ea9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/08f0ea95f3ac44d802d4692350e56f306a84963d))
* **template:** a server-rendered app signs people in at an external provider from its Node server ([#158](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/158)) ([ee870af](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ee870af00cbec3064f4082b341c5101fa50b3d64))
* **template:** add --aspire, an Aspire app host beside Docker Compose ([96fe273](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/96fe27382c81a657c61ba12334bd43929a372f8d)), closes [#228](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/228)
* **template:** carry the solution as an .slnx ([e0bdfa1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e0bdfa119a5ce2ce0f74f33f4169e5bf4736775b)), closes [#337](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/337)
* **template:** choose the code host at generation, GitHub or none ([#33](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/33)) ([a2b4410](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a2b44109db445ec171f69abcdaa3981d4021e6aa)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** cleanarch-presentation adds a presentation project to a project generated without one ([#134](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/134)) ([b6f342f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b6f342f1ab0c44ecab01f4701031d22237613eba))
* **template:** cleanarch-webapp --framework blazor-wasm brings the .NET client, joined by web-join ([#143](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/143)) ([1abb338](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1abb33887ecae4210a51f027c6e69742770112b0))
* **template:** cleanarch.json records the generation and every part added since ([#171](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/171)) ([03b761e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/03b761e03423f1ba900edfb1664b772476fdb282))
* **template:** external sign-in for the Avalonia app, the client sending the provider's token ([#145](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/145)) ([f9af9c2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f9af9c271275720c79fc853551ac81c2d0fb169b))
* **template:** external sign-in for the Blazor WebAssembly app, the client sending the provider's token ([#148](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/148)) ([8747c63](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8747c63bd456cd6647b51327fd30e12121d2abe6))
* **template:** give a GitLab project its merge request and issue templates ([d12cf4c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d12cf4c8930326a30fb000b0ac2b125baf8c7c1b)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** give a GitLab project the gate ci.yml runs ([9e2fec3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9e2fec397b70ba9c5595c7f6d2d84f8cb0477c37)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** keep a GitLab project's dependencies current with Renovate ([#47](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/47)) ([71bca4a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/71bca4a3a7675389cb5073b07a44749e946a0d89)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** name project folders after the product's initials, not its name ([#242](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/242)) ([2270642](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2270642a128a76589a12420f0cb68d628b90c69a))
* **template:** offer example lines in the project section of AGENTS.md ([dfccaea](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/dfccaea0681377f52e04c1f8ab9ce9d76d253dcd))
* **template:** offer gitlab as a --code-host value ([#48](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/48)) ([dde62f2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/dde62f2d0457223201afc4aa0b0d837094841212))
* **template:** one web value per framework ([#169](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/169)) ([80096d0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/80096d0d3717949e86116822c72453aa9139221e))
* **template:** presentation projects are chosen, the API among them ([#172](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/172)) ([c834691](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c834691cdb7661911bc6bf8a93ec6dc08fe6baa2))
* **template:** release a GitLab project on a tag ([#45](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/45)) ([2ddd5b6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2ddd5b612a307c094802788feaa893c10332f297)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** run a GitLab project's image, stack and web jobs ([#41](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/41)) ([4427a82](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4427a824bd0451d2982d15d0fca2cf633b86e282)), closes [#10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/10)
* **template:** the questions start with what the project holds ([#168](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/168)) ([75b4e2b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/75b4e2b57245007892ed89ab5e547e449d65e3b7))
* **tools:** lock the generated web shapes with Tools/WebLockfiles.cs ([cb0e9a4](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cb0e9a4f261125ab89738fc5bb11cf9ac12efd15)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **tools:** new-feature, new-use-case and remove-feature work without the API ([#175](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/175)) ([da16fcb](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/da16fcb7464837e3a40f9b7f6c90ce9567fd35de))
* **verify:** install, lint, test and build the web workspace ([87ad82d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/87ad82da0b79a39a5e15d7ee8eb00092e77d1e75)), closes [#225](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/225)
* **web:** --web react-router, its inline scripts allowed by hash ([#127](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/127)) ([75bd3e9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/75bd3e973e3cbe32720656f58eb9b96397d62a52))
* **web:** vue, vite-react, vite-vue and vite-svelte starters ([#126](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/126)) ([2818771](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2818771bcd11b3eb8cd3561618b26a407e85226e))


### Bug fixes

* answer 409 for a broken invariant on every path, and validate a generated update before it mutates ([#254](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/254)) ([fb582cc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/fb582cc0a2179417ad47de1af360aa5ea5c2efea)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)
* answer 409 uniqueness.conflict when a save breaks a unique index, instead of 500 ([#256](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/256)) ([c47b95d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c47b95d0165c25814c4a68679af39b75297ebcec)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)
* **api:** always put the ValidationProblemDetails schema in the OpenAPI document ([a6ee15a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a6ee15a6011cb83437c3f89f20ecbcfae61764a8)), closes [#236](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/236)
* **api:** complete the idempotency key on every successful result ([1402f0d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1402f0d1ddf9cea904090c9a02b001b9efd10ef4)), closes [#304](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/304)
* **api:** keep the v1 OpenAPI document when the API has no controller yet ([0624c42](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0624c42cadf77c986642be6274df6c303dbc5c73)), closes [#197](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/197)
* **api:** store a client's instants in UTC, which PostgreSQL requires ([b897c69](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b897c693728cd90b90508356a88c375702183f52))
* **api:** the OpenAPI document types a problem's code and field errors ([#136](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/136)) ([d473431](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d47343104beead78884186ea38b492d342e2cdd5))
* **aspire:** build a WebAssembly app from the repository root ([a274879](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a2748794f59107aaf31f0ed4cf24eb0769163bc6))
* **aspire:** give a WebAssembly app its sign-in provider in the app host ([697b200](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/697b2004318cc584f02de4977a9dfdcbac7cb784)), closes [#294](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/294)
* **avalonia:** use PlaceholderText in the starter's window ([f7c668d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f7c668d704d80d866e9f73d1adee5d77e6889510))
* build the HTTP pipeline without a second container, and refuse any silenced diagnostic outside the gate's list ([d26749c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d26749c4898e5c6b5bd291c3755d1482e157bb03)), closes [#197](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/197)
* **build:** fail on a warning a build task logs ([c1a83db](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c1a83db3194614a18bef7f08918cb93052ff07a5)), closes [#299](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/299)
* **build:** keep the Sonar build's findings as warnings ([052d34b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/052d34b833a56505977031028dbfef82e6612793))
* **ci:** check for a shipped docs index right after generation ([fa4eee4](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/fa4eee411344d8f55a5fe541c206b3eb0b5f0af2)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* **ci:** fold the refused-name comment into one line ([24fae78](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/24fae783ae4f748843eb2b8fa2e73bfe20288d9a)), closes [#314](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/314)
* **ci:** install the SDK of global.json in every job ([689634b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/689634b73b3ccd4f10ad3758268dd2c807643d20)), closes [#305](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/305)
* **ci:** write the docs index in each shape the acceptance matrix builds ([7f5ba27](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/7f5ba27d1f321a025ac0e0ec7e1a2a92bde89a5e)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* **cli:** answer --help of a refused command with its reason ([0f67f73](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0f67f73ab36cfe3434595d59f503a1a4a05927e2)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** bootstrap runs every step the task runs, in its order ([28ae47b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/28ae47bdd6274509b13688e556dfe972a05e53d4)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** complete an addition whose folder exists, not refuse it ([9fdb3f7](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9fdb3f7bd0c5e4c0c72feccf949a163bbd9ff11a))
* **cli:** delete openapi --check's temporary folder when the build fails ([65c511c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/65c511c308c801e7e2c7348d62bfd816237f8ea1)), closes [#359](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/359)
* **cli:** find every project path by the rules Tasks.cs follows ([673ed1e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/673ed1eeadb56b483d415c4e941d035b07a978b0)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** format runs dotnet format as format-fix and format do ([89f6c15](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/89f6c15656668effa78d1a06a77bd60d0b262839)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** give an ejected command the naming rule it compiles with ([d9abe2d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d9abe2d1c0375de0707ef671d572dd0eedbf55d5)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** give end-to-end the environment the task gives the stack ([4e8fd83](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4e8fd83b276cd5f07194b5be00c45f29f3319bc6)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** guard migrations add as the migration-add task does ([ca6ccc6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ca6ccc6c3d0487ab2f802063c0680ee00bddb192)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** let openapi tolerate what verify tolerates from kiota ([32e7dba](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/32e7dba80d763a2ddc0a4362af0a2806e183a244)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** name a skipped test project after the run as well as before ([0fb0302](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0fb0302829286ff0ba312b871c78ea348c4f3544)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** name dotnet cleanarch in the documentation index's notice ([5faba65](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5faba65e8c8ab921bc58162360c3d5edd8e0201a)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** name generated event and setting tests after their files ([e8e8945](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e8e894538db3290916245e28e40aa9afdfc478a8))
* **cli:** name the tool's commands in the generators' messages ([2c3139d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2c3139db51ea429e676a5b7fa0fc1b700d68bffd))
* **cli:** name the tool's own commands in what its messages advise ([bef3a3d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/bef3a3d3f1ae5609e6ec1a46811b3aa8bd547fc4))
* **cli:** pass coverage its settings file as a full path ([36dca97](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/36dca979c6ec0ca2124accfe4faeed5e58a3eb68)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** point verify's advice at the tool's own commands ([d3eb4ee](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d3eb4ee0073f0c883db3e0c5948b2de696a597e8)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **cli:** read the presentation list cleanarch.json writes, and pin dotnet cleanarch in every project ([#241](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/241)) ([915241e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/915241ea4831bb7411174f3028acf10df798a7c2))
* **cli:** refuse a package whose template.json cannot be read ([5f84baa](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5f84baa7015c062600bec3253fd4fe5d8d8b8ed3))
* **cli:** write the .NET client in the namespace the template gives it ([f02ef10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f02ef1037d8b477d861c1bbb53eed3fac0c6bcd3)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** write the OpenAPI reference whatever clients the project holds ([b7d0473](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b7d0473f26559f2177982eb06a5b8c6ba0580471)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **community:** name the web workspace in prose in the project's PR template ([8ee026d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8ee026da3de8b92fdca66ad38d8de0c7cc80c908)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **community:** word the project's forms for every part it can gain ([b56a55d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b56a55d55e323010c2a940c98365f63c73f9b5e7)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **compose:** generate the CORS setting only with the API ([cad40ff](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cad40ff0337af6b180784f6216ae207c9d76bc0c)), closes [#309](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/309)
* **compose:** keep CORS closed unless an origin is set ([5b7f191](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5b7f1911d25e619d7476840d08ee6b7926d66e48)), closes [#309](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/309)
* **coverage:** anchor each source exclusion below the project ([56a7781](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/56a778133bf68245a0518599300fcc1fc4fa9a45)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **coverage:** measure async method bodies ([8200de7](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8200de77ea55f2c16240fcd877b8531d84bc5eea))
* **docs:** indent markers inside fences under list items ([3e28b22](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/3e28b229aabbcced94a55ba675e1086369e412d6)), closes [#315](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/315)
* **docs:** keep one blank line in every generated form ([476a1f6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/476a1f6a7e95ece02b5a8e5fa6ee660356755c61)), closes [#311](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/311)
* **infrastructure:** publish domain events only once an explicit transaction commits, and refuse the synchronous SaveChanges ([#246](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/246)) ([c6e0bf1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c6e0bf1cb56fa8681644e7865dcbf83a5d86259f)), closes [#196](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/196)
* **launcher:** draw each question's choices before the first key, with a hint of the keys ([d50f20d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d50f20deff918b5967eb9af5dabf0a4c9719cfc5))
* **launcher:** materialise each intersection of the refusals it weighs ([0567abe](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0567abe871a84226155d0ea07ce25f155a6ba4c7))
* **launcher:** name the cause and the fix in every first-use message, and leave no empty folder behind ([b7c25de](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b7c25de920e731d8800b68637b6a88a6c4ca6a74)), closes [#206](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/206)
* **launcher:** offer only the choices the template can generate ([46b730c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/46b730c0673096f94f2d742f9c2700c7569ee398))
* **launcher:** put the launcher's arguments after --, which dnx otherwise takes as its own ([#238](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/238)) ([17ad375](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/17ad3754f83b5e29ecdbe7039525c46e7328f412))
* **launcher:** readable on any console, and only what is true and left to run ([#167](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/167)) ([2f61b8a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2f61b8a6be8ae227d7bf8ffb402f5cb97686cbd1))
* **launcher:** refuse a product name that holds an underscore ([cd63152](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cd63152cc5dc95e4ef8c1aa2968d58697152c8bb)), closes [#318](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/318)
* load an owned aggregate by its owner and id, never by id alone ([#249](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/249)) ([683a14c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/683a14c8a7281f77e6b90ff2cae432f8364c87f1)), closes [#191](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/191)
* **mcp:** keep the protocol using only where a key's scope is tested ([2234b43](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2234b433bde3bfc80f34bfdd9da4fdcbdac28aa2)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **new-feature:** decode a client's instant in UTC ([734059e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/734059ea37fd1c67e385473be075f6e072e485f0)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222)
* **new-feature:** name the account sweep's lease after the project ([eb1d006](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/eb1d006191efccf0fd1ef7f97abd5dfa2ab76858))
* **new-feature:** write the table test SQLite holds ([13ea95d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/13ea95d06abc731a512365e5479c43fbc14a23e6)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222)
* **new-field:** prove the rollback on Windows too ([44dc626](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/44dc62673228d1e89500590f2242195943fb79da)), closes [#223](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/223)
* **new-use-case:** route an action from the controller's own routes ([b61afb9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b61afb9ee65a847ce7eb076a41b8abd2b004b9d6))
* **package:** give each package a readme nuget.org can render ([63a4b18](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/63a4b18c6bc9f21ba0618818f40680010e333aa9)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **portal:** send the API's security headers and deny by default ([5b8c26d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5b8c26d0361ae4f4cfedbe56972b952eb688ef11)), closes [#214](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/214)
* **security:** fix the session ReDoS and audit npm packages ([63b8676](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/63b8676e4f61fb293c5d409a3380e256909ee104)), closes [#213](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/213)
* **style:** prefer primary constructors, as the code does ([6f0ec3d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6f0ec3dfbcd01e848c018982bb6f2a2d03284ee2)), closes [#218](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/218)
* **tasks:** add a scaffolded project to its layer's solution folder ([409b7f5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/409b7f54c34e81b731dbd850645b3256e7fa4c7c)), closes [#314](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/314)
* **tasks:** build the test projects that need Docker in check and test ([37f23dc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/37f23dc80bf864faa1d76819d674af9c8b8e0382)), closes [#301](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/301)
* **tasks:** keep the web audit in a project without the API ([639ca42](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/639ca42e6997b8056e07833b5cc0b200ed129608)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **tasks:** name the target of dotnet test, as newer SDKs require ([a672aca](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a672aca1c9c4a89d3347b2685d6191e3329a9b3c))
* **tasks:** pass --dry-run and --fields to the generators that take them ([05602fc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/05602fc42bee3ba310b8772f9b6a48dded7581e2)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222) [#224](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/224)
* **tasks:** print the project name presentation-join asks to scaffold ([6b22f54](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6b22f54723690d2d64b6c28ec67f9dd03c3132e3))
* **tasks:** scaffold the official CLIs' files with LF endings ([261d316](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/261d316f1faee1f39c52bdb508e81f84d02ba6e8)), closes [#218](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/218)
* **tasks:** show each task's and each launcher command's own help ([564d5d8](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/564d5d84110fc152713f29dff77adcc57bc1dca1)), closes [#204](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/204)
* **tasks:** target a migration's context, refuse an empty migration, and list the identity step under Next ([76fdefd](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/76fdefdc11b6e97dbfb529a22b1ed37eae7e069f)), closes [#202](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/202)
* **template:** a folder install copies no editor or assistant folder into the project ([#130](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/130)) ([c6d0f38](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/c6d0f38ee0e7fd8c8e1c8cac801f902727174d23))
* **template:** drop the project-file variants no source copies ([8638c82](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8638c82f436dda814bc64f627f0259db25449c65))
* **template:** drop the usings no portal or client test form reads ([8e542a7](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8e542a744fd442f2009bfc1e9024569f717155a5)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **template:** generation says bootstrap must run now, what it creates and what it needs ([#161](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/161)) ([4693258](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4693258c8876dd9f5e5ccf569d3487c4f161465b))
* **template:** give each generated project its own user secrets ([ec9df01](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ec9df01cdf6b17951742fb5f385510b8d0cd48dd))
* **template:** keep the package icon out of content/ ([116bac6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/116bac66d7c77e00f3be00c39e5175d01df7cf58)), closes [#310](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/310)
* **template:** keep the product's name out of the middle of a word ([1b304bf](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1b304bf84641e794bf0f5110adc1d16da8400e2f)), closes [#260](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/260)
* **template:** leave Avalonia's developer tools out of its starter ([9457042](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/94570427dd8c35fc2f6ce70a33e860cc68c52edc)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **template:** leave the Markdown blank-line rule out of a project ([9ba4d7e](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9ba4d7e15ae1a184236e4ad283547b7ec910cff4)), closes [#311](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/311)
* **template:** place a part's decisions index rows at their rank ([af4de7a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/af4de7a4e54f85ab7522842cc1ae8a4102c8fd9d)), closes [#307](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/307)
* **template:** place the WPF starter's decisions row at its rank ([4ef32ef](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4ef32ef71e0a30449d6207090de6b7d542b1b6c5)), closes [#307](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/307)
* **template:** put the MCP server's decisions at their rank when added ([9ab1b90](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9ab1b9073f307f5336648596d41540fa66e895d6)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* **template:** refuse a product name that starts without a letter ([4fad94b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4fad94bab2214f9efa627c92e14f1ae7f37cce57)), closes [#314](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/314)
* **template:** refuse a product name under two characters ([6969aa1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6969aa1800eacecf7b350194d698d4e7e7aa7934)), closes [#260](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/260)
* **template:** save template.json without a BOM, which release-please cannot parse ([#237](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/237)) ([b8d30cc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b8d30ccb9c9569ba04aa492d9186a592bb9d6f94))
* **template:** stop offering the database value no version generates ([139fe1f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/139fe1f75d6b4afc58ded1d79d6b7ce3e2028faa)), closes [#207](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/207)
* **template:** word the options for the question that asks them ([2f81c76](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2f81c76a9748e8e7951283f6fc7e5a5a4e383a27)), closes [#165](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/165)
* **tests:** import only what each database form's cascade-path test reads, so a SQL Server project builds ([b8f1596](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b8f159602cbd3a5dced2c53203037475ed189875)), closes [#257](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/257)
* **tests:** import the file use case only where a test reads it, so a files-only project builds ([#255](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/255)) ([019c4f8](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/019c4f8e2cef48bad9652df3e1fa5381260c3ed2)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)
* **tools:** a generated test name gives a feature named with a vowel the article an ([#174](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/174)) ([4f2f5d3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4f2f5d37f6083dc18091b1d1585cab7a60d60df4))
* **tools:** keep the generators' temporary names to three characters ([4b70d28](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4b70d28341e63fb305952b79c7d7d6b90a54077c)), closes [#297](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/297)
* **upgrade-report:** read a GUID dotnet new draws as no difference ([8563cd1](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8563cd1050df0500f3084c4a177e7ff94d48563e))
* **web-join:** make every line a web app needs, leaving those in place ([50c12bc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/50c12bce44365f286721a0c9abed7bd1f4595f8f)), closes [#151](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/151) [#225](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/225)
* **web-join:** record each web app's port in the project file ([98581c9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/98581c938236339ef017a89d9994ab251a678b49)), closes [#227](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/227) [#225](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/225)
* **web:** give the WebAssembly app its provider from the environment ([97ed7d2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/97ed7d294814e8043dacde8036b16c1312a689cf))
* **web:** keep a web workspace on one Node.js version ([#31](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/31)) ([22c4e93](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/22c4e932759573faea95c6282df752fd54e0046e))
* **web:** keep the WebAssembly sign-in script from calling eval ([64e67a7](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/64e67a75803ca415d7ad936088c4d12f6f57fa96))
* **web:** key the redocly patch on a range, not on one release ([1ae9d55](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1ae9d5596598348db3c856e31293fb16ed7ce20e)), closes [#280](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/280)
* **web:** pin the package template's tools at the workspace's versions ([bda1bfe](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/bda1bfe70c0630b9f7b5cecade0c5c925dfe9d72))
* **web:** run the workspace's tests one package after another ([e0a8a69](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e0a8a693454c391224f70078157f31a9ca37dd83))
* **web:** serve React Router's page without Google Fonts ([cf4d613](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cf4d6137896582c89b09692a4f499d6bb9b799ac))
* **web:** web-scaffold fails instead of waiting when a CLI asks a question ([#124](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/124)) ([556cbaa](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/556cbaa64d8d66ce2f95486fc8a8992ae1ea7a02))
* **worker:** drop the joining section once presentation-join applied it ([3b51d3f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/3b51d3fdcbf09fb32d27a432b19a7f1281af72fa))
* **worker:** give the join instructions only to a worker that joins ([58d2ea4](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/58d2ea46fb1e1bc627e77bc0e0ee8e9a4e1b6407))


### Performance

* **new-feature:** build once and run the later steps from that build ([05462fe](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/05462fe09fdf3900230e9ef790f71c67b834188d)), closes [#222](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/222)


### Documentation

* **agents:** rewrite AGENTS.md as a short brief leading into the docs ([371fb10](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/371fb108ea1a7698d19a6088e064415835b2dcd7))
* **agents:** say plainly when a project line is no rule yet ([7c8e24d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/7c8e24dd87092a386a6b4c590add9a35e5ce93c8))
* **build-and-ci:** list every task in the task table ([28f67b0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/28f67b0823cb54dec44eae3b432ed898a0c11d39))
* cite a web app's compose.yml by a path every project can hold ([cd4d126](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cd4d1265ba985fd22aa4437c1158ac466d44e371))
* cite no API page in the guide to code templates ([e5f82a3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e5f82a372a9db3a051dc0b1306e7160d97f8ed11))
* cite the commands of dotnet cleanarch in guides and decisions ([7e49427](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/7e4942739f475d7c9d505734a9eff5ca518b00a7)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **cli:** describe check, docs-index, upgrade-report and coverage ([2651267](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2651267a104d7cf151ebfacf558102916af5a6a2)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **contributing:** add a first-contribution path ([01d865a](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/01d865a2b63015a7ef484c7f8e59c31f14c766f9)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* correct what the documentation audit found false or missing ([38e2dd5](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/38e2dd57809607819f7cf8410eb8a09db6f8620b))
* decide the code templates' key, language and vocabulary ([f1ad070](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f1ad0702d4e69f44239fb8f1f97c921aefc8199a))
* **decisions:** correct the decisions where they no longer match ([d838f5b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/d838f5bff8a9825d8847a649ec982ef876b775a8)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* **decisions:** record the decisions the code holds without an ADR ([b5ee44d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b5ee44d93563866f41d16b2e0acfac5a8a7c2d96)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* **decisions:** replace ADRs 0003 and 0004 rather than amend them ([8fcf54f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8fcf54f75c3ac77cf3f3c3ef4a4311eca29169ca)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* **decisions:** state the WPF decisions as forces ([1aded2d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1aded2d1aae7efda982da023f669595ffda46b79)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* describe the project tool as the one way to run this project ([00e5572](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/00e5572934c3f01b4f0bf38b51e73c0a7f94ddee)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* describe what the project tool offers and what it decided ([6d34203](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6d34203eb1c360cd68e1116f0e123ef44e8bb67c)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* explain how a project changes the code generators write ([8711374](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/87113743e23cf5e9ee83feafd92308cc7d2c0026))
* **features:** add a page for each example feature ([23adc0b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/23adc0bf52a9fc34f4eecb842b4efa12399d90d1)), closes [#209](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/209)
* **github:** send questions to Discussions from the issue chooser ([fc2b1df](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/fc2b1df32c281fcc4c197a403c92ce1704b1e40d)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **guides:** make the guides and references say what the code does ([b2aeb5b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b2aeb5ba86a061fc869feec2706611ee7e67514b)), closes [#208](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/208)
* **guides:** write the recipes a team needs once past the first feature ([32d3f45](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/32d3f45cd8c55bb14edefbbac011eb4dbfca8fe4)), closes [#212](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/212)
* hold the decisions to the code templates as built ([99664c0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/99664c07c9af3bbad377ab36443549313df54166))
* **layout:** name the Blazor WebAssembly exception without citing a folder some projects lack ([9a5018c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/9a5018ccc51cb1ddb4d72f8cbbaf12d807faba01)), closes [#198](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/198)
* make the first-use pages true for every shape of project ([5ef1ebd](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/5ef1ebdcd5c583d6e4ca34592f48ce3b93d5881f)), closes [#207](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/207)
* mark code templates done on the roadmap ([a32d0a2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a32d0a2839aee55e66423cba92e23a378bac896d))
* name every generator's flags on the maintenance page ([97c7706](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/97c7706e9f380cff5da67d71995cebae3eb1b417))
* open every page under docs/ with a frontmatter ([87a8b6c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/87a8b6ca410d5183c8ce10c6a25151a841d517cf)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* point the project pages to code templates ([be939bd](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/be939bd7b6a61a337623d6db140e02ead37e49ca))
* **readme:** give the What you get table its headers ([b1f5f18](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b1f5f18ef2e4898d7ba5b59376dbad408e3e722d)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **readme:** present the template on the repository's front page ([28ecebc](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/28ecebcac5eed3611691f33a8dd7f60ca39a86b3)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **readme:** say why Mediator and no AutoMapper ([a672b3c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a672b3c11ac94f12dfa8267755c17560dfd94662)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **readme:** show what a project does after it is created ([b766e2c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/b766e2c88cda4b853eaa9332d8f63990de22e281)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* **readme:** write the --web-name values as code ([e3295c2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/e3295c2945484a0e30e06afc7478df7ac1f56dff)), closes [#219](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/219)
* record the tool pinned at generation in the template decisions ([52fdf8f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/52fdf8f6abd2cc66fceadf8d95df3e67c002ca71)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **reference:** say which commands skip Windows tests and what verify formats ([fea4592](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/fea4592ec8c188d3d77e60a4e14b1d3673c031cd)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* say how to take the tool's change to a copied template ([1e8036d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1e8036d3d48899545f48128b6563f57c1efb84d2))
* say why domain events keep their own dispatcher beside Mediator ([#245](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/245)) ([ffb99b0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/ffb99b02c7c4ce4254858bcc009c5284aeb4afeb))
* start a project with dotnet tool restore and dotnet cleanarch ([a5cd51b](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a5cd51b31b7b4d965d2223c0323018ea9d204eff)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **template:** a roadmap of the web apps and presentation projects to come ([1e66656](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/1e6665636689ceee1a06edbf38533a8a5b5946e1))
* **template:** describe the launcher's projects as they now stand ([eb9d43c](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/eb9d43cfccd60b88aa06e35552cdbd3da267c553)), closes [#163](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/163)
* **template:** drop the shipped WPF app from the roadmap ([cc12fd8](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/cc12fd8112476db14811f778a4338cb2dccaed9f)), closes [#208](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/208)
* **template:** name the leading-letter refusal on the pages ([0b48f04](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0b48f0454efce397a6d2911ae195677c9ebc093f)), closes [#314](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/314)
* **template:** name the self-test gates and the checkout's migrations ([11e66ad](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/11e66ad7287c91712a20df7e516f331e06244e07)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **template:** number ADR 0027 as 0027 in its title and heading ([a4c5ef0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/a4c5ef03a5818a53fc6d1870068834a4b36bfc25)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)
* **template:** propose ADRs 0017 to 0019, dotnet cleanarch as the project's own CLI ([#164](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/164)) ([f4381c2](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f4381c20def01c9db2d0877740e6439291d03c69))
* **template:** replace the template decisions that amend one another ([8ce3bb3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/8ce3bb39ff4b6c0cb4cd28a22e3f654a85ed7573)), closes [#229](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/229)
* **template:** say what proves a GitLab pipeline now that gitlab is offered ([#49](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/49)) ([4f7b7d9](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/4f7b7d96055b0a17e96f3eb2e3fd209c60085992))
* **template:** the roadmap plans server rendering after the presentation projects ([#129](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/129)) ([6e7ad7f](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/6e7ad7f36ef4c97f55498aaa3fe48531ee3a2a08))
* the author answers for every line, with no tool attribution in commits or pull requests ([#170](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/170)) ([006f888](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/006f888c78a90bb7b930e029bbacd96ab82ef0ae))
* **upgrading:** say how to read a newer version's changes ([0a38fb3](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0a38fb3c008a284f64c8090f9c9e5e15441b878f)), closes [#210](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/210)
* write a marker inside a table as a row of one cell ([2c716c0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2c716c0b7ab0328d75f35b2218be89c64961635b)), closes [#308](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/308)


### Refactoring

* **auth:** record security events asynchronously, and forbid the synchronous SaveChanges ([#251](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/251)) ([74fb659](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/74fb659d4d566f71adbb93d0e5c1e84a1a78dca2)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)
* lowercase the root folders src, tests, tools and launcher ([af2aba0](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/af2aba047f4ce5106003ce9d9ac9862ada0547b6)), closes [#364](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/364)
* read time from the BCL TimeProvider, and forbid DateTime.UtcNow outside it ([#248](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/248)) ([67e7fbb](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/67e7fbbfcc86e96c93f96268af9a71d05d693c9f)), closes [#195](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/195)
* run the reminder and file jobs on one interval pass, and share one retry backoff ([#253](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/253)) ([0c62969](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/0c62969ee14a08f63e68738b693679c3170908e7)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)
* **template:** drop --agent-files from cleanarch-presentation ([26f76a6](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/26f76a64baf43e87327089b2380c5e5b5cdc35a7))
* **web:** write the web schema from the reference the tool names ([931733d](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/931733df0b6f5c89169d2748499a5209b90cc577)), closes [#363](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/363)
* **wpf:** make the WPF app one Windows project ([51897a4](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/51897a44bb9f0e846f7daa282dfb304bcc808c7f)), closes [#293](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/293)


### Tests

* prove the MCP key and caller guarantees without examples, and record every domain event ([#252](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/252)) ([2304e48](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/2304e48db4b7d68ba1c47da1a789c3feca30a7ca)), closes [#247](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/247)


### Build

* save Markdown as UTF-8 without a BOM ([3806621](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/3806621ecfd62b9485aab779a7d943c2ebac233b)), closes [#230](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/230)


### Chores

* carry develop over from the previous repository ([#22](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/issues/22)) ([f450944](https://github.com/Pierre-MarieMarchio/cleanarch-dotnet/commit/f4509444181b2e0158ce94eec4b13e64055011f2))

## 1.0.0 (2026-10-22)


### Documentation

* **security:** ask a SQLite project to move to a server database before it deploys ([32b93c6](https://github.com/Pierre-MarieMarchio/Template-DotNet_Clean_Architecture/commit/32b93c6dee8d384f567039de1699a83e508a414a))

## 1.0.0: what the first release contains

### Added

- **`--database`.** A generation option naming the database a project runs on: `postgresql` (the
  default), `sqlserver` or `sqlite`; `mysql` stays refused, as its stable EF Core 10 provider cannot map
  the owned JSON entity Identity's passkeys use. A project carries its provider only, no setting
  chooses it, and none ships a migration: `bootstrap` creates each context's initial one. SQL Server
  keeps the schemas, takes its leases from `sp_getapplock` and keeps the row version as a counter the
  save stamps (`VersionStampingSaveChangesInterceptor`). SQLite is a development database: each
  feature's schema becomes a table prefix (`SchemaAsTablePrefixConvention`), leases are in process,
  `DateTimeOffset` is stored as binary at 0.1 ms, text folds ASCII case only, the API refuses to start
  outside Development, and the project has no release workflow, migration bundle or database service
  in Compose. A search lowercases both sides and compares with `LIKE` on every provider. The pages
  that name PostgreSQL, a schema or `xmin` say the right thing for each provider, and
  ADR 0021, generated with SQLite projects only, records the SQLite refusal.
- **`--agent-files`.** A generation option, `true` by default, that leaves out `AGENTS.md` and
  `Src/Web/AGENTS.md` when `false`; `cleanarch-webapp` takes it too, read from the project by the
  launcher. The pages that point to `AGENTS.md` point to the page that owns the rule when it is
  absent.
- **Callers authenticated by an external OpenID Connect provider.** `AddExternalAuthentication`
  (`Api/Features/ExternalIdentity/Composition/`) validates access tokens from Keycloak, Entra ID,
  Auth0 or any provider with a discovery document, configured by the new, start-up-validated
  `Authentication:External` section. The provider's (issuer, subject) is mapped to an internal
  `UserId` in the new `external_identity.ExternalIdentities` table of the business schema, created on
  the caller's first request under a unique index (a lost race reads the winner's row, so one caller
  is one owner) and refreshed when the provider's email or name changes; `ICurrentUser` returns that
  id, never the subject, and a token that cannot be mapped is a 401. Roles come from a configurable
  claim path (`roles`, `realm_access.roles`, a namespaced claim) or from the local
  `external_identity.RoleAssignments` table, and the `Administrator` policy requires the configured
  role. `ExternalIdentityReminderRecipientDirectory` reaches a reminder's owner through the address
  kept on the mapping. `dotnet new cleanarch-webapi --auth external` generates a project composed
  this way: no Auth projects or tests, `AddExternalAuthentication` in `Program.cs`, the
  `Authentication:External` section in `appsettings.json`, `.env.example` and Compose, the two
  `external_identity` tables in the business `InitialCreate` migration, the external reminder
  recipient when `Reminders` is generated, and the examples and `Files` as with `local`. The
  integration tests sign callers in with tokens from a stand-in provider. With `--auth local` or
  `--auth none` none of it is generated.
- **A public API without the supplied accounts.** `dotnet new cleanarch-webapi` takes
  `--auth local|external|none`, `local` by default. `none` generates no Auth projects or tests, no
  account routes, no identity schema, migration, readiness check or development seed, no
  refresh-token purge, no `Administrator` policy and so no maintenance endpoint, and no `Jwt`,
  `Identity` or account-mail settings in configuration, Compose or the release workflow. Public
  access is stated in `Program.cs` as a fallback policy that admits every caller, with no
  authentication middleware; `ICurrentUser` answers no one, so an operation that needs a caller still
  fails, and an `[Authorize]` endpoint errors rather than opens. `--auth none` refuses the examples
  and `Files` at generation (`Missing mandatory option(s)`, naming the option to add), and
  `Tools/NewFeature.cs` refuses to scaffold a feature in such a project. The email module stays in
  every mode.
- **Choosing the example features.** `dotnet new cleanarch-webapi` takes `--examples none|notes|all`,
  `all` by default. `all` generates `TodoLists`, `Reminders` and `Notes`; `notes` generates `Notes`
  only; `none` generates no example feature. `Reminders` needs `TodoLists` and comes only with
  `all`. What examples share follows its real users: the domain's and the application's tagging
  stays while `TodoLists` or `Files` is generated, the reminder recipient directory, notifier and
  job leave with `Reminders`, and the migration, the snapshot, the composition calls, the settings,
  the sample requests and the tests whose subject is an example are conditioned the same way as
  `--files`. Every combination with `--files` builds without warnings, passes its Docker-free tests
  and reports no pending model change for either context; a unit-test project left with no test
  yet accepts a run of none until its first `Features/` folder appears.
- **Generation options.** `dotnet new cleanarch-webapi` takes `--files`, `--ci` and `--sonar`, all
  `true` by default. `--files false` generates no Files feature in any layer, no storage module or
  its unit-test project, no MinIO services, no `Files`/`FileWorker` settings and no `files` schema
  in the business migration; `--ci false` drops the build and release workflows and the workflow
  gate; `--sonar false` drops the local SonarQube profile, the scanner image, task and tool, and
  the SonarQube Cloud workflow, which is generated only when both `--ci` and `--sonar` are true.
  Each generation builds without warnings, passes its Docker-free tests and reports no pending
  model change for either context. Exclusion is `template.json` path conditions plus conditions
  written as comments in the files that compose a module, so the repository itself still builds
  and tests as the default generation.
- **Upload through the API.** `POST /api/v1/files/uploads` takes `multipart/form-data` with
  `category`, exactly one `file` part (its `Content-Type` is the declared media type) and an
  optional `name` (the part's file name otherwise), and answers `201` with the file `deposited`,
  its `ETag` and a `Location` — the state a confirmed direct upload reaches, and the same
  background inspection follows. The API computes the SHA-256, commits the registration through
  the same quota path as a direct one, writes the bytes outside the quota lock and commits the
  confirmation; when the write or the confirmation fails the registration is removed at once, and
  a removal that fails too is left to the abandonment purge and the orphan sweep. The body limit is
  raised for this endpoint alone to `Files:ApiUploadMaxBytes` plus 16 KiB of framing (`413`
  beyond); a file above the ceiling or the category maximum is `400` on `file`; a category without
  `Api` in `UploadModes` is `409` `storedFile.apiUploadUnavailable`. Not `[Idempotent]`: a retry
  uploads a second file. It is the upload path of the `FileSystem` store, which cannot sign.
  `RequestBodyLimitAttribute` is the mechanism an endpoint uses to raise the limit from
  configuration.

- **File categories declared in configuration.** `Files:Categories` names each category a file may
  be registered under — allowed media types, maximum size, `Scan`, `VerifySignature`, `Visibility`
  (`Private` or `Authenticated`), `UploadModes` (`Direct`, `Api`) and a `Quota` — validated at
  start-up together with `Files:ApiUploadMaxBytes`. `POST /api/v1/files` requires `category`;
  every file representation carries it; the quota is counted per owner and per category; the
  inspection scans and checks signatures as the category says (markup is refused in every
  category); and an `Authenticated` category lets any signed-in user download an available file
  of it. `appsettings.json` ships `document` and `image`. **Breaking** for an existing client: a
  registration without `category` is `400`. The business `InitialCreate` gains the `Category`
  column and its index.

- **A local file system store for the Files feature.** `Files:Storage:Provider` chooses `S3` (the
  default) or `FileSystem`; the latter keeps each object at `RootPath/<object key>`, writes through a
  temporary file published only when its size and SHA-256 match, and refuses a key that resolves
  outside the root. It cannot sign URLs, so registering for a direct upload answers `409`
  `storedFile.directUploadUnavailable` and `GET /api/v1/files/{id}/content` streams the bytes
  (`200`, attachment, `nosniff`, `no-store`) instead of redirecting.

- **A feature with no domain model: `Notes`, and the question that chooses its shape.**
  `api/v1/notes` is a caller's private notes — list, read, create, replace, delete — and it is built
  as a service over storage rather than as an aggregate: `INoteService` with one method per
  operation, an `INoteStore` that stages writes and an `INoteQueries` that projects reads, a
  `NoteRecord` that is the only model there is, and no mapper, tracker or domain-event source. The
  rule it teaches is one question, *does the feature have rules that protect a state?* — `Notes` has
  none, `TodoLists` has several — and `docs/ADDING-A-FEATURE.md` states it, tabulates both shapes and
  documents the path from the simple one to the rich one. Both shapes keep the same commit boundary
  (`IUnitOfWork`), the same CQS (a read stages and commits nothing) and the same modular rule.
  The guarantees are the ones the other examples make, held at the real boundary: the owner comes
  from `ICurrentUser` and never from the body, and a missing note and somebody else's are one `404`
  `note.notFound`; a single read publishes an `ETag` and an update or delete honours `If-Match`;
  the version is the row's `xmin`, and the `UPDATE` and `DELETE` carry the version they loaded, so a
  writer that loses a race is refused by the database (`409` `concurrency.conflict`) and a stale
  `If-Match` is `412`; and the create takes `Idempotency-Key`. The business `InitialCreate` now
  includes `notes.Notes`; the template is unreleased, so the migration was regenerated under the same
  id rather than followed by a second one, and a development database that applied the previous one
  has to be recreated.
  **Two architecture rules were adapted, and none weakened.** A feature may omit its domain folder
  only while it is a service over a store port with no use cases
  (`FeatureIsolationTests.AFeatureWithoutADomainFolder_IsAServiceOverAStore` replaces the rule that
  every feature has one), and `Validators/` and `Stores/` joined the two closed folder vocabularies
  as the words this shape needs. The port-width rule is unchanged, which is why storage is a read
  port and a write port.
  `docs/API-CONVENTIONS.md` now says what it always meant: `ETag` and `If-Match` belong to versioned
  resources, and a resource without a version is last-write-wins.
  `docs/REMOVING-THE-EXAMPLE-FEATURES.md` lists what removing `Notes` takes out.
  Integration tests for it need Docker and are compiled but were not run where this was written.

- **Features meet only through ports and domain events, and a test says so.**
  `FeatureIsolationTests` fails when a type under `Features/<A>/` of the application or persistence
  project names anything of `Features/<B>/` except its `Ports/` or its domain `Events/` — no record,
  EF configuration, `DbSet`, DTO or use case. `Reminders` was the one offender: it read
  `TodoLists`' item rows from its own persistence folder and loaded a whole list (up to 500 items)
  to check a single item. It now asks `ITodoItemQueries`, a two-question port that `TodoLists`
  declares and implements, and `GetRemindersUseCase` reads through a new `IReminderQueries` that
  filters and orders in SQL instead of loading aggregates and filtering in memory. `IReminderTargetQueries`
  is gone. Routes, responses and error codes are unchanged. The idea is that every feature stays
  extractable: the day one leaves, the port is the only thing the others need replaced.

- **SonarQube analysis, in its own workflow rather than in the gate.**
  `.github/workflows/sonarqube.yml` analyses the solution on SonarQube Cloud, and the six gates are
  unchanged: the gate stays answerable from a clone alone, with no account and no token, so a
  verdict that needs a third-party service gets its own file. A generated project inherits the
  workflow blocks on the quality gate, so a red gate is a red check — affordable because the default
  gate judges new code only, which lets existing debt sit while keeping new work clean. It arrives
  switched off — the job is skipped unless the repository variables `SONAR_ORGANIZATION`
  and `SONAR_PROJECT_KEY` are both set, and skipped for pull requests from forks, which get no
  secrets. Hard-coding an organisation key would have pointed every generated project at somebody
  else's dashboard. Coverage reaches Sonar from the Cobertura reports and the `coverage.runsettings`
  the coverage gate already uses. Locally, `dotnet run Tools/Tasks.cs sonar` runs the same analysis
  against a SonarQube Community server behind the `sonar` Compose profile;
  `Tools/sonar-scanner.Dockerfile` supplies the JRE the scanner needs, so the prerequisites are
  still the SDK and Docker.

- **One more use case in a feature: `dotnet run Tools/Tasks.cs new-use-case Invoices ApproveInvoice --kind command`.**
  `--kind command|query` is required and `--input id|none` defaults to `id`, a command or query carrying
  the id of the aggregate the feature's repository names. It writes the use case's folder (the input,
  its validator, the named interface, the use case), a query's DTO, and with an input one unit test that
  an empty id is refused before the use case acts; the use case validates, then throws
  `NotImplementedException` until its body is written. It edits nothing: `AddFeature` registers it by
  folder, and the controller action, its using and constructor parameter, and a query's response
  contract are printed. A feature of the simple shape, a missing feature and an existing use case are
  refused before anything is written. When the feature has a manifest, the files join it, so
  `remove-feature --apply` still removes an untouched feature whole. Its `--self-test` runs with the
  other hygiene gates.
- **`dotnet run Tools/Tasks.cs remove-feature Widgets` reports what removing a feature involves, and `--apply` undoes an untouched generated one.**
  The report lists the files the feature owns, what elsewhere is confirmed to depend on it (a registration,
  an EF model configuration, a migration or the snapshot, a use of its types or namespace), what only
  contains its name, and the checks to run afterwards; it writes nothing and can be run on a dirty tree.
  `--apply` reads the manifest `new-feature` wrote and deletes the listed files, the manifest and the
  folders left empty, only when every file still hashes as written, nothing was added to the feature's
  generated folders, and none of the six hand edits the generator prints has been made. Otherwise it
  refuses before deleting anything. A migration is never touched, and a feature with no manifest, such as
  a shipped example, is only reported. Its `--self-test` generates features with the real generator and
  removes them, with the other hygiene gates.
- **A feature generator with four choices: `dotnet run Tools/Tasks.cs new-feature Widgets Widget --access shared`.**
  `--access owner|shared` is required; `--operations` (default all five), `--model record|aggregate`
  (default `record`) and `--concurrency none|etag` (default `none`, last-write-wins) are the rest. The
  `record` model writes the shape of `Notes` — a service over a store and a queries port — and
  `aggregate` the shape of `TodoLists` — an aggregate, a repository port, one use case per operation,
  a record, a mapper and a tracker. An operation left out produces no file, method, route or
  registration. Combinations that would not hold together, such as `create` without `get` or an
  aggregate without `etag`, are refused before anything is written. The product prefix is read off the
  tree, so it works in a project generated under its own name. **It creates files and edits none:**
  the registrations, the context edits and the migration step are printed. A manifest of the options
  and a hash of each created file is written to `.feature-manifest/`, for a later removal command. Its
  `--self-test` runs the real command against throwaway trees with the other hygiene gates.
- **An aggregate no longer declares its own audit and version stamps.**
  `AuditableAggregateRoot<TId>` in `AppTemplate.Domain.Core` holds the five properties and the two
  explicitly implemented interfaces that were byte-identical in all three aggregates, so a new
  aggregate wanting audit columns and optimistic concurrency derives from it and writes none of
  them. The guarantee is unchanged — the setters are still reachable only by casting to
  `IAuditable` or `IVersioned` — and `DomainModelTests` now reads it off the whole inheritance chain,
  so a stamp declared on a base is checked rather than skipped. Adopting this in an existing project:
  a `<see cref>` naming an inherited stamp through the aggregate stops resolving, and names the
  interface instead.
- **Ownership and the version precondition are checked in one place.**
  `OwnedAggregate.Require` in `AppTemplate.Application.Core/Common/Ownership/` takes the loaded
  aggregate, the caller, the feature's own not-found error and the precondition, and answers the
  aggregate or the first refusal. An aggregate declares `IOwnedAggregate`; a feature's service keeps
  its named interface and its error, and its body is a call. Which error says "absent" stays the
  feature's decision, so a non-owner goes on being answered exactly as an unknown id.
- **One paginated collection contract instead of one per feature.**
  `FeaturePageRequest<TFilter>` and `CollectionBinding.Bind` in
  `AppTemplate.Application.Core/Common/Collections/` replace the page-request record and the
  binder each collection was carrying its own copy of. A feature declares `ICollectionQuery` on its
  query record — its positional members, and so the OpenAPI parameters it publishes, are untouched —
  and writes only what it alone knows: its policy, its filter and its tiebreaker field. The request
  is unconstructible outside the project that validates it, which is stronger than the per-feature
  records were.
- **Five projects are vendored SDK projects, and a derived project writes only its own features.**
  `AppTemplate.Domain.Core` holds the primitives an aggregate is built from;
  `AppTemplate.Application.Core` the mechanisms a use case is written *from* — `Result`, the
  `IUseCase` marker, validation, paging, idempotency, concurrency, the cross-cutting ports;
  `AppTemplate.Application.Auth` authentication as use cases behind nine intent-shaped ports, naming no domain
  type; `AppTemplate.Infrastructure.Core` the mechanisms no module owns;
  and `AppTemplate.Presentation.Core` what any host needs whatever its transport, with no framework
  reference. The HTTP pipeline is not one of them: only the API consumes it, so it lives under
  `AppTemplate.Api/Common`, behind one `UseHttpPipeline()`, and `Common/` may not name a feature.
  Each has `CS1591` re-enabled, so every public member owes the reader a sentence, and the
  architecture tests hold their boundary — among them that none of the five, nor the domain,
  application or infrastructure projects around them, requires the ASP.NET shared framework, the
  bearer-token adapter in `AppTemplate.Infrastructure.Auth` excepted.
  They carry **no version**: they are vendored, and a project needing something one of them lacks
  copies it and tunes it. Two things follow for anyone adopting this in an existing project. Every
  `AppTemplate.Application.*` and `AppTemplate.Domain.*` namespace a file names may have moved one
  project inwards, and **there is deliberately no `AddApplicationCore()`** — a call that registered
  nothing would read like the seam that makes the split work.
- **`AppTemplate.Infrastructure.Core`, the infrastructure layer's agnostic half.** An infrastructure
  module may not reference a sibling, so two modules needing one mechanism each kept a copy — which
  is what had happened to multilingual mail rendering, written twice at 196 and 131 lines with
  `RenderedEmail` declared in both. The engine now lives here once, as `EmailTemplate`, rendering a
  module's *own* embedded resources and reading the subject out of the template; both modules render
  through it, and a derived project can render its own mail through it too, which neither `internal`
  copy allowed. `InfrastructureModules_ReferenceOnlyPersistenceHorizontally` is widened to permit
  exactly one more edge — the layer's own `.Core` — with its non-vacuity assertion kept, so the
  permission cannot become a hole nothing uses.
- **A cache, behind `ICacheStore`, with one `HybridCache` adapter.** `AddCacheStore()` is one line of
  each host's composition and registers it in process, so the template runs on `dotnet run` with
  nothing to deploy; a deployment wanting a shared second level registers an `IDistributedCache`
  beside that call and no caller changes. The port's documentation is a short list of what may go
  through it: an answer whose staleness is harmless, and nothing that decides an authorisation,
  enforces a bound, becomes an `ETag`, or tells something else what to delete. Two reads consume it,
  and they are new — `GET /api/v1/todo-lists/tags` and `GET /api/v1/files/tags`, the tags an owner
  has already used, for a picker. No read that existed tolerated staleness. Output caching is
  deliberately absent, and `docs/ARCHITECTURE.md` says why.
- **`Common/Tagging/`, the worked example of a business `Common/`.** A to-do item and a stored file
  are both tagged by the same rule, so `Tag` and `TagSet` sit in `AppTemplate.Domain/Common/Tagging/`
  and `TagValidation` and `UsedTagsCache` in `AppTemplate.Application/Common/Tagging/`. **Two folders
  now share the name `Common/` and the difference is load-bearing:** `<Layer>.Core/Common/` is the
  agnostic half and may never know a feature, `<Layer>/Common/` is the business-shared half. The
  sorting test is one question — does it know a feature? — and it is mechanical rather than a matter
  of taste: `TagSet` names `Tag`, and a `.Core` project may name no business type. What is *not*
  there is as informative: the ownership check every feature performs and the paginated read every
  feature exposes are duplicated and agnostic, so they belong one project inwards.
- **`PeriodicJob`, the loop a host's recurring work is written on.** One interval, one asynchronous
  iteration, ticks coalesced so an iteration never overlaps itself, and a return rather than a throw
  when the host stops — so a shutdown log after the call always runs. It is a **primitive a service
  composes, not a base class it derives from**, which is what lets one service run three passes on
  three intervals without three classes. It deliberately decides nothing else: each loop keeps its
  own logs, span and counter names, scope granularity and treatment of a disabled pass. Two defects
  in the worker loops are fixed rather than carried across: a disabled maintenance purge is now
  counted and logged with its consequence, where it had been indistinguishable from a dead loop, and
  the maintenance and reminder loops now run their shutdown log when the stop lands mid-iteration.

- **docs/INTEGRATING-INTO-AN-EXISTING-REPOSITORY.md**, for the case the generator does not cover: a
  repository that already exists and already has root files of its own. Two shapes, measured against
  each other. Putting the generated tree at the repository root keeps every tool working as
  documented, because each one assumes the solution and the repository root are the same directory.
  Putting it in a subdirectory keeps the host's root untouched and costs six repairs instead, all of
  them listed with the failure and the exit code that produced it. The two that surprise: `dotnet test`
  run from the host's root fails on every test project until the host's `global.json` names the test
  runner, and two hygiene gates go red the moment the workflows move to the git root, where GitHub is
  the only thing that will read them.

- **Mail is written in the reader's language.** Every mail ships one template per language — English
  and French — and the subject is that template's `<title>`, so a subject and a body can no longer
  end up in different languages. `AppTemplate.Api` takes the language from `Accept-Language`;
  `AppTemplate.Worker`, which has no request to read one from, uses `Localization:DefaultCulture`.
  A language with no template falls back to English, and `fr-CA` reaches the `fr` template first.
  Adding a language is adding `<Mail>EmailTemplate.<tag>.html` to two folders and nothing else:
  there is no list of supported languages, because a list can name a language no template backs.
  Two things a reader should know before touching this. The repository builds with
  `InvariantGlobalization=true` — the API's runtime image ships no ICU — so there is no `CultureInfo`
  to carry a language in and `CurrentLanguage` carries a BCP-47 tag instead. And an
  `EmbeddedResource` named `*.fr.html` needs `WithCulture="false"`, or MSBuild reads the second-to-
  last extension as a culture and compiles the template into a satellite assembly, leaving the build
  green and every mail throwing at the first send.
- **`Localization:DefaultCulture`**, bound by both hosts from twin options classes, so one deployment
  cannot answer in two languages.
- **`FileWorker:InspectDepositedFilesInterval` and `…Enabled` are reachable from `.env`**, along with
  the six `FILE_WORKER_*` and `REMINDER_WORKER_*` variables `docker-compose.yml` already read and
  `.env.example` never mentioned. The inspection interval is the one a user waits on, and it was the
  only `FileWorker` key an operator could not turn.
- **Five rules over the mail templates** (`EmailTemplateCoverageTests`): the two modules ship the
  same languages, every mail ships the fallback one, every template carries a non-empty subject, and
  no two languages of one mail share a subject — which is what a file copied and translated in the
  body only looks like.
- **A unit test for `AuditingSaveChangesInterceptor`**, which had none: it was covered only through
  the API, where the current user always resolves.
- **`CollectionOrderTests`**, which did not exist, for the one class that sees the paging mode and
  the sort order together.

- **`Storage:PublicEndpoint`, because a presigned URL covers the host it was signed for.** Measured:
  sign for `127.0.0.1:19000`, follow the URL as `localhost:19000` — same machine, same port, same
  server — and the store answers `403 SignatureDoesNotMatch`. Nothing downstream can rewrite the
  host, so the name has to be right at signing time. As shipped, `docker-compose.yml` was minting
  URLs only a container could follow, because the API reaches MinIO at `minio:9000` and a browser
  does not. Defaults to `Endpoint`, so a deployment that never knew the key is unchanged.
  It also moves two validation rules onto the endpoint they were always about: the scheme of the
  signed URL, and the refusal to hand out a bearer right in clear. The typical deployment is the
  inverse of what the code assumed — plain inside the mesh, TLS at the ingress — and was being made
  to allow insecure transport for the internal hop and then sign `http://` URLs for an `https://`
  public.
- **Deposited files are inspected before they are readable, and refused ones are quarantined.** The
  declared media type is now checked against the leading bytes and the content is scanned, which
  closes the two holes `SECURITY.md` had named — SVG declared as an image is refused outright, since
  there is no safe version of serving one inline. Inspection runs in the worker rather than in the
  request, because scanning 200 MiB inside an HTTP call is the same CPU-denial problem as resizing
  an image there: the visible consequence is a `deposited` state a client can observe, and
  `FileWorker:InspectDepositedFilesInterval` is therefore a user-facing latency rather than a
  background cost. **A scanner that cannot be reached never releases a file** — fail-open would make
  an outage the way through. `Quarantined` is persisted and terminal and costs no query a predicate,
  because the one place that hands out bytes guards with an allow-list, which refuses a new state
  the day it is added rather than the day someone extends a predicate. The refusal deliberately does
  not say what tripped it; that goes to the operator's log, not to the depositor. ClamAV speaks its
  own TCP protocol, so it escapes the outbound HTTP policy exactly as the S3 SDK does, and says so.
- **A blocking end-to-end suite, shipped in every generated project.**
  `Tests/EndToEnd/AppTemplate.EndToEnd.Tests` talks HTTP to the Compose stack and to Mailpit's API:
  readiness and an unmapped route's refusal everywhere; with local accounts, register, the mailed
  link, confirm, sign-in, `/auth/me`, refresh, sign-out and the refresh refused after it; with files,
  a deposit on the signed grant read back once inspected; with a web app, its page and its `/api`
  relay. `dotnet run Tools/Tasks.cs end-to-end` starts the stack under its own project name, runs the
  suite and removes the stack (`--keep-stack` keeps it); `dotnet test` over the solution leaves the
  project out. A CI job runs it after the Compose manifest, and the acceptance matrix's defaults leg
  runs it in the generated project.
- **A load smoke test, run non-blocking in CI.** Every timeout, pool size and rate limit in this
  template was chosen by reasoning and none had ever been observed under concurrency. Its thresholds
  describe a broken system rather than a slow one, and a `429` counts as a pass — punishing the rate
  limiter for working would push someone to loosen it to get a green.
- **A third `IEmailSender`, over an HTTP API rather than SMTP.** `Email:Transport` picks between
  them and defaults to `Smtp`, so no existing deployment changes behaviour; an unrecognised value
  stops the process rather than falling back. It is the first real consumer of the outbound HTTP
  policy — a typed client that inherited the whole budget without asking — and it is what a
  deployment usually needs, since outbound SMTP is blocked at most hosting providers. The SMTP
  settings stop being required when the transport is HTTP, which is the point. Adds `Common/Http/`
  to the email module's folder vocabulary and a `Postmark` section whose token is a secret.
- **The rate limiter counts behind a seam, still in-process, and still without Redis.** The
  effective limit remains the configured one times the replica count — that has not changed and is
  documented — but the counting is now one type behind an abstraction, so a deployment that needs a
  shared count writes an adapter instead of rewriting middleware. Not one existing rate-limiting
  test was touched, which is the evidence that the behaviour did not move.
- **A consumer for `StoredFileDeletedDomainEvent`, which three files already claimed existed.** It
  reclaims a deleted file's bytes promptly; the sweep is what makes reclamation correct. The claim
  had been written in the persistence tracker, the worker options and the reclamation use case
  before anything implemented it, and every test was green — so `DomainEventTests` now requires
  every domain event to be either consumed or listed, with the reason, as a decision.
- **Signing in through an external identity provider, without a cookie or a redirect in sight.** The
  client runs OAuth/PKCE itself and posts the provider's `id_token`; the API verifies it — signature
  against cached JWKS, `iss`, `aud`, `exp`, `nbf` — and mints its own access/refresh pair. The token
  model is untouched, so the same endpoint serves a SPA, a mobile app and a desktop client, and
  ASP.NET's cookie-based `AddGoogle()` middleware would have contradicted the whole design.
  **One adapter driven by configuration, not one class per provider**: Google, Microsoft and Apple
  differ in values rather than behaviour, so adding Okta is a configuration entry. A link is keyed on
  `(provider, subject)` and never on the address, because Apple returns the address only at the first
  authorisation. Automatic linking needs both sides to have proved the address — a local account that
  never confirmed its own is refused rather than linked, which is the account-takeover vector this
  decision exists to close. And external sign-in runs the same tail as local sign-in, so two-factor is
  not bypassed by it. `AspNetUserLogins` had been mapped and unused since the beginning; it is now
  what the link is stored in. See `SECURITY.md`.
- **A third example feature, `Files` — the first whose two halves live in different stores.** A
  `StoredFile` aggregate in PostgreSQL and its bytes in an S3-compatible object store, joined only
  inside a use case. Upload is two-step by necessity, not by taste: the API registers metadata as
  `Pending` and returns a signed upload grant, the client writes to the store directly, and a
  confirmation verifies size and checksum against what the store reports before the file becomes
  `Available`. `RequestLimits:MaxRequestBodyBytes` is 65 536 and `IdempotencyFilter` SHA-256s the
  whole body of every `POST`, so routing bytes through the API was never on the table. Reading works
  the same way in reverse: the API issues a signed download grant and never serves a byte.
  **There is no soft delete**: `DELETE` removes the row, and the bytes are reclaimed by a sweep that
  deletes every object no row references — an effect that re-derives its own precondition, so it
  needs no outbox and no tombstone. The object key is *stored, never derived*, so changing the key
  scheme later moves no bytes; it carries an opaque prefix segment reserved for multi-tenancy that
  nothing uses yet, because adding one after two years of files would mean moving every object.
  Adds `AppTemplate.Infrastructure.Storage` (AWSSDK.S3), a `Storage` configuration section, and
  `minio` to `docker-compose.yml`. `FilesController` exposes the six user-facing operations, and
  `AppTemplate.Worker` gains a third loop for the two sweeps — the reclamation one being the
  feature's correctness guarantee rather than housekeeping, which `FileWorker:ReclaimOrphanedContentEnabled`
  says out loud. Content sniffing is deliberately still absent and is named in `SECURITY.md` as the
  gap most likely to be exploited.
- **One outbound HTTP budget, installed before the first outbound call exists.** 10 s per attempt,
  30 s in total, three retries with jitter on safe verbs only, plus the standard circuit breaker and
  concurrency limiter — set on `IHttpClientFactory`'s defaults in each host, so a module that
  registers a typed client inherits it without opting in and cannot forget it. The 30 s is chosen
  against `RequestTimeouts:Default`'s 5 minutes: an outbound call happens inside an inbound request,
  so the enclosing budget must be the longer one. Deliberately not configurable — see
  `docs/CONFIGURATION.md`. Two rules guard the escapes: `NoType_ConstructsItsOwnHttpClient` and
  `EveryHost_InstallsTheOutboundPolicy`. Adds `Microsoft.Extensions.Http.Resilience` (which brings
  Polly transitively) and `Common/Outbound/` to both hosts' folder vocabulary; the worker also gains
  outbound HTTP instrumentation, which its telemetry had skipped on the grounds that it made no
  outbound calls.
- **`ILeaderLease`, so the worker can run at more than one replica.** The reminder pass runs under
  a PostgreSQL session-level advisory lock (`PostgresLeaderLease`, on its own unpooled connection),
  which one host takes and the rest skip without waiting. Losing that host closes the session and
  releases the lock, which is the whole reason for choosing an advisory lock over a lease row with
  an expiry. The guard is in `FireDueRemindersUseCase` and not in `ReminderBackgroundService`,
  because "one host at a time" is a property of the operation and not of the timer that starts it.
  It is **not** a fencing token: leadership can be lost mid-run, so delivery stays at-least-once.
  Adds `Common/Leases/` to the persistence project's folder vocabulary, and one unpooled connection
  per replica running a leased loop to the `Database:MaxPoolSize` budget — see
  `docs/CONFIGURATION.md`, which also says why a transaction-pooling proxy would break it silently.
- **A second example feature, `Reminders`: a flat aggregate, no child entities.** A reminder is
  scheduled on a to-do item, fired by `AppTemplate.Worker` on a due-date query, and cancelled when
  its item is completed. It exists to be *different in shape* from `TodoLists` — what a to-do list's
  persistence needs only because it owns items is now legible from what a reminder does without, and
  that comparison is what decided which parts of the persistence layer were extracted and which were
  left duplicated.
- **Correctness that does not depend on event delivery** . Firing a
  reminder re-reads whether its item is still outstanding rather than trusting the event that should
  have cancelled it, cancellation assigns a state so a redelivery is a no-op, and the case where
  re-reading finds an already-completed item increments
  `apptemplate.reminders.missed_cancellations` — the number of domain events that went missing.
  Deleting an item needs no domain event at all: its reminders retire at their next due date.
- **Changing an email address**, with the current password required, the token issued for and mailed
  to the *new* address under its own named provider, an unchanged answer when that address is
  already taken, and the refresh-token revocation that a security-stamp rotation does not perform by
  itself.
- **Administrative account operations are covered by a lease on every claim** — see Fixed.
- **Raw Kubernetes manifests** under `deploy/kubernetes/`, with `docs/DEPLOYMENT.md`. Deployment,
  Service and Ingress for the API, a Deployment of its own for the worker, and a migration Job
  because the application refuses to migrate at startup. `preStop`, `Shutdown__Timeout` and
  `terminationGracePeriodSeconds` are one arithmetic and the manifests say which constrains which.
- **`docs/REMOVING-THE-EXAMPLE-FEATURES.md`**, a procedure that was carried out before it was
  written, and which states what stops being demonstrated once the examples are gone.

- **A collection-query contract: sorting, filtering and two paging modes.** `GET /api/v1/todo-lists`
  now takes `sort`, `search`, `createdAfter`, `createdBefore`, `paging`, `cursor` alongside
  `page`/`pageSize`. Sorting is multi-field with a direction per field over a **per-feature
  whitelist** (`ICollectionPolicy` in `Common/Collections/`, declared by
  `TodoListCollectionPolicy`); an unknown or unwhitelisted field is refused before a query is built,
  so no caller string ever reaches a LINQ expression. Every order ends in a unique `Id` tiebreaker,
  because without one two rows with equal keys can swap between pages and be served twice or never.
  `search` is a case-insensitive contains on the list name, executed in PostgreSQL via `ILIKE` with
  `%`, `_` and `\` escaped — it is deliberately **not** accent-insensitive. Every bound (page size,
  sort-term count, filter length, cursor length) is a `400` with its own stable code:
  `paging.invalid`, `sort.invalid`, `filter.invalid`, `cursor.invalid`.
- **Keyset (cursor) pagination alongside offset paging.** `paging=cursor` resumes from the last row
  served, so a concurrent insert cannot shift a caller's position and a deep page costs what the
  first page costs. It answers no `totalCount` — counting the match set is a second scan of it — and
  allows one sort term over a field declared keyset-capable (`lastModifiedAt` is not, being
  nullable). The cursor is opaque but unsigned, which is safe because it carries only values from a
  row the caller was served and the read query filters by owner regardless. `PagedResult<T>` gained
  `nextCursor`, and `page`/`totalCount`/`totalPages` are now nullable, being absent in cursor mode.
- **Composite indexes** `(OwnerId, <sortable field>, Id)` for each whitelisted sort field, so both
  the order and the keyset comparison are index-ordered. Migration `AddExampleFeatures`.
- **Idempotency keys on POST.** An action marked `[Idempotent]` — `POST /api/v1/todo-lists` and
  `POST /api/v1/todo-lists/{id}/items` — honours an `Idempotency-Key` header, so a client retrying
  through a flaky network cannot create twice. A replay returns the original status, body and
  `Location` plus `Idempotency-Replayed: true`; the same key with a different body is `409`
  `idempotency.keyReused`; a still-running duplicate is `409` `idempotency.inProgress`; a failed
  request releases its claim so a corrected retry succeeds. Keys are scoped **per user**, so two
  callers may use the same key string. New `Idempotency` configuration section and a `platform`
  schema; migration `InitialCreate`. Auth endpoints are deliberately **not** marked: replaying
  a login would store a bearer token in the database.
- **Policy-based authorisation, with a real operation behind it.**
  `DELETE /api/v1/maintenance/idempotency-keys/expired` requires the `Administrator` policy and
  prunes expired keys — the seeded `Admin` role now authorises something. The role name has one
  declaration (`IdentityRoles.Administrator`) shared by the seeder and the policy.
- **`Cache-Control: private, no-cache` on reads**, which is what makes the strong `ETag` this API
  already publishes worth having: a client may store a response but must revalidate, and
  `If-None-Match` then answers `304`.
- **A request body size limit** (`RequestLimits:MaxRequestBodyBytes`, default 64 KiB) replacing
  Kestrel's 30 MB default. Enforced both by middleware on `Content-Length` — answering `413`
  `request.tooLarge`, a code that already existed with nothing able to produce it — and by Kestrel
  as the backstop for a chunked body.
- **Per-consumer isolation for domain events.** A consumer that throws no longer prevents the other
  consumers *of the same event* from running; the failure is logged with the event and consumer
  type. This narrows the known gap below without pretending to close it.
- **Packaged as a `dotnet new` template.** `dotnet new install <path>` followed by
  `dotnet new cleanarch-webapi -n Your.Project` generates a solution under your own
  name, namespace, and Compose/Docker identifiers — see the README's "Using this as
  a `dotnet new` template" section. `.github/workflows/ci.yml`
  gained a `template` job that installs, generates under a different name, builds
  and tests the result on every push.
- `docs/ADDING-A-FEATURE.md`, a full walkthrough of the vertical `CONTRIBUTING.md`'s
  "Adding a feature" section summarises — aggregate → EF model → mapper → tracker →
  store → use case (with its named interface) → controller → tests → migration —
  plus what to remove by hand if you do not want the `TodoLists` sample.
- **Conditional requests on the `TodoList` aggregate.** Every read of a list or item publishes a
  strong, opaque `ETag`; every write honours `If-Match`, refusing a stale, malformed or
  unrecognised version with `412`, and `If-Match: *` against a missing or someone-else's list also
  answers `412` rather than `404`. A repeated `GET` naming the current `ETag` in `If-None-Match`
  answers `304` with no body. New `Concurrency:IfMatch` section (`Optional` by default; `Required`
  refuses a write with no `If-Match` at all with `428`).
- `ReverseProxy` configuration section (`Enabled`, `KnownProxies`, `KnownNetworks`, `ForwardLimit`)
  driving `UseForwardedHeaders`. Off by default, and the validator **refuses to start** when it is
  enabled with an empty trust set — because ASP.NET Core treats two empty lists as "trust every
  caller", which would let a client choose its own rate-limit partition.
- `TodoItem.MaxTags`, enforced by the aggregate and surfaced as a 400 by the validator, bounding
  per-item tag growth.
- `IDomainEventSource.Restore`, so events survive a failed save and a retry publishes them.
- `Tests/Integration/AppTemplate.Infrastructure.Auth.IntegrationTests`, including a real
  two-context race against PostgreSQL proving refresh-token rotation is single-use.
- A coverage floor in `coverage.minimum`, read by CI and `tasks.ps1` from the same file. Set from
  measurement (86.53% of lines over 974 tests), not invented, and the file records the measurements
  it was derived from.
- `tasks.ps1`, `AppTemplate.Api.http`, `CONTRIBUTING.md`, `SECURITY.md`, this file, and a release workflow
  publishing to GHCR with an SBOM and a signed provenance attestation.
- Dependabot coverage for NuGet, GitHub Actions, Dockerfile **and** `docker-compose.yml` — the last
  needs its own `docker-compose` ecosystem, which `docker` does not cover.
- Test projects for the two modules that had none: `AppTemplate.Infrastructure.Email.UnitTests` (50 tests —
  the SMTP validator that stops a deployment mailing in plaintext was previously untested) and
  `AppTemplate.Infrastructure.InMemory.UnitTests` (30 tests, pinning the recorded-mail ordering every
  integration test leans on).
- CI and `tasks.ps1` discover test projects from disk instead of naming them: a hard-coded list went
  stale once and a project's tests silently did not run. The only exclusion is the architecture
  suite, for the Coverlet reason recorded in the workflow; `Identity.IntegrationTests` stays out
  of `test -NoIntegration` because its rotation race needs a real database.
- Two repository-hygiene gates, in CI and in `./tasks.ps1 hygiene`:
  `.github/scripts/check-doc-paths.py` asserts every repository path cited in the documentation
  exists, and `.github/scripts/check-workflows.py` catches a dangling `needs:`, an action pinned to
  a mutable tag, a missing `permissions:`, an undefined `$VAR` and a `run:` naming a script that is
  not on disk. Both are the kind of defect no compiler and no test can see.
- A third gate, this one an architecture rule rather than a script:
  `ConfigurationSurfaceTests` pairs every `SectionName` an options class declares against
  `docs/CONFIGURATION.md`, **in both directions**. A bound section with no table, a bound key named
  nowhere, and a documented key no class binds are each a failure. The guide opens by promising that
  every setting the application reads is listed in it, and nothing had ever checked the promise —
  it was false eleven times over when the rule was written. The converse direction is the one that
  matters most: a key documented under a name nothing binds is set by an operator, ignored by the
  binder, and leaves the default in place with no sign that it did.

### Fixed

- **Registration no longer tells a caller whether an address is taken.** `POST /auth/register`
  answered `409` `auth.register.unavailable` for a taken address. It now answers `202` with the same
  `{"status":"pending"}` body for every well-formed request, and the address learns by mail what
  happened: a confirmation link for a new account, a fresh link for an unconfirmed one, a notice with
  sign-in and password-reset hints and no token for a confirmed one. The submitted values are judged
  before the address is looked up, both branches hash the password once and send one mail, and two
  simultaneous registrations of one address are settled by the unique index. The
  `auth.register.unavailable` code is gone; a client reads `202` rather than `200`, and the
  `confirmationEmailSent` flag is gone with the rest of the old body.
- **An email change naming a taken address mails that address.** It already answered `204` like a
  free address, but sent nothing; it now sends the same "you already have an account" notice, so
  both paths send exactly one mail.
- **The first owner feature of a project generated without one passes the architecture tests once
  composed.** `new-feature` printed ten edits for it, and the container tests in
  `HostComposition.cs` then failed twelve times on an `IUserAccountDirectory` nothing registered: the
  architecture tests compose the host's graph too, and the sweep the feature brought was absent from
  it. An eleventh edit is now printed — `AddDeletedAccountSweep()` in each composition, and an
  `ArchitectureTestUserAccountDirectory` double registered in `AddHostSuppliedAdapters` and
  `AddBackgroundOnlyAdapters` — and `remove-feature` counts those lines among the registrations
  that hold the shared account-deletion files back.
- **Removing the last generated feature leaves no empty `Features/` folder.** `remove-feature --apply`
  deleted the feature's folders but not the `Features/` folders above them, so a project generated
  without a feature kept four empty ones after its only feature was removed, and
  `LayoutConventionTests.NoFolderInTheSourceTree_IsEmpty` failed on the two under `Src/`. The folders a removal leaves empty above the feature's own now go
  with it, up to the first that still holds something.
- **A project without a feature can compose the first one the generator writes.** `new-feature`
  tells the user to call `services.AddFeature(...)` in `ApplicationModule`, and to register the
  adapters of a model-mapped feature through `AddScopedTracker` in `PersistenceModule`. Both private
  helpers were generated only with an example or Files, so in a project with neither the printed
  edits did not compile. They are now generated with any authentication but `none`, where the
  generator runs at all. The printed steps also name the `using` a file may lack and say when
  `ApplicationModuleTests.cs` does not exist. `SqliteSavingHost.cs` no longer carries an unused
  `using` that failed the build of an `--auth external --examples none --files false` project.
- **Every named route has a name of its own.** `NotesController` and `TodoListsController` both named
  their read route `GetById`, and route names are global: MVC refuses two attribute routes with one
  name and different templates, so the API failed when it built its endpoints. They are now
  `GetNote` and `GetTodoList`, `NewFeature.cs` generates `Get<Feature>` instead of `GetById`, and
  `ControllerContractTests` fails on any name shared by two actions. URLs and responses are
  unchanged.
- **Confirming two-factor enrollment again on an armed account is refused.** It used to replace the
  recovery codes and then write the switch-on again; a refused write left codes nobody had been
  shown. `POST /auth/two-factor/confirm` now answers 409 `auth.twoFactor.alreadyEnabled`.
- **No file can hold the front of the inspection queue, and the orphan sweep reaches the whole
  store.** The inspection pass took the 20 oldest deposited files every time, so 20 files that got no
  verdict (bytes missing, store or scanner unreachable for them) stopped every later upload from
  becoming readable; a file with no verdict is now deferred — one minute, doubled per attempt, at
  most one hour — and queues behind those that have waited longer, and each file is committed on
  its own. The orphan sweep restarted at the first key every pass, so beyond 250,000 objects the
  tail was never visited; it now resumes from a checkpoint saved after each page
  (`files.ContentSweepCheckpoints`) and restarts from the beginning once it reaches the end. The
  `FileSystem` store's temporary files left by a crashed write are deleted once older than a day.
  The three Files passes run under `ILeaderLease`, one host at a time. `StoredFiles` gains
  `InspectionAttempts` and `NextInspectionAt`; `AddInMemoryOrphanSweepCheckpoint` substitutes an
  in-process checkpoint for a host without PostgreSQL.

- **A strict file quota holds under concurrent registrations.** `POST /api/v1/files` read the
  owner's usage and then inserted the row with no lock, so parallel registrations of one owner in
  one category could all pass the check and exceed the quota. In a category whose
  `Files:Categories:<name>:Quota:Mode` is `Strict` — now the default, and the mode of both shipped
  categories — the usage read, the insert and the commit run under a PostgreSQL advisory lock on
  (owner, category), held on a dedicated connection until the commit; a wait over 10 seconds answers
  `409` `storedFile.quotaBusy`. `Indicative` keeps the unlocked check, whose overshoot is at most
  N − 1 files for a burst of N. `AddInMemoryOwnerQuotaLock` substitutes an in-process lock for a host
  without PostgreSQL.

- **A save flushes and publishes only the work staged on the context being saved.** The flush and
  dispatch interceptors acted on every aggregate tracker in the request, whichever context was being
  saved. Committing the identity context while a business write was staged threw, because the flush
  asked that context about a business row its model does not have; and an idempotency claim, which
  commits through a second instance of the business context, published the staged write's events
  before that write was committed, or ever. A tracker now answers `IsStagedOn(context)` from the
  context's own change tracker, and the interceptors flush, refresh, collect and publish only what
  the saved context holds. The identity context attaches no interceptor at all: none of its rows is
  audited, and it has no aggregate and no event. Both cases are reproduced against real contexts and
  real commits in `Tests/Presentation/AppTemplate.Api.UnitTests/Composition/`.

- **A failed reminder commit no longer resends the whole batch, and a reminder that always fails no
  longer starves the ones behind it.** The reminder pass sent up to 200 notifications and then
  committed once, so one conflict — a reminder cancelled while its mail was in flight — rolled back
  all of them and the next pass mailed every one again. Each reminder is now committed on its own,
  and a conflict ends the pass instead of failing it. Separately, a failing notification left no
  trace, so a backlog of 200 reminders that could never be delivered stayed first in due order and
  every pass spent itself on them; the failure is now recorded (`FailedAttempts`, `NextAttemptAt`),
  retried after a wait of one minute doubling to one hour, and reminders are taken in the order they
  became eligible. Delivery stays at-least-once. The initial migration now carries the two
  columns (`FailedAttempts`, `NextAttemptAt`) on `Reminders`; a database that already applied the
  previous `InitialCreate` has to be recreated, since the template is unreleased.
- **Generating from a working clone no longer copies its build output into the new project.** The
  generator's exclude list named `bin/` and `obj/` but not the two directories a test run leaves at
  the root, so a project generated from a clone where the suite had been run arrived with 11 MB of
  coverage reports in it. Both are git-ignored, which is why nothing noticed: the generator copies
  the filesystem, not the index, and the CI job that rehearses generation works from a fresh
  checkout where neither directory exists. Measured before and after: present in the template,
  absent from the output.

- **`AppTemplate.Worker` could not commit anything, so two features were dead.**
  `AuditingSaveChangesInterceptor` read `ICurrentUser.UserId` unconditionally on every save, and that
  host's `BackgroundCurrentUser` throws by design — so every loop that had real work threw at the
  commit. No deposited file ever became readable, and a due reminder mailed its owner and then rolled
  back, mailing them again on every pass, for ever. The stamp now asks `IAuditActor`, a separate
  abstraction answering "whom do we record" rather than "who is calling": the API answers with the
  caller, the worker answers nobody, and both are true. `ICurrentUser` still throws in the worker,
  which is the guarantee that was worth keeping. Verified end to end: a file reaches `available` and
  its bytes round-trip, and a due reminder produces exactly one mail and is marked `fired`.
  Why no test saw it: the one architecture check that composes the worker substituted a benign
  `ICurrentUser` for the throwing one. It now stands in for both adapters as the host registers them.
- **Cursor paging over an offset-only field answered `500`.** `?paging=cursor&sort=lastModifiedAt:asc`
  on to-do lists, and `sort=availableAt` on files, cleared every check and then asked the read side to
  mint a cursor for a column it has no key for, whose only recourse is to throw. It is a `400`
  `cursor.invalid` now, refused where the multi-term rule already was — before a page is served
  rather than when the next one's cursor is minted. It only fired when a next page existed, which is
  why no test met it.
- **`dotnet run Tools/Tasks.cs compose-up` failed on a healthy stack.** `--wait` counts a container
  that exited as a failure, and `minio-bucket` exits 0 by design. It is now named as a
  `service_completed_successfully` dependency of the two services that need the bucket, which is
  both the honest ordering and what stops `--wait` counting it.
- **The worker logged two errors on every cold start**, running its first passes against a schema the
  API had not migrated yet. It now waits for the API's healthcheck — a compose ordering, not a
  runtime dependency, and commented as such.
- **`AppTemplate.Api.http` could not run its own Files section.** Request 39 sent `mediaType` where
  the contract is `declaredMediaType`, so it answered `400` and every request after it depended on
  its id. Its declared size did not match its checksum either, and its upload command omitted the
  `requiredHeaders` the store insists on. The confirmation-token step called the token a query
  parameter, two lines above the comment explaining why it is a fragment.
- **`api` and `worker` did not depend on the object store**, so `--wait` could report the stack ready
  before the bucket existed.

- **The coverage gate was scoring the identity module's 187 unit tests as zero.**
  `coverage.runsettings` named `GeneratedCodeAttribute` in `ExcludeByAttribute`, and coverlet 10.0.1
  does not exclude a source-generated class with that — it drops the whole assembly holding one from
  the report. `AppTemplate.Infrastructure.Auth` holds `SecurityEventLog`, a `LoggerMessage`
  partial, so its own coverage report contained no entry for it at all, and the 68.22% recorded for
  the module came entirely from the two integration suites. The exclusion is now an `ExcludeByFile`
  glob over `obj/`, which is where every source generator writes: measured to give every other
  assembly identical line counts, to the digit, and not to lose the module. Worth eight points of
  total coverage on tests that were already written and already passing. **The floor stays at 85.**
- **The Files loop's metrics and spans were produced and thrown away, and its own documentation said
  otherwise.** `FileInstruments` declared a `Meter` and an `ActivitySource`, and
  `WorkerObservabilityExtensions` named neither — so
  `apptemplate.worker.files.{iterations,registrations_purged,objects_reclaimed,deposits_inspected}`
  and every span of that source reached no collector, at full recording cost. Nothing caught it:
  `FileInstrumentsTests` listens with an in-process `MeterListener`, which sees a measurement whether
  or not anything exports it. This is the loop that decides whether a deposited file ever becomes
  readable.
  The reminder loop, meanwhile, had no diagnostics class at all — its own comments argued in prose
  for exactly the counter the other two loops have, and settled for a log line. It now has
  `apptemplate.worker.reminders.{iterations,notified}` and a span per pass, with `disabled` as one of
  the iteration outcomes so that a loop switched off by configuration reads differently from one that
  died.
  `ObservabilityRegistrationTests` is what stops this recurring: every `Meter` and `ActivitySource` a
  host declares must be named by that host's own `Common/Observability/` registration, `AddMeter` for
  one and `AddSource` for the other, and a host is discovered from the disk so a third one is covered
  the day it exists. Broken and watched to redden by removing the two calls again.
  **`SECURITY.md` accordingly stops advising an alert on the absence of
  `apptemplate.reminders.missed_cancellations` samples** — that counter fires only on a missed
  cancellation, so silence is its healthy state and it could never have been a heartbeat. The three
  iteration counters are, and are now exported.

- **An idempotency claim now carries a lease.** A process dying between claiming a key and completing
  it left the row in progress, and every retry got `409` until the 24-hour retention purge — an
  interrupted write was unretryable for a day, for an operation that may never have happened.
  `IdempotencyOptions:ClaimLease` bounds it, an expired claim is taken over by a conditional update
  whose zero-row result is the signal, and the filter no longer releases a claim whose write had
  already committed.
- **The two-step upload can complete against a real object store.** The upload grant signed
  `x-amz-sdk-checksum-algorithm: SHA256`, which names an algorithm and supplies nothing to check
  against: MinIO accepted the deposit, recorded no digest, and `IFileContentStore.DescribeAsync` then
  threw for want of one — so **every** file deposited through the two-step upload was unconfirmable and
  the Files feature could not work outside the in-memory double. The grant now carries
  `x-amz-checksum-sha256` with the digest the client declared at registration.
  **Breaking for a derived project:** `IFileContentStore.CreateUploadGrantAsync` takes the declared
  SHA-256 as a new parameter. It also closes a second hole for free — the store refuses content whose
  digest disagrees, so a grant authorises one body rather than any body of the right length and cannot
  be replayed to swap content after a file has been inspected and released.
- **Eleven configuration keys the application reads are now in the configuration guide**, which had
  promised since its first line that all of them were. Four sections had no table at all —
  `IdentityTokens`, `TwoFactor`, `ProblemTypes` and `ReminderWorker`, the last of which
  `deploy/kubernetes/configmap-worker.yaml` sets explicitly — plus `RefreshToken:RetentionInDays`,
  `Postmark:ApiBaseUrl`, `Postmark:MessageStream` and `OpenTelemetry:TracesSamplingRatio`. Two are
  worth naming on their own: `IdentityTokens:Lifespan` is email confirmation's token lifespan and
  defaults to **a day** where the two documented siblings are an hour, and `TwoFactor` holds the
  challenge lifetime and the recovery-code count that `SECURITY.md` describes in prose as though they
  were constants. The `Postmark` table also documented a `BaseAddress` key no options class has ever
  had, which is the failure mode the new rule's converse direction exists for.
- **`ConfirmEmailAsync` rotates the security stamp**, which is what makes the confirmation token
  single-use — the command documented it as such while the token stayed replayable until it expired.
- **The worker's container image builds again**, and the release workflow publishes it. Its restore
  layer was missing a project, which `dotnet restore` skips with a log line rather than an error, so
  the failure only appeared at publish time.
- **`dotnet new` regenerates every project guid.** Five were missing from the list, so two projects
  generated from this template shared them. Generated projects no longer carry a working document or a
  `.vs` cache.
- **A stale `<see cref="…"/>` or an unused `using` now fails the build.** Nine cross-references were
  wrong, one of them naming the wrong layer for a repository contract; forty-two unnecessary usings
  were in the tree while the formatting gate believed to catch them was green.

- **A failure the framework produced carried no `code`.** The API's contract is that a client
  branches on the `code` extension and never on prose, but only errors the application *authors* went
  through `ErrorResults` and got one. A body that is not JSON, a route segment failing its `:guid`
  constraint, a verb no action accepts and an unreadable media type all came back as a bare
  `ProblemDetails` — so the promise was broken on exactly the inputs a client is most likely to get
  wrong by accident. `AddApiProblemDetails` now fills the field in for those, and only for those: a
  `code` already set is never replaced.
- **Every error response from a controller action carried `Content-Type: application/json`
  instead of `application/problem+json`.** `ApiControllerBase`'s class-level
  `[Produces("application/json")]` is a result filter that unconditionally overwrites
  `ObjectResult.ContentTypes`, clobbering the media type `ErrorResults` sets on every failure. The
  attribute is removed; `System.Text.Json` is the only output formatter registered, so a success
  response still negotiates to JSON without it.
- **Rate limiting behind a proxy was ineffective.** The limiter partitions on
  `Connection.RemoteIpAddress`; with no forwarded-headers configuration every caller shared the
  proxy's single 10-request window, so the brute-force protection did not work in production.
- **A failed save destroyed pending domain events.** Catching `ConcurrencyConflictException`,
  reloading and re-saving — the documented recovery for a lost update — published nothing at all.
- **A committed transaction could be reported as failed.** An exception from a domain-event consumer
  escaped the interceptor and surfaced as a commit failure, inviting the caller to retry a write
  that had already been applied. Events after the throwing consumer were also dropped.
- **Refresh-token rotation was not single-use.** The liveness test lived on the `SELECT`, the
  `UPDATE` was keyed on the primary key alone, and the row carried no concurrency token, so two
  simultaneous presentations of one token both succeeded. Revocation is now a single conditional
  `ExecuteUpdate`, and zero affected rows routes to the replay path.
- **A stored row could produce an aggregate violating its own invariants.** `TodoList.Rehydrate`
  appended items without checking the cap, title uniqueness, ownership or nullity.
- **Deleting an aggregate absent from the identity map lost its domain events**, because marking it
  removed was a no-op when it was not already tracked.
- A child-only change rewrote every column of the root row, including its creation stamps.
- `Result<T>.Success` accepted `null`, producing a success carrying `null` under a non-nullable
  declaration — served to a client as a 200 with an empty body.
- Loading a list produced a root × items × tags cartesian product; the read is now a split query.
- The timing-equalisation decoy hash used a different hasher from the one used to verify, so a
  configuration change could silently reopen the account-enumeration timing oracle.
- CI collected coverage over the architecture tests, which fails: NetArchTest resolves types with
  `Type.GetType(name, throwOnError: true)` and that does not work against a Coverlet-instrumented
  assembly. Coverage now excludes that project, and every test still runs exactly once.

### Changed

- **The template ships no migration; `bootstrap` creates each context's first one.** The
  hand-conditioned `InitialCreate` of the business and authentication contexts is gone. The new
  `migration-initial` task, which `bootstrap` runs once the local tools are restored, runs
  `dotnet ef migrations add InitialCreate` on every context whose `Migrations` folder holds no model
  snapshot, for the project's own model and database, and the team commits the result. It leaves a
  context that has one alone; `migration-add`, `database-update`, `migration-bundle` and `verify`
  refuse, naming bootstrap, until it has run.
- **The email is the account's only identifier.** `POST /auth/register` takes `email` and
  `password`; `GET /auth/me` no longer carries `userName`; the account mails greet without a name;
  a passkey's display name and an account provisioned through an external provider use the address.
  ASP.NET Identity's `UserName` column stays and is kept equal to the email, moved with it when an
  email change is confirmed, so no migration is involved. `IdentitySeed:AdminUserName` (and the
  compose file's `SEED_ADMIN_USERNAME`) is removed: the seeded administrator signs in with
  `IdentitySeed:AdminEmail`.
- **Every container image is pinned by digest, as `name:tag@sha256:…`.** The compose services, the
  Dockerfiles' bases and `# syntax=` frontend, the Testcontainers fixtures and the images the Docker
  actions pull in the workflows (binfmt, the BuildKit daemon, the SBOM scanner, whose defaults
  floated). The Dockerfiles write their bases in `FROM` rather than through the
  `DOTNET_SDK_TAG`/`DOTNET_RUNTIME_TAG` arguments, which Dependabot could not update, and the
  runtime image drops its `org.opencontainers.image.base.name` label, which release provenance
  already records. The integration fixtures move from a floating `postgres:17-alpine` to the
  compose file's PostgreSQL 18.6 image. `ContainerImagePinningTests` and the workflow gate fail on
  an image without a digest, and on a fixture that runs anything the compose file does not.

- **The local object store is SeaweedFS, from its own project's image, and MinIO is gone.** MinIO
  stopped publishing images when it went source-only, and the `coollabsio/minio` rebuild the stack
  ran was a third party between the template and the source. `docker-compose.yml` now runs one
  `objectstore` service (`chrislusf/seaweedfs`, pinned by digest) that creates the bucket itself on
  start-up, so the `minio-bucket` one-shot service and the `9001` console are gone; the
  `Storage/` integration suite runs the same image through the core `Testcontainers` package, which
  replaces `Testcontainers.Minio`. Anonymous requests are still refused: the one S3 identity the
  service is created with is what turns them off. **Migration for a derived project:** rename
  `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` in `.env` to `STORAGE_ACCESS_KEY_ID` and
  `STORAGE_SECRET_ACCESS_KEY`, point `STORAGE_ENDPOINT` at `http://objectstore:9000`, and replace
  `minio minio-bucket` with `objectstore` in any `docker compose up` you script. Objects in the old
  `minio-data` volume are not migrated.

- **The background jobs' configuration speaks with one vocabulary, and `Worker` is gone from it.**
  The `MaintenanceWorker`, `ReminderWorker` and `FileWorker` sections become `Jobs:Maintenance`,
  `Jobs:Reminders` and `Jobs:Files`, and the deployment role `BackgroundJobs:Enabled` becomes
  `Jobs:Enabled` in the same `Jobs` section; the options classes follow (`JobsMaintenanceOptions`,
  `JobsRemindersOptions`, `JobsFilesOptions`, `JobsOptions`). The meters are renamed with them:
  `AppTemplate.Jobs.{Maintenance,Reminders,Files}` and the counters under `apptemplate.jobs.*`.
  Nothing is released, so no dashboard or deployment depends on the old names.
  **Migration for a derived project:** rename the keys in `appsettings.json`,
  `appsettings.Development.json`, `docker-compose.yml` and `.env`
  (`MaintenanceWorker__Interval` becomes `Jobs__Maintenance__Interval`, `BACKGROUND_JOBS_ENABLED`
  becomes `JOBS_ENABLED`, and so on for every key of the three sections).

- **`RateLimiting:AuthenticationPermitLimit` exists only with local accounts.** The authentication
  rate-limit policy is carried by the local-accounts controllers, so a project generated with
  `--auth external` or `--auth none` no longer receives the key, the validator rule, the policy
  registration or the appsettings row; its unit tests assert the global limit alone. A project with
  local accounts sees no change.

- **A public API (`--auth none`) refuses an `[Authorize]` action at start-up.** No authentication
  scheme exists in that variant, so the attribute previously surfaced as an unexplained 500 on
  every request. The host now walks the controller actions after mapping them and refuses to boot,
  naming the offenders; `[AllowAnonymous]` still overrides, and bringing a scheme back clears the
  refusal.

- **CI generates and verifies every supported option combination.** The template acceptance job is
  a six-leg matrix (the defaults, `--auth none` with its forced options, `--auth external` with `notes`,
  with `all` and with neither an example nor Files, and `--auth local` without Files, workflows or Sonar). Each leg checks that the
  template's name leaked nowhere, formats, builds, runs the Docker-free tests, compares the model of every
  context it generated and runs the generated project's own hygiene gates. A second job asserts that
  `--auth none` alone and with `--examples notes` are refused before any file is written.

- **Authentication speaks through nine intent-shaped ports instead of twenty.** `IAccountsService`,
  `IEmailTokensService`, `ISessionTokensService`, `ITwoFactorService`,
  `IAccountAdministrationService`, `IExternalIdentityVerifier`, `IExternalLoginsService`,
  `IAccountEmailFactory` and `ISecurityEventLog`, each with one adapter. Statuses a use case
  collapsed into one error are gone: email confirmation answers a `bool`, an unknown address on
  reset or email change is an invalid token, the four administrative statuses are one
  `AccountAdministrationOutcome`, and external verification returns the identity or `null`.
  `PortConventionTests` now caps a port at six operations instead of four. Routes, responses and
  error codes are unchanged.
- **The API composes local accounts behind one call, `AddAuthFeature()`.** It registers the use cases,
  the identity module, the refresh-token purge, the `auth-database` readiness check, the
  `Administrator` policy and the identity schema's development migration and seed, from
  `Features/Auth/Composition/`. The development bootstrap runs one step per module instead of naming
  the identity context. `DELETE /api/v1/maintenance/refresh-tokens/expired` moved to authentication's
  own controller with the same route, tag, policy and responses.
- **A reminder's recipient comes from a port Reminders owns, and authentication can be left out of
  the container.** `FireDueRemindersUseCase` resolves the owner's name and address through
  `IReminderRecipientDirectory` and hands them to `IReminderNotifier`; the host supplies
  `AccountReminderRecipientDirectory`, over the local accounts. The email module no longer references
  `AppTemplate.Application.Auth`, and a reminder whose owner has no account is cancelled instead of
  being marked fired without a mail. `ContainerCompositionTests.TheApiContainer_BuildsWithoutAuthentication`
  holds the result.
- **The `Identity` section is the only password policy.** The request validators no longer copy its
  floor of eight; they check presence and a 256-character ceiling, and the store judges the rest
  with the configured rules. A short password is refused on the same field with the same code, with
  the policy's own messages.
- **`AccountLockedOut` is decided by the login use case**, no longer by the account adapter. It is
  recorded on every sign-in that meets the lockout, as before; its documentation said otherwise.
- **The Files storage and scanning configuration moved under one `Files` section.** `Storage:*`
  is now `Files:Storage:S3:*` with the same keys, validated only when that provider is chosen;
  `ContentInspection` is now `Files:Scanning` with `Enabled`, `Host`, `Port` and
  `MaxScannableBytes` (`Enabled: true` requires `Host`). **Migration for a derived project:** rename
  the sections in configuration files and environment variables (`Storage__BucketName` becomes
  `Files__Storage__S3__BucketName`), and set `Files:Scanning:Enabled` where `ScannerHost` was set.
- **The Files content ports were reshaped.** `IFileContentStore` writes, reads, describes and
  deletes; signing moved to `IFileDirectTransfer`, which a store may not support;
  `IFileContentInspector` gave way to `IFileContentScanner`, with the head read and the type check
  now in the inspection use case; `IFileContentInventory.ListKeysAsync` resumes after a key
  (`startAfter`) instead of an opaque continuation token.

- **The API runs the periodic jobs; `AppTemplate.Worker` is gone.** The maintenance purges, the
  reminder pass and the three file passes start from `AppTemplate.Api` through one call,
  `AddBackgroundJobs()`, so a generated project starts with one application process and one image.
  `BackgroundJobs:Enabled` (default `true`) is the deployment role: `false` serves HTTP only and binds
  no job section, and with several instances it belongs on all but one. The `MaintenanceWorker`,
  `ReminderWorker` and `FileWorker` sections keep their names and keys, are validated when the jobs
  are enabled, and `docker-compose.yml` configures them on the `api` service. The use cases, their
  transactions and the reminder lease are untouched. A job's scope has no caller — `ICurrentUser`
  throws and the audit actor records nobody — while a request keeps its principal; the refresh-token
  purge registers itself from the authentication feature, so removing authentication removes it. The
  meters keep their `AppTemplate.Worker.*` names. **Migration for a derived project:** delete your
  worker's Compose service, Kubernetes manifests and image, move its `MaintenanceWorker`,
  `ReminderWorker` and `FileWorker` settings to the API's, and give the API the real `Jwt:Key` it
  already had — the placeholder key the worker used no longer has a process to live in.
- **Authentication owns its storage, and the module is `AppTemplate.Infrastructure.Auth`.**
  `AuthDbContext` maps the nine identity tables, the refresh-token grants and the data-protection
  key ring in the `identity` schema, with a migrations history of its own; `AppDbContext` keeps the
  business tables and the idempotency one and no longer derives from `IdentityDbContext`. One
  database and one connection string still — and therefore one connection pool, which is why
  `DatabaseOptions` moved to `AppTemplate.Infrastructure.Core` and both modules bind the same
  section into it. Adopting this in an existing project means four things: the namespace
  `AppTemplate.Infrastructure.Identity` is now `AppTemplate.Infrastructure.Auth` and
  `AddIdentityModule` is `AddAuthModule`; a module that owns a context takes
  `IContextUnitOfWork<TContext>` rather than the unnamed `IUnitOfWork`, which stays registered by
  the module owning the business context; both contexts must be migrated and health-checked; and the
  two initial migrations are regenerated rather than split, so a deployed database needs a real
  `DropTable`/`DropSchema` migration instead. The configuration sections keep their names —
  `Identity`, `IdentitySeed` and `IdentityTokens` name ASP.NET Identity's own concepts, and
  `IdentitySeed` is now bound by `AddAuthModule`.
- **Removing authentication is one coupling lighter.** The seeder belongs to the authentication
  module, so nothing in the business half is left needing what only that module supplies, and each
  half's initial migration records only its own tables. What is still to answer for is
  `IReminderNotifier`, whose only implementation resolves an authentication port to find the address
  a due reminder is rung at.
- **No infrastructure module references another.** Every module takes what it shares from the
  layer's SDK half, so the permission for a horizontal reference to the persistence module
  is withdrawn: the rule now reads "only the layer's Core horizontally", and the persistence module
  is a module like any other.

- **What a context saves through now lives in `AppTemplate.Infrastructure.Core`**, not in the
  persistence module: `EfUnitOfWork` — generic over `DbContext` rather than one named context — the
  three save interceptors, the aggregate-tracker base, and the system clock. A module registers its
  commit boundary with `AddContextUnitOfWork<TContext>()`, which is enough to commit, and attaches
  only the mechanisms its rows need where its context's options are built — `UseAggregateFlushing`,
  `UseAuditStamping` and `UseDomainEventDispatch`, in that order, after `AddSavingInterceptors()`
  has registered them. Adopting this in an existing project means two things: the
  namespaces `AppTemplate.Infrastructure.Persistence.Common.{Saving,Time}` are now
  `AppTemplate.Infrastructure.Core.Common.{Saving,Time}`, and a module that registered those types
  by name registers them by call instead — every adapter of an application port behind these calls
  is `internal`, which is what stops a second module from composing one by naming it.
  The idempotency store stays in the persistence module: it owns a table, and which context owns
  that table is not a question this move answers.

- **Tests run on Microsoft.Testing.Platform, and `dotnet test` takes different switches.**
  `global.json` names the runner, each test project builds as a console application hosting its own
  runner, and one invocation runs the whole solution in parallel — no per-project loop, and the
  architecture suite no longer needs a collector-free invocation of its own. The switches are not
  interchangeable with the VSTest ones a derived project may have copied into its own scripts:
  `--logger` and `--collect` are gone, replaced by `--report-xunit-trx` and `--coverage`, and
  `--settings` is accepted and then silently ignored, so coverage settings must arrive through
  `--coverage-settings`. `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` and
  `coverlet.collector` are gone with it; `xunit.v3` 4.0.0 requires the platform, and coverlet's own
  collector does not support it on .NET 10. Adopting this in an existing project means editing every
  place a test command is written down.
- **`coverage.runsettings` is a different file for a different engine.** Coverage is collected by the
  extension the platform ships, which takes .NET regular expressions where coverlet took globs, so
  every filter in that file is rewritten. It also counts sequence points rather than lines: the same
  code and the same tests measure 6,215 lines where coverlet measured 8,704, and score four points
  higher over them. Every figure in `coverage.minimum` before the `extension` rows is on the old
  scale and not comparable.
- **The coverage floor is 88, up from 85.** A floor is worth what its margin is, and re-basing the
  measurement left 85 protecting less than it used to. 88 is set against the lower of the two
  configurations measured, so a local Debug run cannot fail a gate that CI's Release run would pass.
- **The coverage floor is 90, up from 88**, re-measured with Docker present so both integration
  suites contribute: 94.87% in Debug and 96.09% in Release, over 3,328 tests. Same rule as the
  move before it — the margin is what is held constant and the number follows, so 88 against a
  higher measurement was the same figure protecting less. Still set against the Debug row, which
  is the lower of the two. Two of the fifteen assemblies sit under the floor and the total is what
  the gate enforces; `coverage.minimum` names them.
- **`Directory.Build.targets` is new, and carries one property.** `<OutputType>Exe</OutputType>` for
  every project with `IsTestProject`. It cannot live in `Directory.Build.props`, which is imported
  before a project's own body, where `IsTestProject` is still empty.
- **A mail's subject is no longer configured.** `EmailConfirmation:Subject`, `PasswordReset:Subject`
  and `EmailChange:Subject` are gone; the subject is the template's `<title>`. They were a second
  statement of the same fact and had already drifted from it — the registration mail was being
  delivered with an English subject over a French body.
- **The README's error-code table now covers all 49 codes**, in three tables. It documented 14, while
  telling clients to branch on `code`; `auth.forbidden`, which every refused administration call
  carries, was not among them.
- **The README described two of the four file states.** `status` reads `pending`, `deposited`,
  `available` or `quarantined`, and `confirm` answers `deposited` — the value a client is most likely
  to read, and the one the document did not mention.
- **`request.malformed` is not what a wrong-typed query value returns.** The README and two request
  records promised one vocabulary for a shape error and another for a broken rule; the response
  factory has stamped `request.validationFailed` on both since it was introduced, deliberately and
  under test. The documentation now says what the code does.
- **`GET …/items/{id}/reminders` answers `200` with an empty list for an item that is not there**,
  which is now written down as the deliberate exception it is: a reminder outlives the item it is
  about, and this is the only route that can show a cancelled one.
- **`NoResponse_CarriesALinkHeader` watched for the two spellings this repository never uses.** It
  now also watches `Headers.Link`, which is the house idiom — every response header here is written
  through the strongly-typed property — and each needle set has a test proving it detects what it
  names and leaves ordinary code alone.
- **`coverage.minimum` and `coverage.runsettings` pointed at `tasks.ps1`**, deleted when the tooling
  moved to C#, and told the reader to run it.
- **`CONTRIBUTING.md` said `compose-up` starts PostgreSQL and mailpit.** It starts the whole stack.

- **Every infrastructure module now has the same shape: `Common/<Responsibility>/` plus
  `Features/<Feature>/<Responsibility>/`.** `AppTemplate.Infrastructure.Auth` had forty files in
  ten subject folders at its root and `AppTemplate.Infrastructure.Storage` nine in three, with
  neither a `Common/` nor a `Features/`, while the other three modules had both. A folder under
  `Features/` is the plural of the nature word its files carry — a `…Service` is in `Services/` and
  `Services/` holds nothing else, as a `…Repository` has always been in `Repositories/` — so a type
  name tells you its folder and a folder tells you what is in it. Uniformity beats local logic here
  on purpose: several folders hold a single file, and reading one infrastructure module now teaches
  you how to read the next. No type was renamed and no port was touched; the two modules' public
  surface is the same set of classes at new namespaces. It also puts `BucketBudget` and
  `ScannerBudget` side by side in `Common/Budgets/` — they say the same thing about two
  dependencies the outbound HTTP policy cannot reach, and each already cited the other — and moves
  `IdentityTokenOptions` to `Common/Options/`, since it sets one lifespan for every provider
  `AddDefaultTokenProviders` registers rather than belonging to any one subject.
  **Migration for a derived project:** every namespace under
  `AppTemplate.Infrastructure.{Identity,Storage}` changed, so a `using` of one will not compile.
  Fifty-one files moved and fifty-five referenced them. There is no configuration key, database
  column or wire format involved — it is a rename, and the compiler names every site.
  `LayoutConventionTests` now also fails the build for an infrastructure module absent from its two
  vocabulary dictionaries, which is what let these two drift: the previous guard could only catch a
  project listed *without* the folder it named, never one on disk that nobody listed.
- **`AppTemplate.Worker` no longer receives the API's `Jwt:Key`.** It still needs the `Jwt` section
  — it composes the identity module, and `JwtOptionsValidator` runs at startup — but it signs and
  verifies nothing, so `docker-compose.yml` and `deploy/kubernetes/configmap-worker.yaml` now give
  it a fixed placeholder and only the API's Deployment references the real key. `Jwt:Issuer` and
  `Jwt:Audience` stay identical across both hosts.
  **Migration for a derived project:** drop the `Jwt__Key` `secretKeyRef` from your worker
  Deployment and set `Jwt__Key` to any non-blank value of at least 32 bytes in its ConfigMap. No
  configuration *key* changed, so nothing fails if you do not — you simply keep handing a signing
  key to a process that never uses it.
- **The seven migrations that accumulated while the schema was still moving are recomposed into
  exactly two: `InitialCreate` (`identity` and `platform` — the schema every derived project keeps)
  and `AddExampleFeatures` (`todo` and `reminders`, and nothing else).** Net effect only: every table,
  column and index that existed before still exists after, in the same shape. This is what makes
  `docs/REMOVING-THE-EXAMPLE-FEATURES.md`'s migration step a `rm` of one file pair instead of a
  generated `DropTable` migration — a project that deletes the example features before ever running a
  migration against a real database never has their schema to begin with.
- **One closed folder vocabulary per layer, and one public type per file**. Each
  use case owns a folder holding its command, interface, implementation and validator; each port
  owns a folder holding its interface and the messages it exchanges. Architecture tests read the
  source tree and refuse a folder outside the vocabulary, an empty folder, a file declaring two
  public types, and a use-case folder that does not name what is inside it.
- **A repository contract lives in the Domain; every other port lives in Application**. The domain's `Stores/` folder is now `Repositories/`, after the contract it has
  always held; `Store` is kept for a technical contract with no aggregate behind it.

- **BREAKING: the project prefix is `AppTemplate`, not `CA`.** Every project, namespace, `.sln`/
  `.csproj`/`.http` file name, `InternalsVisibleTo`, and `.editorconfig` path-scoped section is
  renamed — CA.Domain → `AppTemplate.Domain`, CA.Api → `AppTemplate.Api`, CA.sln →
  `AppTemplate.sln`, and so on for every module and test project. The rename exists because
  `dotnet new`'s `sourceName` replaces its token by literal substring match: a `sourceName` of `CA`
  would have rewritten the analyzer rule IDs in `.editorconfig` (`CA1000`, `CA1707`, …) into
  garbage in every generated project. `AppTemplate` cannot collide with a `CAxxxx` id now or in the
  future. **Anyone who forked this repository before this change must merge or reapply that rename
  by hand** — there is no automated migration path, because there is no way to distinguish "this
  repository's own `CA` token" from an adopter's unrelated code that happens to contain `CA` once
  the fork has diverged.
- Compose's project name and the API image tag are now `app-template`/`app-template-api` (was
  `ca-template`/`ca-api`); `POSTGRES_DB`/`POSTGRES_USER` are now the generic `appdb`/`appuser` (was
  `ca_template`/`ca_app`) — generic on purpose, so a generated project's database credentials do not
  encode its name.
- `Features/<F>/DomainEvents/` is now `Features/<F>/Consumers/` — the folder holds consumers, while
  the Domain's `Events/` holds the event types.
- Feature-level error catalogues moved to `Features/<F>/Errors/`.
- **`IAuthService` is gone.** The six-method façade port is replaced by five narrow capability ports
  (`IUserAccounts`, `IEmailConfirmationTokens`, `IAccessTokenIssuer`, `IRefreshTokenGrants`,
  `IConfirmationEmailComposer`), and the sequencing moved from the Identity adapter into the Auth use
  cases — which is what finally gives `IEmailSender` a consumer in the layer that declares it. The
  refusal reasons are now explicit enums, so collapsing them to one client-facing error is a decision
  a test can watch rather than something invisible inside `SignInManager`.
- Auth request records now live in the file of the use case that accepts them, responses in
  `Features/Auth/Dtos/`, one validator per file — the shape TodoLists already had. `Contracts/`
  is gone.
- The Identity module's folders are `Bearer/`, `Notifications/`, `Options/`, `Templates/`, `Tokens/`
  and `Users/`; `Authentication/` is gone, so composing a mail is no longer filed under
  authentication.
- The two OpenAPI endpoints are mapped `.AllowAnonymous()`. They previously answered **401** in
  Development, because the default-deny fallback policy caught them — the API-reference page has
  never been reachable.
- `ITodoListRepository.GetAsync` no longer defaults its `CancellationToken`, making a dropped token
  visible at the call site.
- `DomainException` is `sealed` and always carries a message.
- Refresh-token primary keys are UUIDv7, matching the domain and giving index locality.

### Removed

- **The reminder claim: `Reminder.TryClaim`, `ReleaseClaim`, `ClaimedAt` and the stale-claim timeout.**
  The claim was taken in memory and settled — released on a failure, kept on success — before any
  commit, so it never reached the store while a reminder was still pending: the state the timeout
  exists to recover from could not occur, and a second replica never saw it. The leader lease is the
  exclusivity, and delivery is at-least-once with or without it. **The `claimedAt` field is gone from
  the reminder response** — it was null on every pending reminder and, on a fired one, only restated
  when it fired; read `notifiedAt`. The initial migration no longer has a `ClaimedAt` column on
  `Reminders`.
- The `ValueObject` base class and its tests. Nothing derived from it: value objects in this
  template are `record`s, which is the idiom it now demonstrates without a second one alongside.
- The `Required by EF Core` parameterless constructors on the domain entities. EF maps persistence
  models, never the domain types, so they were dead code and a persistence concern in the Domain.
- Two unit tests asserting guarantees they could not verify — each configured a substitute and then
  asserted what it had just configured. The real guarantees are pinned where the behaviour lives.

### Deliberately not added

Each of these was investigated and refused, and the record says why so the next person does not
repeat the investigation. A template's value is as much in what it refuses as in what it ships.

| Capability | In one line |
|---|---|
| A filter expression language (OData `$filter`, RSQL) | Makes query cost unbounded and the whitelist unprovable; the typed surface can be read off a type. |
| RFC 8288 `Link` headers for paging | A second statement of next-page that can disagree with the envelope. |
| An outbox for domain events | At-least-once is a contract on every consumer; the effect re-derives its own precondition instead, and the divergence is counted. |
| A security-stamp cache | Invalidating at the rotation points does not propagate between instances, so the observable promise stays "within at most the TTL" and the invalidation buys nothing. |
| Rate limiting partitioned by identity | The limiter runs before authentication, so the principal is always anonymous where the partition key is computed. Moving authentication earlier would make every request the limiter is about to reject pay for a bearer validation first. |
| A paginated user search | Every other endpoint in the authentication vertical spends its effort not revealing whether an address exists; listing them all in one call would contradict that effort. |
| A caller id on `ICurrentUser` | Nothing would populate it until machine-to-machine authentication exists, and a member that is never set makes every consuming use case imply a capability the template does not have. |
| Listing and revoking active sessions | Rotation inserts a new row per refresh, so the id a client read would be dead within the access token's lifetime and `DELETE` would fail silently against a live session. It needs a session id stable across rotation first — a column, not an endpoint. |
| `PATCH` (JSON Patch or Merge Patch) | Patching a representation lets a caller assemble a state change no aggregate operation authorises. |
| Output caching | Every response is per-user, so it either serves the wrong data or has no hit rate. |
| `Deprecation`/`Sunset` headers | One version ships, so any date emitted would be invented. |
| A queryable audit trail | A table the app can `UPDATE` through its own connection has the appearance of an audit trail without the property. |
| Feature flags through OpenFeature | Every loop already has an `Enabled` key an operator can flip without a deploy, and `IOptionsMonitor` reloads them — so the kill switch exists and a vendor-neutral abstraction over one in-process provider would be an abstraction with one implementation, which this repository calls a guessed abstraction. Add it when a second provider is real. |
| A CAPTCHA on registration | It is a real anti-abuse tool, but it puts a vendor on the critical path of authentication for a template that already has per-address rate limiting and account lockout. Named as an extension point rather than shipped. |
| SMS or push for `IReminderNotifier` | The port already has two adapters; a third channel proves nothing the second did not, and adds a vendor. |
| Redis, for the rate limiter or a cache | The seam exists so an adapter can be written; the dependency does not, because this template refused output caching and the security-stamp cache and so has no other use for one. |
| Soft delete / restore | An invisible predicate on every read of every feature, where one omission leaks deleted rows silently. |

### Known gaps

Stated rather than left to be discovered:

- **Domain-event delivery is best-effort.** Consumers of one event are now isolated from each other,
  so one throwing no longer cancels its siblings — but the throwing consumer's own side effect is
  still lost, and a process that dies between the commit and the dispatch loop loses every consumer
  for that save. Closing this needs an outbox, which is refused for the reasons in
  no outbox; add one before relying on a consumer whose absence a user would notice.
- **Idempotency retention is enforced by a purge you must schedule.** `Idempotency:Retention` only
  stamps each row's `ExpiresAt`. Until `DELETE /api/v1/maintenance/idempotency-keys/expired` runs,
  a completed key stays replayable past its retention and the table grows.
- `ConfigureJwtBearerOptions` builds `ProblemDetails` and owns the `auth.required` /
  `auth.forbidden` codes, so the wire format for auth failures has an owner in the infrastructure
  layer as well as in the API.
- `UserManager` and `SignInManager` accept no `CancellationToken`, so cancellation in
  `IUserAccounts` and `IEmailConfirmationTokens` is observed on entry only and cannot be
  propagated to the I/O.

[Unreleased]: https://github.com/OWNER/REPO/compare/main...HEAD
