using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

static class LocalesHarness
{
    public static void Run(Action<bool, string> check)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "ModManifest.asset"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Run the locale checks from a repository build.");
        string locales = Path.Combine(root.FullName, "Locales");
        string[] expected = "cs da de el en es fi fr hu it ja ko lt nl pl pt ro ru tr uk zh-cn zh-tw".Split(' ');
        var files = Directory.GetFiles(locales, "*.json").OrderBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal).ToArray();
        check(files.Select(Path.GetFileNameWithoutExtension).SequenceEqual(expected), "Locales: all 22 native selectable language codes");
        using var english = Read(Path.Combine(locales, "en.json"));
        var baseline = english.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString(), StringComparer.Ordinal);
        check(baseline["bacg_menu_controls"].Contains("{0}") && baseline["bacg_menu_controls"].Contains("{1}") &&
            baseline["bacg_menu_back"].Contains("{0}") && baseline["bacg_menu_back"].Contains("{1}") &&
            baseline["bacg_retry"].Contains("{0}") && baseline["bacg_return_menu"].Contains("{0}"),
            "Locales: every session hint uses live shortcut placeholders");
        check(new[] { "bacg_shortcuts_header", "bacg_shortcut_return", "bacg_shortcut_leave", "bacg_shortcut_unbound",
                "bacg_shortcut_capture", "bacg_shortcut_conflict" }.All(baseline.ContainsKey),
            "Locales: shortcut selector labels are translated by every locale");
        var keys = baseline.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var guids = new List<string>();
        foreach (var file in files)
        {
            using var document = Read(file);
            var values = document.RootElement.EnumerateObject().ToArray();
            bool valid = values.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() == values.Length &&
                values.Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(keys) &&
                values.All(p => p.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.Value.GetString()) &&
                    Placeholders(p.Value.GetString()).SequenceEqual(Placeholders(baseline[p.Name])));
            check(valid, "Locales: " + Path.GetFileNameWithoutExtension(file) + " has every key, valid UTF-8 text and matching placeholders");
            guids.Add(Regex.Match(File.ReadAllText(file + ".meta"), @"(?m)^guid: ([a-f0-9]{32})\r?$").Groups[1].Value);
        }
        check(guids.All(g => g.Length == 32) && guids.Distinct(StringComparer.Ordinal).Count() == files.Length,
            "Locales: each Unity asset has a unique valid GUID");
        var referenced = Directory.GetFiles(Path.Combine(root.FullName, "Scripts"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), "\"(bacg_[a-z0-9_]+)\"").Select(m => m.Groups[1].Value)).Distinct();
        check(referenced.All(baseline.ContainsKey), "Locales: all localization keys referenced by MCG sources exist");
    }
    static JsonDocument Read(string path) => JsonDocument.Parse(new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF'));
    static IEnumerable<string> Placeholders(string value) => Regex.Matches(value, @"(?<!\{)\{\d+(?:[^{}]*)\}(?!\})")
        .Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal);
}
