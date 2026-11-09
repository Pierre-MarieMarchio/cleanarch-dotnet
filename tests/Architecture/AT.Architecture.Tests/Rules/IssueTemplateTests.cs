using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class IssueTemplateTests
{
    private const string _vulnerabilities = "SECURITY.md";

    private static readonly Regex _heading = new(@"^## (.+)$", RegexOptions.Multiline, TimeSpan.FromSeconds(5));

    private static readonly Regex _label = new(@"^/label ~\S+$", RegexOptions.Multiline, TimeSpan.FromSeconds(5));

    /*#if (templateMaintenance)*/
    private static string TemplatesPath => Path.Combine(
        ProjectReferenceGraph.RepositoryRoot, ".template.config", "variants", "code-host-gitlab", ".gitlab", "issue_templates");
    /*#else
    private static string TemplatesPath => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".gitlab", "issue_templates");
    #endif*/

    [Fact]
    public void TheIssueTemplates_AskForAReproducibleAndMinimalReport() => AssertTemplates(TemplatesPath);

    internal static void AssertTemplates(string directory)
    {
        AssertTemplate(
            Path.Combine(directory, "Bug.md"),
            ["Version", "Minimal steps to reproduce", "What you expected", "What you observed", "Logs"]);
        AssertTemplate(Path.Combine(directory, "Feature.md"), ["The problem", "The proposal"]);
    }

    internal static string[] Headings(string text) =>
        [.. _heading.Matches(text).Select(match => match.Groups[1].Value.Trim())];

    private static void AssertTemplate(string path, string[] requiredSections)
    {
        File.Exists(path).ShouldBeTrue($"{path} is missing.");
        string text = File.ReadAllText(path);
        string[] headings = Headings(text);

        headings.Distinct(StringComparer.Ordinal).Count().ShouldBe(headings.Length, $"{path} repeats a section.");
        requiredSections.Except(headings, StringComparer.Ordinal).ShouldBeEmpty($"{path} no longer asks for these sections.");
        text.ShouldContain(_vulnerabilities, Case.Sensitive, $"{path} no longer sends a vulnerability to the private report {_vulnerabilities} describes.");
        File.Exists(Path.Combine(ProjectReferenceGraph.RepositoryRoot, _vulnerabilities)).ShouldBeTrue($"{_vulnerabilities} does not exist.");
        _label.IsMatch(text).ShouldBeTrue($"{path} labels nothing: a /label quick action on a line of its own sorts the issue as it is opened.");
    }
}
