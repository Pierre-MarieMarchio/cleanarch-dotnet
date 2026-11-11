using CleanArch.Cli.Generators.AddUseCase;
using CleanArch.Cli.Generators.Shared;
using Xunit;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewUseCaseTests
{
    [Fact]
    public void EveryUseCaseTemplateIsWritten() => Expectations.Hold(EveryUseCaseTemplateIsWrittenCases);

    private static void EveryUseCaseTemplateIsWrittenCases(string scratch, Action<bool, string> expect)
    {
        string root = NewTree(scratch);
        var command = Invoke(root, "Invoices", "ApproveInvoice", "--kind", "command", "--input", "id, Reason:string");
        var query = Invoke(root, "Invoices", "GetInvoiceTotals", "--kind", "query", "--input", "id, Since:DateOnly");
        Dictionary<string, string>[] generations = [Names("ApproveInvoice"), Names("GetInvoiceTotals")];
        string[] unwritten =
        [
            .. ToolTemplates.Of(NewUseCase.TemplateFolder).Keys
                .Where(reference => reference != ToolTemplates.Report && !generations.Any(names => File.Exists(Full(root, PathOf("Acme", reference, names)))))
                .Select(reference => ToolTemplates.PathOf(NewUseCase.TemplateFolder, reference))
                .Order(StringComparer.Ordinal),
        ];

        expect(command.Code == 0 && query.Code == 0, $"add use-case refused a run the template test reads: {command.Error}{query.Error}");
        expect(
            unwritten.Length == 0,
            "add use-case wrote none of these templates for a command and a query that carry the id, an input and a rule, "
            + $"so each is text no generation reads: write it from the plan, or delete it. {string.Join(", ", unwritten)}");

        static Dictionary<string, string> Names(string useCase) =>
            new(StringComparer.Ordinal) { ["F"] = "Invoices", ["A"] = "Invoice", ["UseCase"] = useCase, ["Answer"] = "InvoiceTotals" };
    }
}
