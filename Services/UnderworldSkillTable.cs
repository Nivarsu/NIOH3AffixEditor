using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Nioh3AffixEditor.Services;

public sealed class UnderworldSkillTable
{
    private const string BuiltInResourceName = "Nioh3AffixEditor.Resources.underworld_skill_table.csv";

    private readonly Dictionary<int, string> _idToName;
    private readonly HashSet<string> _duplicateNames;
    private readonly Dictionary<string, int> _displayToId;

    private UnderworldSkillTable(
        Dictionary<int, string> idToName,
        HashSet<string> duplicateNames,
        Dictionary<string, int> displayToId,
        IReadOnlyList<string> options)
    {
        _idToName = idToName;
        _duplicateNames = duplicateNames;
        _displayToId = displayToId;
        Options = options;
    }

    public static UnderworldSkillTable LoadDefault()
    {
        var overridePath = AppPaths.GetUnderworldSkillTablePath();
        if (File.Exists(overridePath))
        {
            try
            {
                using var fs = File.OpenRead(overridePath);
                using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return LoadFromCsv(sr);
            }
            catch
            {
                // Fall back to built-in.
            }
        }

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream(BuiltInResourceName);
            if (s is null)
            {
                return Empty();
            }

            using var sr = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return LoadFromCsv(sr);
        }
        catch
        {
            return Empty();
        }
    }

    public static UnderworldSkillTable Empty()
        => new(new Dictionary<int, string>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase), new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), Array.Empty<string>());

    public IReadOnlyList<string> Options { get; }

    public bool TryGetName(int underworldSkillId, out string name)
        => _idToName.TryGetValue(underworldSkillId, out name!);

    public string FormatForDisplay(int underworldSkillId)
    {
        if (!TryGetName(underworldSkillId, out var name))
        {
            return underworldSkillId.ToString(CultureInfo.InvariantCulture);
        }

        return _duplicateNames.Contains(name)
            ? $"{name} ({underworldSkillId.ToString(CultureInfo.InvariantCulture)})"
            : name;
    }

    public bool TryResolveToId(string? input, out int underworldSkillId)
    {
        underworldSkillId = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            underworldSkillId = numeric;
            return true;
        }

        if (_displayToId.TryGetValue(trimmed, out var fromDisplay))
        {
            underworldSkillId = fromDisplay;
            return true;
        }

        if (TryParseTrailingIdInParens(trimmed, out var parsedFromParens))
        {
            underworldSkillId = parsedFromParens;
            return true;
        }

        return false;
    }

    private static bool TryParseTrailingIdInParens(string text, out int id)
    {
        id = 0;
        var open = text.LastIndexOf('(');
        var close = text.LastIndexOf(')');
        if (open < 0 || close < 0 || close < open)
        {
            return false;
        }

        var inner = text.Substring(open + 1, close - open - 1).Trim();
        return int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
    }

    private static UnderworldSkillTable LoadFromCsv(TextReader reader)
    {
        var idToName = new Dictionary<int, string>();
        var allNames = new List<string>();

        bool headerConsumed = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (fields.Count < 2)
            {
                continue;
            }

            if (!headerConsumed && fields[0].Trim().Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                headerConsumed = true;
                continue;
            }

            headerConsumed = true;

            if (!int.TryParse(fields[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            var name = fields[1].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (!idToName.TryAdd(id, name))
            {
                continue;
            }

            allNames.Add(name);
        }

        var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in allNames)
        {
            counts.TryGetValue(name, out var cnt);
            counts[name] = cnt + 1;
        }

        foreach (var kv in counts)
        {
            if (kv.Value > 1)
            {
                duplicateNames.Add(kv.Key);
            }
        }

        var displayToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var options = new List<string>(idToName.Count);
        foreach (var kv in idToName.OrderBy(k => k.Value, StringComparer.OrdinalIgnoreCase).ThenBy(k => k.Key))
        {
            var display = duplicateNames.Contains(kv.Value) ? $"{kv.Value} ({kv.Key})" : kv.Value;
            if (!displayToId.TryAdd(display, kv.Key))
            {
                display = $"{kv.Value} ({kv.Key})";
                displayToId[display] = kv.Key;
            }

            options.Add(display);
        }

        return new UnderworldSkillTable(idToName, duplicateNames, displayToId, options);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                sb.Append(ch);
                continue;
            }

            if (ch == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            if (ch == '"')
            {
                inQuotes = true;
                continue;
            }

            sb.Append(ch);
        }

        fields.Add(sb.ToString());
        return fields;
    }
}

