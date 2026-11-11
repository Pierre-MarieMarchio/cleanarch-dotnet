namespace CleanArch.Cli.Generators.Shared;

internal static class FeatureFlags
{
    internal static FlagSet Vocabulary { get; } = new(
        FeatureCode.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["get"] = "`get` is among `--operations`: one is read by its id.",
            ["list"] = "`list` is among `--operations`: a page of them is read.",
            ["create"] = "`create` is among `--operations`.",
            ["update"] = "`update` is among `--operations`.",
            ["delete"] = "`delete` is among `--operations`.",
            ["owner"] = "`--access owner`: each one belongs to the caller who created it, and the deletion of that account reaches it.",
            ["shared"] = "`--access shared`: every caller reaches every one.",
            ["record"] = "`--model record`: a service over a store, without a domain aggregate.",
            ["aggregate"] = "`--model aggregate`: one use case per operation over a domain aggregate.",
            ["etag"] = "`--concurrency etag`: a persisted version, an `ETag` and `If-Match`.",
            ["erase"] = "`--deletion delete`: the rows of a deleted account are removed.",
            ["anonymise"] = "`--deletion anonymise`: what identifies the owner of a deleted account is rewritten.",
            ["prefixedTables"] = "The database is SQLite, which has no schemas, so a table's name starts with its feature's schema.",
            ["writes"] = "Create, update or delete is among the operations, so the feature writes through a store or a repository.",
            ["changesOne"] = "Update or delete is among the operations: each changes one that can be missing, or stale under `etag`.",
            ["takesContent"] = "Create or update is among the operations: each takes the feature's content and validates it.",
            ["returnsOne"] = "Get, create or update is among the operations: each answers with one in full.",
            ["readsOne"] = "Get is among the operations, or a `record` creates or updates and reads back what it wrote: the queries port reads one by its id.",
            ["reads"] = "The feature has a queries port: it reads one, lists, or is `owner`, whose account deletion lists the owners.",
            ["store"] = "A `record` that writes or is `owner` stages its changes through a store.",
            ["notFound"] = "An operation can answer that one is not found: get, update, delete, or a `record`'s create.",
            ["stampsTime"] = "A `record` creates or updates, so its service reads the time it stamps.",
            ["anonymisedText"] = "Under `anonymise`, a required text is rewritten to a fixed text: a field is a non-nullable string.",
            ["trimsText"] = "Create or update takes a text the service trims: a field is a string.",
            ["constants"] = "A limit of the content is declared as a constant: a field is a string, a text or a decimal, or has a bound.",
        });

    internal static FlagSet ReportShapes { get; } = new(
        FeatureCode.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["api"] = "The project has the API, so the feature's routes and its refusal join `docs/reference/api.md`.",
            ["reaction"] = "The project had no feature that owns user data, so the account-deleted reaction ships with this one, in five more edits.",
            ["fieldTests"] = "The integration test of the fields is written, and the database fixture truncates the feature's schema.",
            ["sorted"] = "A field sorts the list.",
            ["kept"] = "Under `anonymise`, a field is kept as it was: one that is not a string, a text or nullable.",
            ["page"] = "The feature's page is written.",
            ["inPlace"] = "An edit was in place already, so it was left as it was and not recorded.",
        });

    internal static FlagSet Report { get; } = ReportFlags.For(Vocabulary, ReportShapes);
}
