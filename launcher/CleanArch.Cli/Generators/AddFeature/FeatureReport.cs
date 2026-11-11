using CleanArch.Cli.Generators.Shared;
using static CleanArch.Cli.Generators.Shared.CSharpNames;
using static CleanArch.Cli.Generators.Shared.FeatureCode;

namespace CleanArch.Cli.Generators.AddFeature;

internal sealed record FeatureOutput(
    string Root,
    FeatureSpec Spec,
    List<Emission> Plan,
    string Manifest,
    List<string> Skipped,
    Emission? Page,
    string? Unwritten,
    IReadOnlyList<string> InPlace);

internal static class FeatureReport
{
    internal static void Write(TextWriter output, TemplateOverrides templates, FeatureOutput run, ReportMode mode)
    {
        var flags = Flags(run.Spec);

        flags.UnionWith(ShapesOf(run).Where(shape => shape.Holds).Select(shape => shape.Flag));
        templates.WriteReport(output, mode, flags, Tokens(run));
    }

    internal static string ErrorRow(FeatureSpec spec) => $"| `{CSharpNames.Lower(spec.Type)}.notFound` | 404 | Missing, or another caller's |";

    private static Dictionary<string, string> Tokens(FeatureOutput run)
    {
        var spec = run.Spec;
        var tokens = NameTokens(spec);

        tokens["Count"] = $"{run.Plan.Count}";
        tokens["Options"] = Described(spec);
        tokens["Files"] = ReportLines.Of(run.Plan.Select(emission => emission.Path));
        tokens["Manifest"] = run.Manifest;
        tokens["Skipped"] = string.Join('\n', run.Skipped.Select(project => $"Not written: '{project}' is not in this tree, so its tests were skipped."));
        tokens["Edits"] = Sections.Edits(run.Root, spec);
        tokens["TestDatabase"] = TestDatabasePath(spec);
        tokens["Fields"] = string.Join(", ", spec.Fields.Select(field => field.Declaration));
        tokens["Sortable"] = string.Join(", ", Sortable(spec));
        tokens["Kept"] = string.Join(", ", Kept(spec));
        tokens["Steps"] = ReportLines.Of(FeatureGeneration.Steps(run.Root, spec).Select(step => step.Display));
        tokens["Page"] = FeaturePage.PagePath(spec.Features);
        tokens["PageTemplate"] = FeaturePage._pageTemplate;
        tokens["Unwritten"] = run.Unwritten ?? string.Empty;
        tokens["Routes"] = spec.HasApi ? string.Join('\n', FeatureEdits.RouteSection(spec).Select(line => "  " + line)) : string.Empty;
        tokens["ErrorRow"] = ErrorRow(spec);
        tokens["InPlace"] = string.Join("; ", run.InPlace.Distinct(StringComparer.Ordinal));

        return tokens;
    }

    private static (string Flag, bool Holds)[] ShapesOf(FeatureOutput run) =>
    [
        ("api", run.Spec.HasApi),
        ("reaction", run.Spec.Reaction is not null),
        ("fieldTests", WritesTheFieldTests(run.Spec, run.Plan)),
        ("sorted", Sortable(run.Spec).Length > 0),
        ("kept", Kept(run.Spec).Length > 0),
        ("page", run.Page is not null),
        ("inPlace", run.InPlace.Count > 0),
    ];

    private static string[] Sortable(FeatureSpec spec) => [.. spec.Fields.Where(FieldCode.IsSortable).Select(field => field.Name)];

    private static string[] Kept(FeatureSpec spec) =>
        spec.Deletion == "anonymise" ? [.. spec.Fields.Where(field => !FieldCode.IsAnonymised(field)).Select(field => field.Name)] : [];

    private sealed class Sections(TextWriter output, string root, FeatureSpec spec)
    {
        private readonly HashSet<string> _flags = Flags(spec);

        private readonly string[] _hosts = ComposingHosts(root, spec);

        private bool IsRecord => spec.Model == "record";

        private bool IsOwner => spec.Access == "owner";

        private string Persistence => ProjectOf(spec, "src/Infrastructure", "Infrastructure.Persistence");

        internal static string Edits(string root, FeatureSpec spec)
        {
            using var output = new StringWriter { NewLine = "\n" };
            var sections = new Sections(output, root, spec);

            sections.Composition();
            sections.ModuleTests();
            sections.Reaction();

            return output.ToString().Trim('\n');
        }

        private void Composition()
        {
            string f = spec.Features;
            string a = spec.Type;

            output.WriteLine();
            output.WriteLine($"  1. {Persistence}/Common/Contexts/AppDbContext.cs");
            output.WriteLine($"       public const string {f}Schema = \"{Snake(f)}\";");
            output.WriteLine($"       internal DbSet<{a}Record> {f} => Set<{a}Record>();");
            output.WriteLine($"       modelBuilder.ApplyConfiguration(new {a}RecordConfiguration());");
            output.WriteLine($"     with the usings of {spec.Prefix}.Infrastructure.Persistence.Features.{f}.Rows.Configurations and .Rows.Models");
            output.WriteLine();
            ApplicationModule();
            output.WriteLine();
            PersistenceModule();
            output.WriteLine();
            Hosts();
        }

        private void ApplicationModule()
        {
            string a = spec.Type;

            output.WriteLine($"  2. {ProjectOf(spec, "src/Application", "Application")}/ApplicationModule.cs: an Add{spec.Features}() beside the others");
            output.WriteLine($"       services.AddFeature(\"{spec.Features}\");   // its validators, found by folder, and the use-case pipeline");

            if (IsRecord || _flags.Contains("changesOne"))
            {
                output.WriteLine($"       services.AddScoped<I{a}Service, {a}Service>();");
            }

            if (IsOwner)
            {
                string verb = spec.Deletion == "anonymise" ? "Anonymise" : "Delete";

                output.WriteLine($"       services.AddDomainEventConsumer<UserAccountDeleted, {verb}{a}OnAccountDeletedConsumer>();");
                output.WriteLine($"       services.AddScoped<IDeletedAccountDataPolicy, {verb}{a}OnAccountDeletedConsumer>();");
                output.WriteLine("     // the declared fate of this feature's data once the account that owned it is gone, applied");
                output.WriteLine("     // by the consumer when the fact arrives and by the sweep when it never does");
            }
        }

        private void PersistenceModule()
        {
            string a = spec.Type;

            output.WriteLine($"  3. {Persistence}/PersistenceModule.cs: an Add{spec.Features}Feature(services),");
            output.WriteLine("     called from AddPersistenceModule, registering the adapters of this feature's ports:");

            if (IsRecord && _flags.Contains("store"))
            {
                output.WriteLine($"       services.TryAddScoped<I{a}Store, {a}Store>();");
            }

            if (!IsRecord)
            {
                output.WriteLine($"       services.TryAddSingleton<I{a}Mapper, {a}Mapper>();");
                output.WriteLine($"       AddScopedTracker<{a}Tracker, I{a}Tracker>(services);");
                output.WriteLine($"       services.TryAddScoped<I{a}Repository, {a}Repository>();");
            }

            if (_flags.Contains("reads"))
            {
                output.WriteLine($"       services.TryAddScoped<I{a}Queries, {a}Queries>();");
            }
        }

        private void Hosts()
        {
            output.WriteLine($"  4. src/Presentation/{_hosts[0]}/Program.cs: builder.Services.Add{spec.Features}(); beside the other features,");
            output.WriteLine($"     with using {spec.Prefix}.Application; if the file has none yet");

            foreach (string host in _hosts.Skip(1))
            {
                output.WriteLine($"     and src/Presentation/{host}/Program.cs the same, since that host composes the features itself");
            }

            output.WriteLine();
            output.WriteLine($"  5. {ProjectOf(spec, "tests/Architecture", "Architecture.Tests")}/Composition/HostComposition.cs: services.Add{spec.Features}(); in each");
            output.WriteLine("     composition that offers the feature, so the container tests resolve what it registers,");
            output.WriteLine($"     with using {spec.Prefix}.Application; if the file has none yet");
            output.WriteLine();
        }

        private void ModuleTests()
        {
            string moduleTests = $"{ProjectOf(spec, "tests/Application", "Application.UnitTests")}/ApplicationModuleTests.cs";

            if (File.Exists(Path.Combine(root, moduleTests)))
            {
                output.WriteLine($"  6. {moduleTests}: services.Add{spec.Features}(); in ComposeEveryFeature,");
                output.WriteLine("     so the registration facts read the feature's use cases and validators");
            }
            else
            {
                output.WriteLine($"  6. Nothing to edit: {moduleTests} is only generated with a feature,");
                output.WriteLine("     and this project had none. Step 5 is what makes the container tests resolve the feature.");
            }
        }

        private void Reaction()
        {
            if (!IsOwner || spec.Reaction is not { } adapter)
            {
                return;
            }

            string jobsHost = JobsHost(root, spec.Prefix);

            output.WriteLine();
            output.WriteLine($"  7. {ProjectOf(spec, "src/Application", "Application.Core")}/ApplicationCoreModule.cs: the AddDeletedAccountSweep()");
            output.WriteLine("     method before AddUseCasePipeline, composing the use-case pipeline as AddPurgeExpiredIdempotencyKeys does:");

            foreach (string line in FeatureEdits.SweepMethod())
            {
                output.WriteLine(line.Length == 0 ? string.Empty : "     " + line);
            }

            output.WriteLine();
            output.WriteLine($"  8. src/Presentation/{jobsHost}/Program.cs: builder.Services.AddDeletedAccountSweep(); beside");
            output.WriteLine("     builder.Services.AddPurgeExpiredIdempotencyKeys();");
            output.WriteLine();
            output.WriteLine($"  9. src/Presentation/{jobsHost}/Features/BackgroundJobs/Jobs/BackgroundJobsModule.cs:");
            output.WriteLine("     services.AddDeletedAccountSweepJobs(configuration); after services.AddMaintenanceJobs(configuration);");
            output.WriteLine();
            UserAccountDirectory(adapter);
            output.WriteLine();
            DirectoryDouble();
        }

        private void UserAccountDirectory(string adapter)
        {
            string directory = adapter == "Auth" ? "AccountUserAccountDirectory" : "ExternalIdentityUserAccountDirectory";

            output.WriteLine($" 10. {DirectoryComposition(adapter)}: services.AddScoped<IUserAccountDirectory, {directory}>();");
            output.WriteLine("     with the using of Application.Core's ports; the directory shares the composition's namespace");

            foreach (string host in _hosts.Skip(1))
            {
                output.WriteLine($"     and in {HostComposition(root, host, adapter)}, beside the directory written for that host");
            }
        }

        private string DirectoryComposition(string adapter)
        {
            if (!spec.HasApi)
            {
                return HostComposition(root, _hosts[0], adapter);
            }

            string api = ProjectOf(spec, "src/Presentation", "Api");

            return adapter == "Auth"
                ? $"{api}/Features/Auth/Composition/AuthComposition.cs"
                : $"{api}/Features/ExternalIdentity/Composition/ExternalAuthenticationComposition.cs";
        }

        private void DirectoryDouble()
        {
            output.WriteLine($" 11. {ProjectOf(spec, "tests/Architecture", "Architecture.Tests")}/Composition/HostComposition.cs: services.AddDeletedAccountSweep();");
            output.WriteLine("     after services.AddPurgeExpiredIdempotencyKeys(); in each composition, and in AddHostSuppliedAdapters");
            output.WriteLine(spec.HasApi
                ? "     and AddBackgroundOnlyAdapters the double that stands in for the directory the API composes:"
                : "     and AddBackgroundOnlyAdapters the double that stands in for the directory the hosts compose:");
            output.WriteLine("       services.AddScoped<IUserAccountDirectory, ArchitectureTestUserAccountDirectory>();");
            output.WriteLine("     declared beside the other doubles, or the container tests cannot resolve the feature's consumer:");

            foreach (string line in FeatureEdits.DirectoryDouble())
            {
                output.WriteLine("       " + line);
            }
        }
    }
}
