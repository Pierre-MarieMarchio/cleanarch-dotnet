---
type: decision
title: 0005. A project carries one code host's files
description: Why a generated project holds the files of one code host, chosen at generation, or none, and no other host's.
status: Accepted
---

# 0005. A project carries one code host's files

Status: Accepted

## Context

Every project was generated for GitHub alone: its `.github` folder holds the workflows, Dependabot,
CodeQL, the issue forms and the pull request template. A team on GitLab rewrote the pipeline by hand
and got no merge request or issue template; a team merging the project into a repository that
already has its own, or running its CI elsewhere, such as Jenkins or TeamCity, deleted the folder
and every page that named it. Either way a generated project read as written for a host it did not
live on, and the gate the template proves was no longer the one the project ran.

## Decision

- **One code host per project, chosen at generation.** `--code-host` names it; a project holds that
  host's files and no other's. It is a generation choice, not a setting: an option removes what it
  leaves out, it never disables it
  ([adding an option](../maintaining-the-template.md#adding-an-option)).
- **GitHub is in-tree.** Its files are in the checkout: the template's own CI runs them, and
  `WorkflowPinningTests`, `IssueFormTests` and `PullRequestTemplateTests` hold them, as PostgreSQL is
  in-tree in [decision 0001](0001-a-project-carries-one-database-provider.md).
- **Every other host is a whole-file overlay** under `.template.config/variants/code-host-<value>/`,
  copied under that value's symbol. A leg's options and a template gate prove it, without a leg of
  its own wherever an existing leg fits its shape
  ([adding a leg](../maintaining-the-template.md#adding-a-leg)). The contract is in
  [code hosts](../maintaining-the-template.md#code-hosts).
- **The values.** `github`, the default, whose output is unchanged. `none`, which writes no host file
  at all: no workflow, no Dependabot, no issue form, no pull request template. `gitlab`: a pipeline
  holding the same gate, merge request and issue templates, Renovate in place of Dependabot, and
  GitLab's SAST in place of CodeQL. A host whose overlay is unfinished stays out of the choices, so
  no project is generated without it.
- **`--ci` keeps its meaning for the chosen host**: that host's pipelines. Under `none` there is
  none to write, so the launcher does not ask it and `cleanarch.json` records `"ci": false`.
- **The only unpinned includes a GitLab pipeline may carry are GitLab's own SAST and Secret
  Detection templates**, which follow the instance's version: a pinned component is not available on
  every self-managed instance, and a pipeline that names one fails there before it runs.

`TemplatePackagingTests` holds the decision:
`EveryCodeHost_IsGitHubsTree_ANoneThatWritesNothing_OrAVariantOfItsOwn` fails on a choice of
`codeHost` that is neither in-tree nor an overlay of its own, and
`EverySourceThatWritesTheGitHubFolder_WritesNothingThereWithoutGitHub` on a source that writes a
`.github` path for a project another host, or none, holds.

## Alternatives considered

- **Azure DevOps.** The strongest pull among .NET teams, about one in seven in the surveys, but a
  second YAML dialect to keep in step; its tasks are pinned by major version only, so no commit SHA
  pins them as `WorkflowPinningTests` requires on GitHub; it reads no issue template from a file; and
  CodeQL there comes only with the paid Advanced Security. Deferred, not refused.
- **Gitea and Forgejo.** Their Actions read `.github/workflows` as a fallback, and Gitea reads
  `.github/ISSUE_TEMPLATE`, so a value of their own would add little to `github`. Nothing here can
  prove a project runs there, so no page claims it does.
- **Bitbucket.** Its pipelines run no privileged container and give a Docker service 1 GB by
  default, and its free plan 50 build minutes a month: Testcontainers and the Compose stack the gate
  starts fit badly.
- **A setting or a run-time switch over every host's files.** It would ship files a project never
  runs, and contradicts "an option removes, it never disables".
- **One Renovate configuration for every host.** Dependabot is native on GitHub and needs no app to
  install, and the default project must not change.

## Consequences

- A generated project reads as written for its host, and the pages condition on it; under `none`
  they say that CI and dependency updates are the team's to add.
- Each host's files evolve together: a step added to one host's gate is added to every other's, and
  `TemplatePackagingTests.AGitLabProject_RunsEveryCommandOfTheGitHubGate` pairs the gate jobs of
  `ci.yml` with GitLab's.
- A GitLab pipeline is never run by the template's own CI, which runs on GitHub. GitLab's schema,
  the template gate and `gitlab-ci-local` prove it on every pull request that changes it; a GitLab CE
  instance with a runner, started by hand, proves its rules, templates and release jobs. What only
  GitLab.com can prove, the push to its registry and the keyless signature its OIDC issuer allows, is
  proved by a project's first tag there.
- A host's free tier sets limits the project inherits: GitLab's SAST runs on every tier, but its
  merge request widget is Ultimate's.
- A project cannot move host by editing a setting: it moves by generating again with the other value
  and carrying the host's files across.

## Revisit when

Teams ask for Azure DevOps in numbers, GitLab makes its SAST pinnable on every instance, or a host's
pipeline needs more than an overlay of whole files.
