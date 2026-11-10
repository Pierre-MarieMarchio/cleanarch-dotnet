---
type: guide
title: Moving into an existing repository
description: How to make this project a repository of its own, or move it into one that already has a history.
---

# Moving into an existing repository

This project can become a repository of its own, or move into one that already has a history. Do this
once, right after generation and bootstrap.

## At the root or in a subdirectory

| Shape | What you do | What it costs |
|---|---|---|
| At the root | the tree becomes the repository; merge by hand what both already have | nothing in the tooling: every command and gate assumes the solution sits at the repository root |
| <!--#if (githubHost)--> |
| In a subdirectory, such as `services/api/` | copy the tree there; the host keeps its own root files | the host's `global.json` learns the test runner; the workflows move to the git root |
| <!--#endif--> |
| <!--#if (gitlabCi)--> |
| In a subdirectory, such as `services/api/` | copy the tree there; the host keeps its own root files | the host's `global.json` learns the test runner; the pipeline and the `.gitlab` folder move to the git root |
| <!--#endif--> |
| <!--#if (gitlabHost && !ci)--> |
| In a subdirectory, such as `services/api/` | copy the tree there; the host keeps its own root files | the host's `global.json` learns the test runner; the `.gitlab` folder moves to the git root |
| <!--#endif--> |
| <!--#if (!anyCodeHost)--> |
| In a subdirectory, such as `services/api/` | copy the tree there; the host keeps its own root files | the host's `global.json` learns the test runner |
| <!--#endif--> |

Take the root unless the repository must hold more than this service. At the root, the files to merge
are the ones a host is likely to have already: `README.md`, `.gitignore`, `global.json`,
`Directory.Build.props`, `Directory.Packages.props` and `.editorconfig`.

<!--#if (githubCi || gitlabCi)-->
Three facts decide the subdirectory shape:
<!--#endif-->
<!--#if (!githubCi && !gitlabCi)-->
Two facts decide the subdirectory shape:
<!--#endif-->

- `dotnet cleanarch` finds the repository root by walking up to `cleanarch.json`, so every command
  works from the subdirectory unchanged.
- MSBuild stops at the nearest `Directory.Build.props` and `Directory.Packages.props`, and
  `.editorconfig` declares `root = true`: the host's root files do not reach these projects, and these
  do not reach the host's.
<!--#if (githubCi)-->
- GitHub reads workflows from `.github/workflows` at the git root only.
<!--#endif-->
<!--#if (gitlabCi)-->
- GitLab reads the pipeline from `.gitlab-ci.yml` at the git root only, and a job runs there.
<!--#endif-->

## In a subdirectory, step by step

1. Copy the tree, bootstrapped and committed, into the subdirectory:

   ```bash
   mkdir -p <host-repo>/services/api
   cp -a <this-project>/. <host-repo>/services/api/
   ```

2. Add the test runner to the host's root `global.json`. `dotnet` resolves `global.json` upward from
   where it runs, so a test run started at the host's root otherwise fails on every test project with
   "Testing with VSTest target is no longer supported":

   ```json
   {
     "test": { "runner": "Microsoft.Testing.Platform" }
   }
   ```

<!--#if (githubCi)-->
3. Move the workflows to the git root, renaming them if the host has its own:

   ```bash
   mv services/api/.github/workflows/ci.yml      .github/workflows/api-ci.yml
   mv services/api/.github/workflows/codeql.yml  .github/workflows/api-codeql.yml
   <!--#if (serverDatabase)-->
   mv services/api/.github/workflows/release.yml .github/workflows/api-release.yml
   <!--#endif-->
   ```

<!--#endif-->
<!--#if (githubCi && anyWeb)-->
   Move `web.yml` and `web-node-version.yml` the same way.

<!--#endif-->
<!--#if (githubCi)-->
   A workflow runs from the git root, so prefix every path its steps name (the solution,
   `.config/dotnet-tools.json`, `coverage.minimum`, `global.json`) with the subdirectory.

<!--#endif-->
<!--#if (githubCi)-->
4. Merge `.github/dependabot.yml` into the host's, at the git root, each `directory` prefixed with
   the subdirectory. Dependabot reads that one file only.

5. Keep Compose in the subdirectory. Its build contexts are relative to the compose file, so nothing
   needs re-pointing; copy `.env.example` to `.env` beside it.
<!--#endif-->
<!--#if (githubHost && !ci)-->
3. Merge `.github/dependabot.yml` into the host's, at the git root, each `directory` prefixed with
   the subdirectory. Dependabot reads that one file only.

4. Keep Compose in the subdirectory. Its build contexts are relative to the compose file, so nothing
   needs re-pointing; copy `.env.example` to `.env` beside it.
<!--#endif-->
<!--#if (gitlabCi)-->
3. Move the `.gitlab` folder to the git root, every path under it kept, renaming a template whose
   name the host's already uses, then the pipeline:

   ```bash
   mkdir -p .gitlab
   cp -a services/api/.gitlab/. .gitlab/
   rm -r services/api/.gitlab
   mv services/api/.gitlab-ci.yml .gitlab-ci.yml
   ```

   If the host has a `.gitlab-ci.yml` of its own, the last line moves this one to `.gitlab/ci/api.yml`
   instead, and the host's includes it; its `local:` includes keep their paths:

   ```yaml
   include:
     - local: .gitlab/ci/api.yml
   ```

   A project runs one pipeline, so the host's then holds `stages:`, `workflow:` and GitLab's SAST and
   Secret Detection templates once, merged from both. A job runs from the git root, so prefix with the
   subdirectory every other path a job names (the solution, the cache keys' files,
   `.config/dotnet-tools.json`, `coverage.minimum`, `global.json`).

<!--#endif-->
<!--#if (gitlabHost && !ci)-->
3. Move the `.gitlab` folder to the git root, every path under it kept, renaming a template whose
   name the host's already uses:

   ```bash
   mkdir -p .gitlab
   cp -a services/api/.gitlab/. .gitlab/
   rm -r services/api/.gitlab
   ```

<!--#endif-->
<!--#if (gitlabHost)-->
   GitLab reads the merge request and issue templates from there only, beside the host's own.
   Renovate reads one configuration file: if the host has one, merge `.gitlab/renovate.jsonc` into
   it; either way, prefix each `matchFileNames` entry with the subdirectory.

4. Keep Compose in the subdirectory. Its build contexts are relative to the compose file, so nothing
   needs re-pointing; copy `.env.example` to `.env` beside it.
<!--#endif-->
<!--#if (!anyCodeHost)-->
3. Keep Compose in the subdirectory. Its build contexts are relative to the compose file, so nothing
   needs re-pointing; copy `.env.example` to `.env` beside it.
<!--#endif-->

## Verify

From the subdirectory:

```bash
dotnet cleanarch verify
docker compose config -q
```

Then, from the host's root, `dotnet test --solution services/api/AppTemplate.slnx` proves the runner reached
the host's `global.json`.

`coverage.minimum` holds the floor this project's own suite was measured against. It stays right on
the first day; re-measure it once your own code lands, as [testing](../guides/testing.md#coverage)
explains.
