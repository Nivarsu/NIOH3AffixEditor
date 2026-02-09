using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Nioh3AffixEditor.Services;

public sealed class AffixIdTable
{
    private const string BuiltInResourceName = "Nioh3AffixEditor.Resources.affix_id_table.csv";

    private readonly Dictionary<int, string> _idToName;
    private readonly Dictionary<string, int> _displayToId;

    private AffixIdTable(
        Dictionary<int, string> idToName,
        Dictionary<string, int> displayToId,
        IReadOnlyList<string> options)
    {
        _idToName = idToName;
        _displayToId = displayToId;
        Options = options;
    }

    public static AffixIdTable LoadDefault()
    {
        // Optional override: if the user drops a CSV into AppData, use it.
        // This keeps the app usable even if the embedded table is incomplete/outdated.
        var overridePath = AppPaths.GetAffixIdTablePath();
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

    public static AffixIdTable Empty()
        => new(
            new Dictionary<int, string>(),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            Array.Empty<string>());

    public IReadOnlyList<string> Options { get; }

    public bool TryGetName(int affixId, out string name)
        => _idToName.TryGetValue(affixId, out name!);

    public string FormatForDisplay(int affixId)
    {
        if (affixId == -1)
        {
            return "FFFFFFFF";
        }

        if (!TryGetName(affixId, out var name))
        {
            return affixId.ToString(CultureInfo.InvariantCulture);
        }

        return $"{name} ({affixId.ToString(CultureInfo.InvariantCulture)})";
    }

    public bool TryResolveToId(string? input, out int affixId)
    {
        affixId = -1;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (trimmed.Equals("FFFFFFFF", StringComparison.OrdinalIgnoreCase))
        {
            affixId = -1;
            return true;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            affixId = numeric;
            return true;
        }

        // Name-based selection resolves to the configured canonical ID (minimum ID for same-name group).
        if (_displayToId.TryGetValue(trimmed, out var fromDisplay))
        {
            affixId = fromDisplay;
            return true;
        }

        // If the user typed "Name (12345)", accept explicit ID override.
        if (TryParseTrailingIdInParens(trimmed, out var parsedFromParens))
        {
            affixId = parsedFromParens;
            return true;
        }

        return false;
    }

    private static bool TryParseTrailingIdInParens(string text, out int id)
    {
        id = -1;
        var openAscii = text.LastIndexOf('(');
        var closeAscii = text.LastIndexOf(')');
        var openWide = text.LastIndexOf('（');
        var closeWide = text.LastIndexOf('）');

        int open;
        int close;
        if (openAscii >= 0 && closeAscii > openAscii)
        {
            open = openAscii;
            close = closeAscii;
        }
        else if (openWide >= 0 && closeWide > openWide)
        {
            open = openWide;
            close = closeWide;
        }
        else
        {
            return false;
        }

        var inner = text.Substring(open + 1, close - open - 1).Trim();
        return int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
    }

    private static AffixIdTable LoadFromCsv(TextReader reader)
    {
        var idToName = new Dictionary<int, string>();

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

            if (id == 0)
            {
                // "0=备用" is intentionally hidden from the editor list.
                continue;
            }

            var name = fields[1].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (!idToName.TryAdd(id, name))
            {
                // Keep the first.
                continue;
            }
        }

        var nameToMinId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in idToName)
        {
            if (nameToMinId.TryGetValue(kv.Value, out var existing))
            {
                if (kv.Key < existing)
                {
                    nameToMinId[kv.Value] = kv.Key;
                }
            }
            else
            {
                nameToMinId[kv.Value] = kv.Key;
            }
        }

        // Lookup map:
        // 1) plain name -> minimum ID for that name group (dropdown/canonical behavior)
        // 2) "name (id)" -> exact ID (manual override behavior)
        var displayToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in nameToMinId)
        {
            displayToId[kv.Key] = kv.Value;
        }

        foreach (var kv in idToName)
        {
            var displayWithId = $"{kv.Value} ({kv.Key.ToString(CultureInfo.InvariantCulture)})";
            displayToId[displayWithId] = kv.Key;
        }

        var options = nameToMinId
            .Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AffixIdTable(idToName, displayToId, options);
    }

    // Minimal CSV parser: supports commas + quotes + escaped quotes. Good enough for two columns.
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
