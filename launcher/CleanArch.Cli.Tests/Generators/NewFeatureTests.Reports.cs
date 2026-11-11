using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewFeatureTests
{
    private static readonly (string Group, string Digest)[] _reportDigests =
    [
        ("add feature, applied", "81c26c29e5b68aadf099bd139f489f0ec83f45e93f3836861a53fc125af60f54"),
        ("add feature, refused", "30ded8fc88162e65f7b0282fc67ab6c4e529804a3a3a8b186c12277306f67496"),
        ("add field, refused", "57755d63f42e79f60464dc1b24f09b8a7ffe513df500a6ea3cdae0b7bc379ed9"),
    ];

    [Fact]
    public void Reports() => Expectations.Hold(ReportsCases);

    private static void ReportsCases(string scratch, Action<bool, string> expect)
    {
        foreach (var (group, digest) in ReportDigests(scratch, expect))
        {
            string recorded = _reportDigests.Single(entry => entry.Group == group).Digest;

            expect(
                digest == recorded,
                $"[{group}] the output of these runs is no longer the one recorded: digest {digest}, recorded {recorded}. "
                + "The dry-run digests cover the report's shapes; these cover the text of the other modes. Make a change to it on purpose, then record the new digest.");
        }
    }

    private static IEnumerable<(string Group, string Digest)> ReportDigests(string scratch, Action<bool, string> expect)
    {
        yield return ("add feature, applied", ReportDigest(AppliedFeatures(scratch), 0, expect));
        yield return ("add feature, refused", ReportDigest(RefusedFeatures(scratch), 1, expect));
        yield return ("add field, refused", ReportDigest(RefusedFields(scratch), 1, expect));
    }

    private static IEnumerable<(string Root, string Label, (int Code, string Output, string Error) Result)> AppliedFeatures(string scratch)
    {
        foreach (var (group, options, reaction, api, database) in _digestGroups)
        {
            string root = DigestTree(scratch, reaction, api, database, integration: false);

            string[] concurrency = options.Contains("aggregate") ? ["--concurrency", "etag"] : [];

            yield return (root, group, Invoke(root, ["Widgets", "Widget", .. options, .. concurrency]));
        }

        string fielded = FieldTree(scratch);

        yield return (fielded, "every type", Invoke(fielded, "Invoices", "Invoice", "--access", "owner", "--deletion", "anonymise", "--model", "aggregate", "--concurrency", "etag", "--fields", _everyType));

        string kept = NewTree(scratch);
        string program = System.IO.Path.Combine(kept, "src/Presentation/A.Api/Program.cs");

        File.WriteAllText(program, File.ReadAllText(program).Replace(
            "builder.Services.AddPurgeExpiredIdempotencyKeys();",
            "builder.Services.AddWidgets();\n\nbuilder.Services.AddPurgeExpiredIdempotencyKeys();",
            StringComparison.Ordinal));

        yield return (kept, "an edit in place", Invoke(kept, "Widgets", "Widget", "--access", "shared"));
    }

    private static IEnumerable<(string Root, string Label, (int Code, string Output, string Error) Result)> RefusedFeatures(string scratch)
    {
        foreach (var (group, options, reaction, api, database) in _digestGroups.Where(entry => entry.Group is "record, shared" or "record, owner, delete, the project's first owner feature" or "record, owner, delete, without the API"))
        {
            string root = DigestTree(scratch, reaction, api, database, integration: false);
            string context = System.IO.Path.Combine(root, "src/Infrastructure/A.Infrastructure.Persistence/Common/Contexts/AppDbContext.cs");

            File.WriteAllLines(context, File.ReadAllLines(context).Where(line => line != "    public const string PlatformSchema = \"platform\";"));

            yield return (root, group, Invoke(root, ["Widgets", "Widget", .. options]));
        }
    }

    private static IEnumerable<(string Root, string Label, (int Code, string Output, string Error) Result)> RefusedFields(string scratch)
    {
        const string record = "src/Infrastructure/A.Infrastructure.Persistence/Features/Things/Rows/Models/ThingRecord.cs";

        foreach (bool api in new[] { true, false })
        {
            string root = api ? FieldTree(scratch) : WithoutTheApi(FieldTree(scratch), "A.Worker");

            Invoke(root, ["Things", "Thing", .. _fieldShapes[0].Options, "--fields", _existingFields]);
            File.Delete(System.IO.Path.Combine(root, record));

            yield return (root, api ? "with the API" : "without the API", InvokeField(root, Recording([]), "Things", _everyAdded));
        }
    }

    private static string ReportDigest(IEnumerable<(string Root, string Label, (int Code, string Output, string Error) Result)> runs, int exit, Action<bool, string> expect)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var (root, label, (code, output, error)) in runs)
        {
            expect(code == exit && output.Length > 0, $"[{label}] exited {code} without the report a run exiting {exit} prints: {error}");

            foreach (string text in new[] { label, $"{code}", output, error })
            {
                digest.AppendData(Encoding.UTF8.GetBytes(text.Replace(root, "<root>", StringComparison.Ordinal) + "\u0000"));
            }
        }

        return Convert.ToHexStringLower(digest.GetHashAndReset());
    }
}
