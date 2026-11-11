using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddSetting;

internal static class KeyBlocks
{
    internal static SettingKey Broken(SettingSpec spec) => spec.Keys.First(key => key.IsValidated);

    internal static TemplateList For(SettingSpec spec)
    {
        string broken = Broken(spec).Name;

        return TemplateList.Of(
            "key",
            spec.Keys,
            new Dictionary<string, Func<SettingKey, string>>(StringComparer.Ordinal)
            {
                ["Name"] = key => key.Name,
                ["Type"] = key => key.ClrType,
                ["Default"] = KeyCode.Default,
                ["Condition"] = KeyCode.Condition,
                ["Message"] = KeyCode.Message,
                ["PassingCases"] = KeyCode.PassingCases,
                ["Passing"] = KeyCode.Passing,
                ["PassingParameter"] = KeyCode.PassingParameter,
                ["PassingValue"] = KeyCode.PassingValue,
                ["FailingCases"] = KeyCode.FailingCases,
                ["Failing"] = KeyCode.Failing,
                ["Parameter"] = KeyCode.Parameter,
                ["Value"] = KeyCode.Value,
                ["Valid"] = KeyCode.Valid,
                ["Setting"] = KeyCode.Setting,
                ["Expected"] = KeyCode.Expected,
            },
            new Dictionary<string, Func<SettingKey, bool>>(StringComparer.Ordinal)
            {
                ["validated"] = key => key.IsValidated,
                ["required"] = key => key.IsRequired,
                ["supplied"] = key => key.IsRequired && key.Name != broken,
            });
    }
}
