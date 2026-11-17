using System.Xml.Linq;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class AppHostIsolationTests
{
    private const string _appHost = "AppTemplate.AppHost";

    private const string _why =
        "the app host stands outside the four layers: it starts the local stack for development, so it reaches the "
        + "hosts and the product never reaches it.";

    [Fact]
    public void NoSourceProject_ReferencesTheAppHost()
    {
        ProjectReferenceGraph.Project(_appHost);

        ProjectReferenceGraph.SourceProjects.Values
            .Where(project => project.References.Contains(_appHost))
            .Select(project => project.RelativePath)
            .ShouldBeEmpty($"These projects reference the app host; remove the reference, since {_why}");
    }

    [Fact]
    public void TheAppHost_ReferencesOnlyPresentationProjects()
    {
        string projectFile = Path.Combine(ProjectReferenceGraph.RepositoryRoot, ProjectReferenceGraph.Project(_appHost).RelativePath);

        OutsidePresentation(XDocument.Load(projectFile))
            .ShouldBeEmpty($"The app host references these projects outside src/Presentation; reference the hosts alone, since {_why}");
    }

    [Fact]
    public void TheRule_IsSensitive_AndSeesAReferenceOutsidePresentation()
    {
        var project = XDocument.Parse(
            """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\Presentation\*\*.csproj" />
                <ProjectReference Include="..\Infrastructure\Storage\Storage.csproj" />
              </ItemGroup>
            </Project>
            """);

        OutsidePresentation(project).ShouldBe([@"..\Infrastructure\Storage\Storage.csproj"]);
    }

    private static IEnumerable<string> OutsidePresentation(XDocument project) =>
        project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Where(include => !include.Replace('\\', '/').StartsWith("../Presentation/", StringComparison.Ordinal));
}
