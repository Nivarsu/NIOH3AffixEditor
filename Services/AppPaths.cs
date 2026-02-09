namespace Nioh3AffixEditor.Services;

public static class AppPaths
{
    public static string GetAppDataDir()
        => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nioh3AffixEditor");

    public static string GetUpdateCachePath()
        => System.IO.Path.Combine(GetAppDataDir(), "update_cache.json");

    public static string GetPresetPath()
        => System.IO.Path.Combine(GetAppDataDir(), "affix_preset.json");

    public static string GetAffixIdTablePath()
        => System.IO.Path.Combine(GetAppDataDir(), "affix_id_table.csv");

    public static string GetUnderworldSkillTablePath()
        => System.IO.Path.Combine(GetAppDataDir(), "underworld_skill_table.csv");
}
