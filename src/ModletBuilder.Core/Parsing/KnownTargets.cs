namespace ModletBuilder.Core.Parsing;

internal static class KnownTargets
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["items"]              = "items.xml",
        ["blocks"]             = "blocks.xml",
        ["recipes"]            = "recipes.xml",
        ["loot"]               = "loot.xml",
        ["entityclasses"]      = "entityclasses.xml",
        ["entitygroups"]       = "entitygroups.xml",
        ["buffs"]              = "buffs.xml",
        ["progression"]        = "progression.xml",
        ["gamestages"]         = "gamestages.xml",
        ["spawning"]           = "spawning.xml",
        ["traders"]            = "traders.xml",
        ["vehicles"]           = "vehicles.xml",
        ["item_modifiers"]     = "item_modifiers.xml",
        ["quests"]             = "quests.xml",
        ["biomes"]             = "biomes.xml",
        ["sounds"]             = "sounds.xml",
        ["materials"]          = "materials.xml",
        ["shapes"]             = "shapes.xml",
        ["qualityinfo"]        = "qualityinfo.xml",
        ["worldglobal"]        = "worldglobal.xml",
        ["weathersurvival"]    = "weathersurvival.xml",
        ["painting"]           = "painting.xml",
        ["nav_objects"]        = "nav_objects.xml",
        ["archetypes"]         = "archetypes.xml",
        ["dialogs"]            = "dialogs.xml",
        ["npc"]                = "npc.xml",
        ["challenges"]         = "challenges.xml",
        ["events"]             = "events.xml",
        ["gameevents"]         = "gameevents.xml",
        ["rwgmixer"]           = "rwgmixer.xml",
        ["utilityai"]          = "utilityai.xml",
        ["misc"]               = "misc.xml",
        ["physicsbodies"]      = "physicsbodies.xml",
        ["ui_display"]         = "ui_display.xml",
        ["music"]              = "music.xml",
        ["subtitles"]          = "subtitles.xml",
        ["dmscontent"]         = "dmscontent.xml",
        ["twitch"]             = "twitch.xml",
        ["twitch_events"]      = "twitch_events.xml",
        ["videos"]             = "videos.xml",
        ["loadingscreen"]      = "loadingscreen.xml",
        ["blockplaceholders"]  = "blockplaceholders.xml",
        ["xui_windows"]        = "XUi/windows.xml",
        ["xui_controls"]       = "XUi/controls.xml",
        ["xui_styles"]         = "XUi/styles.xml",
        ["xui_menu_windows"]   = "XUi_Menu/windows.xml",
        ["xui_menu_controls"]  = "XUi_Menu/controls.xml",
        ["xui_menu_styles"]    = "XUi_Menu/styles.xml",
        ["xui_common_controls"] = "XUi_Common/controls.xml",
        ["xui_common_styles"]  = "XUi_Common/styles.xml",
    };

    internal static bool IsKnown(string target) => Map.ContainsKey(target);

    internal static string GetFilePath(string target) => Map[target];

    internal static IReadOnlyDictionary<string, string> All => Map;
}
