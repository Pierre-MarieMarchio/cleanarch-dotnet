using System.Text.Json.Nodes;
using static CleanArch.Cli.Generators.Shared.CSharpNames;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.Shared;

internal static class FeatureCode
{
    internal const string Generator = "dotnet cleanarch add feature";

    internal const string ScriptGenerator = "Tools/NewFeature.cs";

    internal const string TemplateFolder = "add-feature";

    internal static string TestDatabasePath(FeatureSpec spec) => $"{IntegrationTests(spec)}/Infrastructure/TestDatabase.cs";

    internal static bool WritesTheFieldTests(FeatureSpec spec, List<Emission> plan) =>
        plan.Any(emission => emission.Path.StartsWith(IntegrationTests(spec) + "/", StringComparison.Ordinal));

    internal static string IntegrationTests(FeatureSpec spec) => ProjectOf(spec, "tests/Integration", "Api.IntegrationTests");

    internal static string PersistenceProject(FeatureSpec spec) => ProjectOf(spec, "src/Infrastructure", "Infrastructure.Persistence");

    internal static string? Preconditions(string root, FeatureSpec spec, TextWriter output, IExecutor executor)
    {
        string auth = ProjectOf(spec, "src/Infrastructure", "Infrastructure.Auth");
        string[] contexts =
        [
            PersistenceProject(spec),
            .. Directory.Exists(System.IO.Path.Combine(root, auth)) ? [auth] : Array.Empty<string>(),
        ];

        string[] missing = [.. contexts.Where(context => !HasInitialMigration(root, context))];

        if (missing.Length > 0)
        {
            return $"{string.Join(" and ", missing)} has no initial migration, so the migration of {spec.Features} would have "
                + $"none to follow. Run `{Tool.Name} bootstrap`, which creates it, "
                + "and commit the migrations first (docs/guides/migrations.md).";
        }

        return MigrationCheck.Refusal(root, output, executor, $"The migration of {spec.Features}");
    }

    internal static bool HasInitialMigration(string root, string context)
    {
        string folder = System.IO.Path.Combine(root, context, "Migrations");

        return Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*ModelSnapshot.cs").Any();
    }

    internal static bool? TablePrefixesOf(LocatedProject project, List<string> problems)
    {
        try
        {
            return TablePrefixes(project);
        }
        catch (InvalidOperationException exception)
        {
            problems.Add(exception.Message);

            return null;
        }
    }

    internal static bool TablePrefixes(LocatedProject project) =>
        project.File.Options.Database switch
        {
            "postgresql" or "sqlserver" => false,
            "sqlite" => true,
            _ => throw new InvalidOperationException(
                $"'{Path.Combine(project.Root, ProjectLocator.FileName)}' records no options.database the generator knows (postgresql, sqlserver or sqlite), "
                + "and the feature's table test depends on it: the provider decides whether a schema holds the table or prefixes its name."),
        };

    internal static IEnumerable<string> PresentationHosts(string root, string prefix)
    {
        string presentation = Full(root, Presentation);

        return Directory.Exists(presentation)
            ? Directory.GetDirectories(presentation)
                .Where(folder => File.Exists(System.IO.Path.Combine(folder, "Program.cs")))
                .Where(folder => !IsClientPresentation(folder))
                .Select(System.IO.Path.GetFileName)
                .OfType<string>()
                .Where(host => host != FolderOf(prefix, "Api"))
                .Order(StringComparer.Ordinal)
            : [];
    }

    internal static string[] ComposingHosts(string root, FeatureSpec spec) =>
        spec.HasApi
            ? [.. PresentationHosts(root, spec.Prefix).Prepend(FolderOf(spec.Prefix, "Api"))]
            : [.. PresentationHosts(root, spec.Prefix)];

    internal static string JobsHost(string root, string prefix)
    {
        string presentation = Full(root, Presentation);

        return (Directory.Exists(presentation)
            ? Directory.GetDirectories(presentation)
                .Where(folder => File.Exists(System.IO.Path.Combine(folder, "Features", "BackgroundJobs", "Jobs", "BackgroundJobsModule.cs")))
                .Select(System.IO.Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal)
                .FirstOrDefault()
            : null) ?? FolderOf(prefix, "Api");
    }

    internal static bool IsClientPresentation(string folder) =>
        Directory.EnumerateFiles(folder, "*.csproj")
            .Any(project => File.ReadAllText(project).Contains(".Presentation.Client.csproj", StringComparison.Ordinal));

    internal static string HostComposition(string root, string host, string adapter)
    {
        string folder = Full(root, $"{Presentation}/{host}");
        string module = adapter == "Auth" ? "AddAuthModule(" : "AddExternalIdentityPersistence(";

        string? found = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault(file => File.ReadAllText(file).Contains(module, StringComparison.Ordinal));

        return System.IO.Path.GetRelativePath(root, found ?? System.IO.Path.Combine(folder, "Program.cs")).Replace('\\', '/');
    }

    internal static string ProjectOf(FeatureSpec spec, string layer, string project) =>
        $"{layer}/{FolderOf(spec.Prefix, project)}";

    internal static string ProjectInFolder(string prefix, string folder) => prefix + folder[Initials(prefix).Length..];

    internal static string PathOf(FeatureSpec spec, string reference, Dictionary<string, string> tokens, string? host = null)
    {
        string[] segments = reference.Split('/', 4);
        string referenceFolder = FolderOf(ReferenceProduct, string.Empty);

        if (segments.Length < 4 || !segments[2].StartsWith(referenceFolder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{reference} names no file of a {referenceFolder}<Part> project, so add feature cannot tell where it goes.");
        }

        string folder = host ?? FolderOf(spec.Prefix, segments[2][referenceFolder.Length..]);

        return $"{segments[0]}/{segments[1]}/{folder}/{Template.ReplaceReferenceNames(segments[3], tokens)}";
    }

    internal static TemplateOverrides Overrides(string root) => TemplateOverrides.Read(root, TemplateFolder, FeatureFlags.Vocabulary);

    internal static List<Emission> Plan(FeatureSpec spec, string root, TemplateOverrides templates, out List<string> skippedTestProjects)
    {
        var plan = new PlanWriter(spec, root, templates);

        if (spec.Reaction is { } adapter)
        {
            plan.Reaction(adapter);
        }

        plan.Domain();
        plan.Application();
        plan.UseCases();
        plan.Persistence();

        if (spec.HasApi)
        {
            plan.Api();
        }

        plan.UnitTests();
        plan.FieldTests();
        skippedTestProjects = plan.Skipped;

        return plan.Emissions;
    }

    internal static HashSet<string> Flags(FeatureSpec spec)
    {
        var flags = new HashSet<string>(spec.Operations, StringComparer.Ordinal)
        {
            spec.Access,
            IsRecord(spec) ? "record" : "aggregate",
        };

        foreach (var (flag, _) in OptionFlags(spec).Concat(OperationFlags(spec)).Concat(FieldFlags(spec)).Where(candidate => candidate.Holds))
        {
            flags.Add(flag);
        }

        return flags;
    }

    internal static Dictionary<string, string> Tokens(FeatureSpec spec)
    {
        var flags = Flags(spec);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);

        NameTokens(tokens, spec);
        OwnershipTokens(tokens, spec);
        ConcurrencyTokens(tokens, spec);
        ModelTokens(tokens, spec);
        tokens["ServiceArgs"] = ServiceArguments(spec, flags);
        CallTokens(tokens, spec);
        DocumentationTokens(tokens, spec, flags);
        CodeTokens(tokens, spec, spec.Fields, flags);

        return tokens;
    }

    private static void CodeTokens(Dictionary<string, string> tokens, FeatureSpec spec, IReadOnlyList<Field> fields, HashSet<string> flags)
    {
        var writer = new FieldTokenWriter(tokens, spec, fields, flags);

        writer.Contracts();
        writer.Tests();
    }

    private static bool IsRecord(FeatureSpec spec) => spec.Model == "record";

    private static bool IsOwner(FeatureSpec spec) => spec.Access == "owner";

    private static bool HasEtag(FeatureSpec spec) => spec.Concurrency == "etag";

    private static bool Anonymises(FeatureSpec spec) => spec.Deletion == "anonymise";

    private static bool HasAny(FeatureSpec spec, params string[] operations) => operations.Any(spec.Has);

    private static (string Flag, bool Holds)[] OptionFlags(FeatureSpec spec) =>
    [
        ("etag", HasEtag(spec)),
        ("erase", spec.Deletion == "delete"),
        ("anonymise", Anonymises(spec)),
        ("prefixedTables", spec.TablePrefixes),
    ];

    private static (string Flag, bool Holds)[] OperationFlags(FeatureSpec spec)
    {
        bool record = IsRecord(spec);
        bool readsOne = spec.Has("get") || (record && HasAny(spec, "create", "update"));

        return
        [
            ("writes", HasAny(spec, "create", "update", "delete")),
            ("changesOne", HasAny(spec, "update", "delete")),
            ("takesContent", HasAny(spec, "create", "update")),
            ("returnsOne", HasAny(spec, "get", "create", "update")),
            ("readsOne", readsOne),
            ("reads", readsOne || spec.Has("list") || IsOwner(spec)),
            ("store", record && (HasAny(spec, "create", "update", "delete") || IsOwner(spec))),
            ("notFound", HasAny(spec, "get", "update", "delete") || (record && spec.Has("create"))),
            ("stampsTime", record && HasAny(spec, "create", "update")),
        ];
    }

    private static (string Flag, bool Holds)[] FieldFlags(FeatureSpec spec)
    {
        var fields = spec.Fields;

        return
        [
            ("anonymisedText", Anonymises(spec) && fields.Any(field => field is { Kind: FieldKind.String, Nullable: false })),
            ("trimsText", HasAny(spec, "create", "update") && fields.Any(field => field.Kind == FieldKind.String)),
            ("constants", fields.Any(FieldCode.HasConstants)),
        ];
    }

    private static void NameTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        string lower = CSharpNames.Lower(spec.Type);

        tokens["P"] = spec.Prefix;
        tokens["LowerP"] = spec.Prefix.ToLowerInvariant();
        tokens["F"] = spec.Features;
        tokens["A"] = spec.Type;
        tokens["L"] = lower;
        tokens["R"] = Kebab(spec.Features);
        tokens["S"] = Snake(spec.Features);
        tokens["a"] = IsVowel(lower[0]) ? "an" : "a";
        tokens["An"] = IsVowel(lower[0]) ? "An" : "A";
    }

    private static void OwnershipTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        bool owner = IsOwner(spec);

        tokens["OwnerClause"] = owner ? " && record.OwnerId == ownerId.Value" : string.Empty;
        tokens["ListQuery"] = owner ? "GetForOwnerAsync" : "GetPageAsync";
        tokens["SvcOwner"] = owner ? "userId.Value, " : string.Empty;
        tokens["Implements"] = owner ? ", IOwnedAggregate" : string.Empty;
        tokens["Load"] = owner ? "LoadOwnedAsync" : "LoadAsync";
        tokens["Scope"] = owner ? "caller's own " : string.Empty;
        tokens["OwnerArg"] = owner ? "ownerId, " : string.Empty;
        tokens["OwnerIdArg"] = owner ? "_ownerId, " : string.Empty;
        tokens["AnyOwner"] = owner ? "Arg.Any<UserId>(), " : string.Empty;
    }

    private static void ConcurrencyTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        bool etag = HasEtag(spec);

        tokens["ReadDto"] = etag ? $"Versioned<{spec.Type}Dto>" : $"{spec.Type}Dto";
        tokens["ReadResponse"] = etag ? $"Versioned<{spec.Type}Response>" : $"{spec.Type}Response";
        tokens["Precondition"] = etag ? ", precondition" : string.Empty;
        tokens["NullPrecondition"] = etag ? ", null" : string.Empty;
        tokens["AnyPrecondition"] = etag ? "Arg.Any<VersionPrecondition?>(), " : string.Empty;
    }

    private static void ModelTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        bool record = IsRecord(spec);
        string features = spec.Features;

        tokens["Limits"] = Limits(spec);
        tokens["ListInput"] = record ? $"{spec.Type}ListQueryDto" : $"Get{features}Query";
        tokens["ListInputNamespace"] = record
            ? $"{spec.Prefix}.Application.Features.{features}.Model.Dtos"
            : $"{spec.Prefix}.Application.Features.{features}.UseCases.Queries.Get{features}";
        tokens["CtorParams"] = record ? $"I{spec.Type}Service service" : "ISender sender";
    }

    private static string Limits(FeatureSpec spec) => IsRecord(spec) ? $"{spec.Type}InputPolicy" : spec.Type;

    private static string ServiceArguments(FeatureSpec spec, HashSet<string> flags) =>
        string.Join(
            ", ",
            [
                .. flags.Contains("reads") ? ["_queries"] : Array.Empty<string>(),
                .. flags.Contains("writes") ? ["_store", "_unitOfWork"] : Array.Empty<string>(),
                .. flags.Contains("stampsTime") ? ["TheClock.Start()"] : Array.Empty<string>(),
                .. flags.Contains("takesContent") ? [$"new {spec.Type}ContentDtoValidator()"] : Array.Empty<string>(),
                "currentUser",
            ]);

    private static void CallTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        string type = spec.Type;
        string id = $"{CSharpNames.Lower(type)}Id";
        string precondition = HasEtag(spec) ? ", precondition" : string.Empty;
        string content = string.Join(", ", spec.Fields.Select(field => $"request.{field.Name}"));

        if (IsRecord(spec))
        {
            tokens["CallList"] = "service.ListAsync(query, cancellationToken)";
            tokens["CallGet"] = $"service.GetAsync({id}, cancellationToken)";
            tokens["CallCreate"] = $"service.CreateAsync(new {type}ContentDto({content}), cancellationToken)";
            tokens["CallUpdate"] = $"service.UpdateAsync({id}, new {type}ContentDto({content}){precondition}, cancellationToken)";
            tokens["CallDelete"] = $"service.DeleteAsync({id}{precondition}, cancellationToken)";
        }
        else
        {
            tokens["CallList"] = "sender.Send(query, cancellationToken)";
            tokens["CallGet"] = $"sender.Send(new Get{type}Query({id}), cancellationToken)";
            tokens["CallCreate"] = $"sender.Send(new Create{type}Command({content}), cancellationToken)";
            tokens["CallUpdate"] = $"sender.Send(new Update{type}Command({id}, {content}{precondition}), cancellationToken)";
            tokens["CallDelete"] = $"sender.Send(new Delete{type}Command({id}{precondition}), cancellationToken)";
        }
    }

    private static void DocumentationTokens(Dictionary<string, string> tokens, FeatureSpec spec, HashSet<string> flags)
    {
        var fields = spec.Fields;
        bool summarised = fields.Any(field => !FieldCode.InSummary(field));

        tokens["Staged"] = flags.Contains("trimsText") ? "Trimmed(content)" : "content";
        tokens["ListTail"] = summarised ? ", without their texts" : string.Empty;
        tokens["SummaryDoc"] = summarised ? ", without its texts" : string.Empty;
        tokens["SortDoc"] = string.Join(
            ", ",
            fields.Where(FieldCode.IsSortable).Select(field => CSharpNames.Lower(field.Name))
                .Append("createdAt")
                .Select(field => $"<c>{field}</c>"));
        tokens["DateKeys"] = string.Join(
            ", ",
            fields
                .Where(field => FieldCode.IsKeyset(field) && field.Kind == FieldKind.DateTimeOffset)
                .Select(field => $"{spec.Type}CollectionPolicy.{field.Name}Field")
                .Prepend($"{spec.Type}CollectionPolicy.CreatedAtField"));
    }

    internal static string ManifestPath(FeatureSpec spec) => FeatureManifest.PathOf(spec.Features);

    internal static JsonArray FieldsOf(IReadOnlyList<Field> fields)
    {
        var written = new JsonArray();

        foreach (var field in fields)
        {
            var entry = new JsonObject
            {
                ["name"] = field.Name,
                ["declaration"] = field.Declaration,
                ["kind"] = field.Kind.ToString(),
                ["nullable"] = field.Nullable,
            };

            if (FieldCode.IsReference(field))
            {
                entry["maxLength"] = field.Length;
            }

            if (field.Kind == FieldKind.Decimal)
            {
                entry["precision"] = field.Precision;
                entry["scale"] = field.Scale;
            }

            foreach (var bound in field.Bounds)
            {
                entry[char.ToLowerInvariant(bound.Suffix[0]) + bound.Suffix[1..]] = bound.Literal;
            }

            entry["sortable"] = FieldCode.IsSortable(field);
            entry["inSummary"] = FieldCode.InSummary(field);
            written.Add((JsonNode)entry);
        }

        return written;
    }

    private sealed class PlanWriter(FeatureSpec spec, string root, TemplateOverrides templates)
    {
        private const string _feature = "Features/Placeholders";

        private const string _core = "src/Application/AT.Application.Core";

        private const string _host = "src/Presentation/AT.Api";

        private const string _domain = $"src/Domain/AT.Domain/{_feature}";

        private const string _application = $"src/Application/AT.Application/{_feature}";

        private const string _persistence = $"src/Infrastructure/AT.Infrastructure.Persistence/{_feature}";

        private const string _api = $"{_host}/{_feature}";

        private const string _applicationTests = $"tests/Application/AT.Application.UnitTests/{_feature}";

        private const string _domainTests = $"tests/Domain/AT.Domain.UnitTests/{_feature}";

        private const string _persistenceTests = $"tests/Infrastructure/AT.Infrastructure.Persistence.UnitTests/{_feature}";

        private const string _integrationTests = $"tests/Integration/AT.Api.IntegrationTests/{_feature}";

        private readonly HashSet<string> _flags = Flags(spec);

        private readonly Dictionary<string, string> _tokens = Tokens(spec);

        private readonly TemplateList _fields = FieldBlocks.For(spec.Fields);

        internal List<Emission> Emissions { get; } = [];

        internal List<string> Skipped { get; } = [];

        internal void Reaction(string adapter)
        {
            string jobsHost = JobsHost(root, spec.Prefix);
            var jobs = new Dictionary<string, string>(StringComparer.Ordinal) { ["J"] = ProjectInFolder(spec.Prefix, jobsHost) };

            Add($"{_core}/Common/Ports/IDeletedAccountDataPolicy.cs");
            Add($"{_core}/Common/Ports/IUserAccountDirectory.cs");
            Add($"{_core}/Features/DeletedAccounts/UseCases/Commands/SweepDeletedAccountData/SweepDeletedAccountData.cs");
            Add($"{_host}/Features/BackgroundJobs/Jobs/DeletedAccountSweepJobsExtensions.cs", extra: jobs, host: jobsHost);
            Add($"{_host}/Features/BackgroundJobs/Jobs/DeletedAccountSweepOptions.cs", extra: jobs, host: jobsHost);

            foreach (string host in ComposingHosts(root, spec))
            {
                var named = new Dictionary<string, string>(StringComparer.Ordinal) { ["H"] = ProjectInFolder(spec.Prefix, host) };

                if (adapter == "Auth")
                {
                    Add($"{_host}/Features/Auth/Composition/AccountUserAccountDirectory.cs", extra: named, host: host);
                }
                else
                {
                    Add($"{_host}/Features/ExternalIdentity/Composition/ExternalIdentityUserAccountDirectory.cs", extra: named, host: host);
                }
            }
        }

        internal void Domain()
        {
            Add($"{_domain}/Model/Entities/Placeholder.cs", "aggregate");
            Add($"{_domain}/Repositories/IPlaceholderRepository.cs", "aggregate && writes");
        }

        internal void Application()
        {
            Add($"{_application}/Model/Dtos/PlaceholderContentDto.cs", "record && takesContent");
            Add($"{_application}/Model/Dtos/PlaceholderDto.cs", "returnsOne");
            Add($"{_application}/Model/Dtos/PlaceholderSummaryDto.cs", "list");
            Add($"{_application}/Model/Dtos/PlaceholderListQueryDto.cs", "record && list");
            Add($"{_application}/Model/Errors/PlaceholderErrors.cs", "notFound");
            Add($"{_application}/Model/Policies/PlaceholderCollectionPolicy.cs", "list");
            Add($"{_application}/Model/Policies/PlaceholderInputPolicy.cs", "record && constants");
            Add($"{_application}/Ports/PlaceholderQueries/IPlaceholderQueries.cs", "reads");
            Add($"{_application}/Ports/PlaceholderQueries/PlaceholderFilter.cs", "list");
            Add($"{_application}/Ports/PlaceholderStore/IPlaceholderStore.cs", "record && store");
            Add($"{_application}/Ports/PlaceholderStore/PlaceholderChangeStatus.cs", "record && changesOne");
            Add($"{_application}/Model/Mapping/PlaceholderDtoMapping.cs", "aggregate && takesContent");
            Add($"{_application}/Services/IPlaceholderService.cs", "record || aggregate && changesOne");
            Add($"{_application}/Services/PlaceholderService.cs", "record || aggregate && changesOne");
            Add($"{_application}/Model/Dtos/PlaceholderContentDtoValidator.cs", "record && takesContent");
        }

        internal void UseCases()
        {
            Add($"{_application}/UseCases/Queries/GetPlaceholder/GetPlaceholder.cs", "aggregate && get");
            Add($"{_application}/UseCases/Queries/GetPlaceholder/GetPlaceholderQueryValidator.cs", "aggregate && get");
            Add($"{_application}/UseCases/Queries/GetPlaceholders/GetPlaceholders.cs", "aggregate && list");
            Add($"{_application}/UseCases/Commands/CreatePlaceholder/CreatePlaceholder.cs", "aggregate && create");
            Add($"{_application}/UseCases/Commands/CreatePlaceholder/CreatePlaceholderCommandValidator.cs", "aggregate && create");
            Add($"{_application}/UseCases/Commands/UpdatePlaceholder/UpdatePlaceholder.cs", "aggregate && update");
            Add($"{_application}/UseCases/Commands/UpdatePlaceholder/UpdatePlaceholderCommandValidator.cs", "aggregate && update");
            Add($"{_application}/UseCases/Commands/DeletePlaceholder/DeletePlaceholder.cs", "aggregate && delete");
            Add($"{_application}/UseCases/Commands/DeletePlaceholder/DeletePlaceholderCommandValidator.cs", "aggregate && delete");
            Add($"{_application}/Services/Consumers/DeletePlaceholderOnAccountDeletedConsumer.cs", "owner && erase");
            Add($"{_application}/Services/Consumers/AnonymisePlaceholderOnAccountDeletedConsumer.cs", "owner && anonymise");
        }

        internal void Persistence()
        {
            Add($"{_persistence}/Rows/Models/PlaceholderRecord.cs");
            Add($"{_persistence}/Rows/Configurations/PlaceholderRecordConfiguration.cs");
            Add($"{_persistence}/Access/Stores/PlaceholderStore.cs", "record && store");
            Add($"{_persistence}/Rows/Mapping/IPlaceholderMapper.cs", "aggregate");
            Add($"{_persistence}/Rows/Mapping/PlaceholderMapper.cs", "aggregate");
            Add($"{_persistence}/Rows/Tracking/IPlaceholderTracker.cs", "aggregate");
            Add($"{_persistence}/Rows/Tracking/PlaceholderTracker.cs", "aggregate");
            Add($"{_persistence}/Access/Repositories/PlaceholderRepository.cs", "aggregate && writes");
            Add($"{_persistence}/Access/Queries/PlaceholderQueries.cs", "reads");
            Add($"{_persistence}/Access/Queries/PlaceholderSortMap.cs", "list");
        }

        internal void Api()
        {
            Add($"{_api}/Contracts/Requests/CreatePlaceholderRequest.cs", "create");
            Add($"{_api}/Contracts/Requests/UpdatePlaceholderRequest.cs", "update");
            Add($"{_api}/Contracts/Requests/GetPlaceholdersRequest.cs", "list");
            Add($"{_api}/Contracts/Responses/PlaceholderResponse.cs", "returnsOne");
            Add($"{_api}/Contracts/Responses/PlaceholderSummaryResponse.cs", "list");
            Add($"{_api}/Controllers/PlaceholdersController.cs");
            Add($"{_api}/Contracts/Mapping/PlaceholderResponseMapping.cs", "returnsOne || list");
        }

        internal void UnitTests()
        {
            AddTest($"{_applicationTests}/Model/Dtos/PlaceholderContentDtoValidatorTests.cs", "record && takesContent");
            AddTest($"{_applicationTests}/Services/PlaceholderServiceTests.cs", "record");
            AddTest($"{_applicationTests}/UseCases/Commands/CreatePlaceholder/CreatePlaceholderCommandValidatorTests.cs", "aggregate && create");
            AddTest($"{_applicationTests}/UseCases/Commands/UpdatePlaceholder/UpdatePlaceholderCommandValidatorTests.cs", "aggregate && update");
            AddTest($"{_domainTests}/Model/Entities/PlaceholderTests.cs", "aggregate");
            AddTest($"{_persistenceTests}/Rows/Configurations/PlaceholderRecordConfigurationTests.cs");
            AddTest($"{_persistenceTests}/PlaceholderMapperFidelityTests.cs", "aggregate");
        }

        internal void FieldTests()
        {
            if (spec.HasApi && spec.Has("create"))
            {
                AddTest($"{_integrationTests}/PlaceholderFieldTests.cs");
            }
        }

        private void Add(string reference, string when = "", Dictionary<string, string>? extra = null, string? host = null)
        {
            if (when.Length > 0 && !Condition.Parse(when, FeatureFlags.Vocabulary).Evaluate(_flags))
            {
                return;
            }

            var tokens = extra is null ? _tokens : _tokens.Concat(extra).ToDictionary(pair => pair.Key, pair => pair.Value);

            Emissions.Add(new Emission(PathOf(spec, reference, tokens, host), templates.Render(reference, _flags, tokens, _fields)));
        }

        private void AddTest(string reference, string when = "")
        {
            string project = string.Join('/', PathOf(spec, reference, _tokens).Split('/')[..3]);

            if (Directory.Exists(System.IO.Path.Combine(root, project)))
            {
                Add(reference, when);
            }
            else if (!Skipped.Contains(project, StringComparer.Ordinal))
            {
                Skipped.Add(project);
            }
        }
    }

    private sealed class FieldTokenWriter
    {
        private readonly Dictionary<string, string> _tokens;

        private readonly FeatureSpec _spec;

        private readonly IReadOnlyList<Field> _fields;

        private readonly HashSet<string> _flags;

        private readonly string _limits;

        internal FieldTokenWriter(Dictionary<string, string> tokens, FeatureSpec spec, IReadOnlyList<Field> fields, HashSet<string> flags)
        {
            _tokens = tokens;
            _spec = spec;
            _fields = fields;
            _flags = flags;
            _limits = Limits(spec);
        }

        internal void Contracts()
        {
            _tokens["FRulesCommand"] = FieldCode.Rules(_fields, "command", _spec.Type);
            _tokens["FRulesContent"] = FieldCode.Rules(_fields, "content", _limits);
        }

        internal void Tests()
        {
            _tokens["FServiceInvalid"] = ServiceRefusal();
            _tokens["FContentValidatorTests"] = FieldCode.ValidatorTests(_fields, $"{_spec.Type}ContentDto", "AValidContentDto", _limits);
            _tokens["FCreateValidatorTests"] = FieldCode.ValidatorTests(_fields, $"Create{_spec.Type}Command", "AValidCommand", _spec.Type);
            _tokens["FUpdateValidatorTests"] = FieldCode.ValidatorTests(_fields, $"Update{_spec.Type}Command", "AValidCommand", _spec.Type);
            _tokens["FAggregateTests"] = FieldCode.AggregateTests(_fields, _flags.Contains("create"), _flags.Contains("update"));
            _tokens["FConfigurationTests"] = FieldCode.ConfigurationTests(_fields, _limits);
            _tokens["FApiInstants"] = ApiInstants();
            _tokens["FApiRefusal"] = ApiRefusal();
        }

        private string ServiceRefusal()
        {
            if (_fields.FirstOrDefault(field => FieldCode.Invalid(field, _limits).Count > 0) is not { } refused)
            {
                return string.Empty;
            }

            return FieldCode.Joined(
            [
                "    [Fact]",
                "    public async Task CreateAsync_RejectsInvalidContent_AndStagesNothing()",
                "    {",
                $"        var result = await Service().CreateAsync(AValidContent with {{ {refused.Name} = {FieldCode.Invalid(refused, _limits)[0].Value} }}, TestToken);",
                string.Empty,
                "        result.Error!.Type.ShouldBe(ErrorType.Validation);",
                "        _store.ReceivedCalls().ShouldBeEmpty();",
                "        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());",
                "    }",
            ]);
        }

        private string ApiInstants()
        {
            var instants = _fields.Where(field => field.Kind == FieldKind.DateTimeOffset).ToList();

            if (instants.Count == 0)
            {
                return string.Empty;
            }

            return FieldCode.Joined(
            [
                "    [Fact]",
                "    public async Task EveryDateTimeOffset_SentWithAnOffset_IsKeptAsTheSameInstant_InUtc()",
                "    {",
                "        var (client, _, _) = await SignInAsync();",
                "        var sent = new DateTimeOffset(2026, 3, 4, 7, 6, 7, TimeSpan.FromHours(2));",
                string.Empty,
                $"        using var response = await client.PostAsJsonAsync(_route, AValidRequest() with {{ {string.Join(", ", instants.Select(field => $"{field.Name} = sent"))} }}, TestToken);",
                string.Empty,
                "        response.StatusCode.ShouldBe(HttpStatusCode.Created);",
                string.Empty,
                "        var created = await ApiJson.ReadAsync<%A%Response>(response, TestToken);",
                string.Empty,
                .. instants.SelectMany(field => new[]
                {
                    $"        created.{field.Name}.ShouldBe(sent);",
                    $"        created.{field.Name}{(field.Nullable ? "!.Value" : string.Empty)}.Offset.ShouldBe(TimeSpan.Zero);",
                }),
                "    }",
            ]);
        }

        private string ApiRefusal()
        {
            var refusal = _fields
                .SelectMany(field => FieldCode.Invalid(field, null).Where(invalid => invalid.Case != "Missing").Select(invalid => (Field: field, invalid.Case, invalid.Value)))
                .FirstOrDefault();

            if (refusal.Field is null)
            {
                return string.Empty;
            }

            return FieldCode.Joined(
            [
                "    [Fact]",
                $"    public async Task {refusal.Field.Name}{refusal.Case}_Answers400_WithTheValidationProblem()",
                "    {",
                "        var (client, _, _) = await SignInAsync();",
                string.Empty,
                $"        using var response = await client.PostAsJsonAsync(_route, AValidRequest() with {{ {refusal.Field.Name} = {refusal.Value} }}, TestToken);",
                string.Empty,
                "        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);",
                "        (await ApiJson.ReadProblemAsync(response, TestToken)).Code.ShouldBe(\"request.validationFailed\");",
                "    }",
            ]);
        }
    }
}
