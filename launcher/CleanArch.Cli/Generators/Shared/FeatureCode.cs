using System.Text.Json.Nodes;
using static CleanArch.Cli.Generators.Shared.CSharpNames;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.Shared;

internal static class FeatureCode
{
    internal const string Generator = "dotnet cleanarch add feature";

    internal const string ScriptGenerator = "Tools/NewFeature.cs";

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

    internal static string PathOf(FeatureSpec spec, string reference, string? host, Dictionary<string, string> tokens)
    {
        string[] segments = reference.Split('/');
        string part = segments[2][FolderOf(ReferenceProduct, string.Empty).Length..];

        segments[2] = host ?? FolderOf(spec.Prefix, part);

        return Template.ReplaceReferenceNames(string.Join('/', segments), tokens);
    }

    internal static List<Emission> Plan(FeatureSpec spec, string root, out List<string> skippedTestProjects)
    {
        var plan = new PlanWriter(spec, root);

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
            IsRecord(spec) ? "rec" : "agg",
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

        if (spec.Fields is { } fields)
        {
            FieldTokens(tokens, spec, fields);
        }

        return tokens;
    }

    internal static void FieldTokens(Dictionary<string, string> tokens, FeatureSpec spec, IReadOnlyList<Field> fields)
    {
        var writer = new FieldTokenWriter(tokens, spec, fields, Flags(spec));

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
        ("purge", spec.Deletion == "delete"),
        ("anon", Anonymises(spec)),
        ("prefixed", spec.TablePrefixes),
    ];

    private static (string Flag, bool Holds)[] OperationFlags(FeatureSpec spec)
    {
        bool record = IsRecord(spec);
        bool single = spec.Has("get") || (record && HasAny(spec, "create", "update"));

        return
        [
            ("wq", HasAny(spec, "create", "update", "delete")),
            ("change", HasAny(spec, "update", "delete")),
            ("content", HasAny(spec, "create", "update")),
            ("dto", HasAny(spec, "get", "create", "update")),
            ("single", single),
            ("rq", single || spec.Has("list") || IsOwner(spec)),
            ("stage", record && (HasAny(spec, "create", "update", "delete") || IsOwner(spec))),
            ("notfound", HasAny(spec, "get", "update", "delete") || (record && spec.Has("create"))),
            ("clock", record && HasAny(spec, "create", "update")),
        ];
    }

    private static (string Flag, bool Holds)[] FieldFlags(FeatureSpec spec)
    {
        var fields = spec.Fields;

        return
        [
            ("fields", fields is not null),
            ("anontext", Anonymises(spec) && (fields is null || fields.Any(field => field is { Kind: FieldKind.String, Nullable: false }))),
            ("trim", HasAny(spec, "create", "update") && (fields is null || fields.Any(field => field.Kind == FieldKind.String))),
            ("policy", fields is null || fields.Any(FieldCode.HasConstants)),
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
                .. flags.Contains("rq") ? ["_queries"] : Array.Empty<string>(),
                .. flags.Contains("wq") ? ["_store", "_unitOfWork"] : Array.Empty<string>(),
                .. flags.Contains("clock") ? ["TheClock.Start()"] : Array.Empty<string>(),
                .. flags.Contains("content") ? [$"new {spec.Type}ContentDtoValidator()"] : Array.Empty<string>(),
                "currentUser",
            ]);

    private static void CallTokens(Dictionary<string, string> tokens, FeatureSpec spec)
    {
        string type = spec.Type;
        string id = $"{CSharpNames.Lower(type)}Id";
        string precondition = HasEtag(spec) ? ", precondition" : string.Empty;
        string content = spec.Fields is { } fields
            ? string.Join(", ", fields.Select(field => $"request.{field.Name}"))
            : "request.Name, request.Description";

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
        bool generic = fields is null;

        tokens["Staged"] = flags.Contains("trim") ? "Trimmed(content)" : "content";
        tokens["Replaced"] = generic ? "name and description" : "fields";
        tokens["Revised"] = generic ? "text" : "fields";
        tokens["ListTail"] = Omission(fields, ", names only", ", without their texts");
        tokens["SummaryDoc"] = Omission(fields, ", without its description", ", without its texts");
        tokens["SortDoc"] = string.Join(
            ", ",
            (fields?.Where(FieldCode.IsSortable).Select(field => CSharpNames.Lower(field.Name)) ?? ["name"])
                .Append("createdAt")
                .Select(field => $"<c>{field}</c>"));
        tokens["DateKeys"] = string.Join(
            ", ",
            (fields ?? [])
                .Where(field => FieldCode.IsKeyset(field) && field.Kind == FieldKind.DateTimeOffset)
                .Select(field => $"{spec.Type}CollectionPolicy.{field.Name}Field")
                .Prepend($"{spec.Type}CollectionPolicy.CreatedAtField"));
    }

    private static string Omission(IReadOnlyList<Field>? fields, string placeholders, string texts)
    {
        if (fields is null)
        {
            return placeholders;
        }

        return fields.Any(field => !FieldCode.InSummary(field)) ? texts : string.Empty;
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

    private sealed class PlanWriter(FeatureSpec spec, string root)
    {
        private const string _core = "src/Application/AT.Application.Core";

        private const string _host = "src/Presentation/AT.Api";

        private const string _domain = "src/Domain/AT.Domain/Features/Placeholders";

        private const string _application = "src/Application/AT.Application/Features/Placeholders";

        private const string _queries = $"{_application}/UseCases/Queries";

        private const string _commands = $"{_application}/UseCases/Commands";

        private const string _persistence = "src/Infrastructure/AT.Infrastructure.Persistence/Features/Placeholders";

        private const string _api = $"{_host}/Features/Placeholders";

        private const string _applicationTests = "tests/Application/AT.Application.UnitTests/Features/Placeholders";

        private const string _domainTests = "tests/Domain/AT.Domain.UnitTests/Features/Placeholders";

        private const string _persistenceTests = "tests/Infrastructure/AT.Infrastructure.Persistence.UnitTests/Features/Placeholders";

        private const string _integrationTests = "tests/Integration/AT.Api.IntegrationTests/Features/Placeholders";

        private readonly IReadOnlyDictionary<string, string> _templates = ToolTemplates.Of(ToolTemplates.AddFeature);

        private readonly HashSet<string> _flags = Flags(spec);

        private readonly Dictionary<string, string> _tokens = Tokens(spec);

        private readonly TemplateList? _fields = spec.Fields is { } fields ? FieldBlocks.For(fields, Flags(spec)) : null;

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

            string directory = adapter == "Auth"
                ? $"{_host}/Features/Auth/Composition/AccountUserAccountDirectory.cs"
                : $"{_host}/Features/ExternalIdentity/Composition/ExternalIdentityUserAccountDirectory.cs";

            foreach (string host in ComposingHosts(root, spec))
            {
                var named = new Dictionary<string, string>(StringComparer.Ordinal) { ["H"] = ProjectInFolder(spec.Prefix, host) };

                Add(directory, extra: named, host: host);
            }
        }

        internal void Domain()
        {
            Add($"{_domain}/Model/Entities/Placeholder.cs", "agg");
            Add($"{_domain}/Repositories/IPlaceholderRepository.cs", "agg&wq");
        }

        internal void Application()
        {
            Add($"{_application}/Model/Dtos/PlaceholderContentDto.cs", "rec&content");
            Add($"{_application}/Model/Dtos/PlaceholderDto.cs", "dto");
            Add($"{_application}/Model/Dtos/PlaceholderSummaryDto.cs", "list");
            Add($"{_application}/Model/Dtos/PlaceholderListQueryDto.cs", "rec&list");
            Add($"{_application}/Model/Errors/PlaceholderErrors.cs", "notfound");
            Add($"{_application}/Model/Policies/PlaceholderCollectionPolicy.cs", "list");
            Add($"{_application}/Model/Policies/PlaceholderInputPolicy.cs", "rec&policy");
            Add($"{_application}/Ports/PlaceholderQueries/IPlaceholderQueries.cs", "rq");
            Add($"{_application}/Ports/PlaceholderQueries/PlaceholderFilter.cs", "list");
            Add($"{_application}/Ports/PlaceholderStore/IPlaceholderStore.cs", "rec&stage");
            Add($"{_application}/Ports/PlaceholderStore/PlaceholderChangeStatus.cs", "rec&change");
            Add($"{_application}/Model/Mapping/PlaceholderDtoMapping.cs", "agg&content");
            Add($"{_application}/Services/IPlaceholderService.cs", "rec|agg&change");
            Add($"{_application}/Services/PlaceholderService.cs", "rec|agg&change");
            Add($"{_application}/Model/Dtos/PlaceholderContentDtoValidator.cs", "rec&content");
        }

        internal void UseCases()
        {
            Add($"{_queries}/GetPlaceholder/GetPlaceholder.cs", "agg&get");
            Add($"{_queries}/GetPlaceholder/GetPlaceholderQueryValidator.cs", "agg&get");
            Add($"{_queries}/GetPlaceholders/GetPlaceholders.cs", "agg&list");
            Add($"{_commands}/CreatePlaceholder/CreatePlaceholder.cs", "agg&create");
            Add($"{_commands}/CreatePlaceholder/CreatePlaceholderCommandValidator.cs", "agg&create");
            Add($"{_commands}/UpdatePlaceholder/UpdatePlaceholder.cs", "agg&update");
            Add($"{_commands}/UpdatePlaceholder/UpdatePlaceholderCommandValidator.cs", "agg&update");
            Add($"{_commands}/DeletePlaceholder/DeletePlaceholder.cs", "agg&delete");
            Add($"{_commands}/DeletePlaceholder/DeletePlaceholderCommandValidator.cs", "agg&delete");
            Add($"{_application}/Services/Consumers/DeletePlaceholderOnAccountDeletedConsumer.cs", "purge");
            Add($"{_application}/Services/Consumers/AnonymisePlaceholderOnAccountDeletedConsumer.cs", "anon");
        }

        internal void Persistence()
        {
            Add($"{_persistence}/Rows/Models/PlaceholderRecord.cs");
            Add($"{_persistence}/Rows/Configurations/PlaceholderRecordConfiguration.cs");
            Add($"{_persistence}/Access/Stores/PlaceholderStore.cs", "rec&stage");
            Add($"{_persistence}/Rows/Mapping/IPlaceholderMapper.cs", "agg");
            Add($"{_persistence}/Rows/Mapping/PlaceholderMapper.cs", "agg");
            Add($"{_persistence}/Rows/Tracking/IPlaceholderTracker.cs", "agg");
            Add($"{_persistence}/Rows/Tracking/PlaceholderTracker.cs", "agg");
            Add($"{_persistence}/Access/Repositories/PlaceholderRepository.cs", "agg&wq");
            Add($"{_persistence}/Access/Queries/PlaceholderQueries.cs", "rq");
            Add($"{_persistence}/Access/Queries/PlaceholderSortMap.cs", "list");
        }

        internal void Api()
        {
            Add($"{_api}/Contracts/Requests/CreatePlaceholderRequest.cs", "create");
            Add($"{_api}/Contracts/Requests/UpdatePlaceholderRequest.cs", "update");
            Add($"{_api}/Contracts/Requests/GetPlaceholdersRequest.cs", "list");
            Add($"{_api}/Contracts/Responses/PlaceholderResponse.cs", "dto");
            Add($"{_api}/Contracts/Responses/PlaceholderSummaryResponse.cs", "list");
            Add($"{_api}/Controllers/PlaceholdersController.cs");
            Add($"{_api}/Contracts/Mapping/PlaceholderResponseMapping.cs", "dto|list");
        }

        internal void UnitTests()
        {
            AddTest($"{_applicationTests}/Model/Dtos/PlaceholderContentDtoValidatorTests.cs", "rec&content");
            AddTest($"{_applicationTests}/Services/PlaceholderServiceTests.cs", "rec");
            AddTest($"{_applicationTests}/UseCases/Commands/CreatePlaceholder/CreatePlaceholderCommandValidatorTests.cs", "agg&create", CommandValidatorTokens("Create"));
            AddTest($"{_applicationTests}/UseCases/Commands/UpdatePlaceholder/UpdatePlaceholderCommandValidatorTests.cs", "agg&update", CommandValidatorTokens("Update"));
            AddTest($"{_domainTests}/Model/Entities/PlaceholderTests.cs", "agg");
            AddTest($"{_persistenceTests}/Rows/Configurations/PlaceholderRecordConfigurationTests.cs");
            AddTest($"{_persistenceTests}/PlaceholderMapperFidelityTests.cs", "agg");
        }

        internal void FieldTests()
        {
            if (spec.HasApi && spec.Fields is not null && spec.Has("create"))
            {
                AddTest($"{_integrationTests}/PlaceholderFieldTests.cs");
            }
        }

        private Dictionary<string, string>? CommandValidatorTokens(string operation)
        {
            if (spec.Fields is not { } fields)
            {
                return null;
            }

            var samples = fields.Select(field => FieldCode.Sample(field, 1));

            return new()
            {
                ["FValidatorTests"] = FieldCode.ValidatorTests(fields, $"{operation}{spec.Type}Command", "AValidCommand", spec.Type),
                ["FValidCommand"] = FieldCode.Construction(operation == "Create" ? samples : samples.Prepend("Guid.CreateVersion7()")),
            };
        }

        private void Add(string reference, string when = "", Dictionary<string, string>? extra = null, string? host = null)
        {
            if (when.Length > 0 && !Template.Evaluate(when, _flags))
            {
                return;
            }

            var tokens = extra is null ? _tokens : _tokens.Concat(extra).ToDictionary(pair => pair.Key, pair => pair.Value);

            Emissions.Add(new Emission(
                PathOf(spec, reference, host, tokens),
                Template.Render(_templates[reference], _flags, tokens, _fields)));
        }

        private void AddTest(string reference, string when = "", Dictionary<string, string>? extra = null)
        {
            string project = string.Join('/', PathOf(spec, reference, host: null, _tokens).Split('/')[..3]);

            if (Directory.Exists(System.IO.Path.Combine(root, project)))
            {
                Add(reference, when, extra);
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
            _tokens["FPolicyConstants"] = FieldCode.Constants(_fields);
            _tokens["FRulesCommand"] = FieldCode.Rules(_fields, "command", _spec.Type);
            _tokens["FRulesContent"] = FieldCode.Rules(_fields, "content", _limits);
            _tokens["FSortables"] = FieldCode.Joined(_fields.Where(FieldCode.IsSortable).Select(field =>
                $"        SortableField.{(FieldCode.IsKeyset(field) ? "Keyset" : "OffsetOnly")}({field.Name}Field),"));
        }

        internal void Tests()
        {
            _tokens["FServiceInvalid"] = ServiceRefusal();
            _tokens["FContentValidatorTests"] = FieldCode.ValidatorTests(_fields, $"{_spec.Type}ContentDto", "AValidContentDto", _limits);
            _tokens["FAggregateTests"] = FieldCode.AggregateTests(_fields, _flags.Contains("create"), _flags.Contains("update"), "%OwnerIdArg%");
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
