using ModletBuilder.Core.Parsing;

namespace ModletBuilder.Tests;

public class KnownTargetsTests
{
    [Theory]
    [InlineData("items",             "items.xml")]
    [InlineData("blocks",            "blocks.xml")]
    [InlineData("recipes",           "recipes.xml")]
    [InlineData("loot",              "loot.xml")]
    [InlineData("entityclasses",     "entityclasses.xml")]
    [InlineData("entitygroups",      "entitygroups.xml")]
    [InlineData("buffs",             "buffs.xml")]
    [InlineData("progression",       "progression.xml")]
    [InlineData("gamestages",        "gamestages.xml")]
    [InlineData("spawning",          "spawning.xml")]
    [InlineData("traders",           "traders.xml")]
    [InlineData("vehicles",          "vehicles.xml")]
    [InlineData("item_modifiers",    "item_modifiers.xml")]
    [InlineData("quests",            "quests.xml")]
    [InlineData("biomes",            "biomes.xml")]
    [InlineData("sounds",            "sounds.xml")]
    [InlineData("materials",         "materials.xml")]
    [InlineData("shapes",            "shapes.xml")]
    [InlineData("qualityinfo",       "qualityinfo.xml")]
    [InlineData("worldglobal",       "worldglobal.xml")]
    [InlineData("weathersurvival",   "weathersurvival.xml")]
    [InlineData("painting",          "painting.xml")]
    [InlineData("nav_objects",       "nav_objects.xml")]
    [InlineData("archetypes",        "archetypes.xml")]
    [InlineData("dialogs",           "dialogs.xml")]
    [InlineData("npc",               "npc.xml")]
    [InlineData("challenges",        "challenges.xml")]
    [InlineData("events",            "events.xml")]
    [InlineData("gameevents",        "gameevents.xml")]
    [InlineData("rwgmixer",          "rwgmixer.xml")]
    [InlineData("utilityai",         "utilityai.xml")]
    [InlineData("misc",              "misc.xml")]
    [InlineData("physicsbodies",     "physicsbodies.xml")]
    [InlineData("ui_display",        "ui_display.xml")]
    [InlineData("music",             "music.xml")]
    [InlineData("subtitles",         "subtitles.xml")]
    [InlineData("dmscontent",        "dmscontent.xml")]
    [InlineData("twitch",            "twitch.xml")]
    [InlineData("twitch_events",     "twitch_events.xml")]
    [InlineData("videos",            "videos.xml")]
    [InlineData("loadingscreen",     "loadingscreen.xml")]
    [InlineData("blockplaceholders", "blockplaceholders.xml")]
    [InlineData("xui_windows",       "XUi/windows.xml")]
    [InlineData("xui_controls",      "XUi/controls.xml")]
    [InlineData("xui_styles",        "XUi/styles.xml")]
    [InlineData("xui_menu_windows",  "XUi_Menu/windows.xml")]
    [InlineData("xui_menu_controls", "XUi_Menu/controls.xml")]
    [InlineData("xui_menu_styles",   "XUi_Menu/styles.xml")]
    [InlineData("xui_common_controls", "XUi_Common/controls.xml")]
    [InlineData("xui_common_styles", "XUi_Common/styles.xml")]
    public void Known_target_resolves_to_expected_file_path(string target, string expectedPath)
    {
        Assert.True(KnownTargets.IsKnown(target), $"Target '{target}' should be known.");
        Assert.Equal(expectedPath, KnownTargets.GetFilePath(target));
    }

    [Fact]
    public void Unknown_target_is_not_known()
    {
        Assert.False(KnownTargets.IsKnown("nonexistent_target"));
    }

    [Fact]
    public void Target_lookup_is_case_sensitive()
    {
        Assert.False(KnownTargets.IsKnown("Items"));
        Assert.False(KnownTargets.IsKnown("ITEMS"));
    }
}
