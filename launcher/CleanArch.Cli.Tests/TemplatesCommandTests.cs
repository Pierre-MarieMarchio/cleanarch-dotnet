using System.Text;
using System.Text.Json.Nodes;
using CleanArch.Cli.Generators.Shared;
using Shouldly;
using Xunit;

namespace CleanArch.Cli.Tests;

public sealed class TemplatesCommandTests : IDisposable
{
    private const string _generator = "add-feature";

    private const string _controller = "PlaceholdersController.cs";

    private const string _entity = "src/Domain/AT.Domain/Features/Placeholders/Model/Entities/Placeholder.cs";

    private readonly SampleProject _project = new(SampleProject.Api);
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    private static IReadOnlyDictionary<string, string> Templates => ToolTemplates.Of(_generator);

    private static string ControllerReference => Templates.Keys.Single(reference => reference.EndsWith("/" + _controller, StringComparison.Ordinal));

    public static TheoryData<string> GeneratorsRefusingAProjectWithoutAuthentication => ["add-feature"];

    public void Dispose()
    {
        _project.Dispose();
        _output.Dispose();
        _error.Dispose();
    }

    [Fact]
    public void CopyWritesEachTemplateAsTheToolHoldsIt_AndRecordsItsSource()
    {
        Run("templates", "copy", _generator).ShouldBe(CleanArchCommand.Succeeded, _error.ToString());

        foreach (var (reference, text) in Templates)
        {
            File.ReadAllBytes(Copied(reference)).ShouldBe(Encoding.UTF8.GetBytes(text + "\n"), reference);
        }

        Recorded().ShouldBe(
            [.. Templates.OrderBy(template => ToolTemplates.PathOf(_generator, template.Key), StringComparer.Ordinal)
                .Select(template => (ToolTemplates.PathOf(_generator, template.Key), GeneratorCommand.Version, ToolTemplates.Digest(template.Value)))]);
        _output.ToString().ShouldStartWith(
            $"Wrote {Templates.Count} templates of add feature from version {GeneratorCommand.Version} of the tool, recorded in {TemplateCopies.Manifest}:{Environment.NewLine}"
            + $"  {ToolTemplates.PathOf(_generator, Templates.Keys.Order(StringComparer.Ordinal).First())}{Environment.NewLine}");
        _output.ToString().ShouldEndWith($"{Environment.NewLine}add feature and add field now write those files from them.{Environment.NewLine}");
    }

    [Fact]
    public void CopyOfNamedTemplatesWritesOnlyThem()
    {
        Run("templates", "copy", _generator, _controller, _entity).ShouldBe(CleanArchCommand.Succeeded, _error.ToString());

        string[] written = [ToolTemplates.PathOf(_generator, ControllerReference), ToolTemplates.PathOf(_generator, _entity)];
        CopiedFiles().ShouldBe(written, ignoreOrder: true);
        Recorded().Select(copy => copy.Path).ShouldBe(written.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CopyNeverOverwrites_AndRefusesWhenEveryTemplateNamedExists()
    {
        Run("templates", "copy", _generator, _controller).ShouldBe(CleanArchCommand.Succeeded, _error.ToString());
        File.WriteAllText(Copied(ControllerReference), "this project's controller\n");
        Dictionary<string, string> before = Snapshot(_project.Root);

        Run("templates", "copy", _generator, _controller).ShouldBe(CleanArchCommand.Failed);
        _error.ToString().ShouldStartWith("Every template to copy exists in this project already, and none is ever overwritten: move one aside to copy it again.");
        Snapshot(_project.Root).ShouldBe(before);

        Run("templates", "copy", _generator, _controller, _entity).ShouldBe(CleanArchCommand.Succeeded, _error.ToString());
        File.ReadAllText(Copied(ControllerReference)).ShouldBe("this project's controller\n");
        _output.ToString().ShouldContain($"Kept, as none is ever overwritten:{Environment.NewLine}  {ToolTemplates.PathOf(_generator, ControllerReference)}");
        Recorded().Select(copy => copy.Path).ShouldBe([ToolTemplates.PathOf(_generator, ControllerReference), ToolTemplates.PathOf(_generator, _entity)], ignoreOrder: true);
    }

    [Theory]
    [InlineData("add-widget", "add-widget has no code templates to copy; the generators that have some are add-feature, add-use-case.")]
    [InlineData("add-feature Widget.cs", "Widget.cs names no template of add feature: dotnet cleanarch templates copy add-feature --dry-run lists them.")]
    [InlineData("add-feature Model/Entities", "Model/Entities names no template of add feature: dotnet cleanarch templates copy add-feature --dry-run lists them.")]
    public void CopyRefusesAnUnknownGeneratorOrTemplate(string arguments, string refusal)
    {
        Run(["templates", "copy", .. arguments.Split(' ')]).ShouldBe(CleanArchCommand.Failed);

        _error.ToString().ShouldBe(refusal + Environment.NewLine);
        Directory.Exists(Path.Combine(_project.Root, ToolTemplates.Folder)).ShouldBeFalse();
    }

    [Fact]
    public void CopyRefusesAManifestItCannotRead_AndWritesNothing()
    {
        string manifest = Path.Combine(_project.Root, TemplateCopies.Manifest);
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        File.WriteAllText(manifest, """{ "schemaVersion": 1, "templates": [ { "path": "x" } ] }""");
        Dictionary<string, string> before = Snapshot(_project.Root);

        Run("templates", "copy", _generator).ShouldBe(CleanArchCommand.Failed);

        _error.ToString().ShouldStartWith($"{TemplateCopies.Manifest} does not read as the record of the templates copied");
        Snapshot(_project.Root).ShouldBe(before);
    }

    [Fact]
    public void DryRunWritesNothing()
    {
        Run("templates", "copy", _generator, _controller, "--dry-run").ShouldBe(CleanArchCommand.Succeeded, _error.ToString());

        _output.ToString().ShouldBe(
            $"Would write 1 template of add feature from version {GeneratorCommand.Version} of the tool, recorded in {TemplateCopies.Manifest}:{Environment.NewLine}"
            + $"  {ToolTemplates.PathOf(_generator, ControllerReference)}{Environment.NewLine}{Environment.NewLine}"
            + $"--dry-run: nothing was written.{Environment.NewLine}");
        Directory.Exists(Path.Combine(_project.Root, ToolTemplates.Folder)).ShouldBeFalse();
    }

    [Fact]
    public void CopyAddUseCase_WithoutAuthentication()
    {
        using var unauthenticated = new SampleProject(SampleProject.Minimal);
        var templates = ToolTemplates.Of("add-use-case");

        Run(["templates", "copy", "add-use-case"], unauthenticated.Root).ShouldBe(CleanArchCommand.Succeeded, _error.ToString());

        foreach (var (reference, text) in templates)
        {
            File.ReadAllText(Path.Combine(unauthenticated.Root, ToolTemplates.PathOf("add-use-case", reference))).ShouldBe(text + "\n", reference);
        }

        _output.ToString().ShouldStartWith($"Wrote {templates.Count} templates of add use-case from version {GeneratorCommand.Version} of the tool");
        _output.ToString().ShouldEndWith($"{Environment.NewLine}add use-case now writes those files from them.{Environment.NewLine}");
    }

    [Theory]
    [MemberData(nameof(GeneratorsRefusingAProjectWithoutAuthentication))]
    public void EveryGeneratorRefusingAProjectRefusesToCopyItsTemplatesThere(string generator)
    {
        using var unauthenticated = new SampleProject(SampleProject.Minimal);
        ToolTemplates.Of(generator).ShouldNotBeEmpty();

        Run([.. generator.Split('-')], unauthenticated.Root).ShouldBe(CleanArchCommand.Failed);
        string refusal = _error.ToString().Trim();
        Run(["templates", "copy", generator], unauthenticated.Root).ShouldBe(CleanArchCommand.Failed);

        refusal.ShouldNotBeEmpty();
        _error.ToString().ShouldContain(refusal);
        Directory.Exists(Path.Combine(unauthenticated.Root, ToolTemplates.Folder)).ShouldBeFalse();
    }

    private int Run(params string[] arguments) => Run(arguments, _project.Root);

    private int Run(string[] arguments, string root)
    {
        _output.GetStringBuilder().Clear();
        _error.GetStringBuilder().Clear();
        return BuiltInCommands.Run(arguments, root, _output, _error, new FakeProcesses());
    }

    private string Copied(string reference) => Path.Combine(_project.Root, ToolTemplates.PathOf(_generator, reference));

    private string[] CopiedFiles() =>
        [.. Directory.EnumerateFiles(Path.Combine(_project.Root, ToolTemplates.Folder, _generator), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_project.Root, path).Replace('\\', '/'))];

    private (string Path, string Version, string Sha256)[] Recorded()
    {
        JsonNode manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(_project.Root, TemplateCopies.Manifest))).ShouldNotBeNull();
        manifest["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        return [.. manifest["templates"]!.AsArray().Select(copy => (copy!["path"]!.GetValue<string>(), copy["version"]!.GetValue<string>(), copy["sha256"]!.GetValue<string>()))];
    }

    private static Dictionary<string, string> Snapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllText);
}
