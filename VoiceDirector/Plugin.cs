using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using VoiceDirector.Windows;
using Dalamud.Game.Config;
using Lumina.Excel.Sheets;
using Maps = Lumina.Excel.Sheets.Map;
using System;
using System.Linq;
using System.Collections.Generic;
using Lumina.Extensions;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace VoiceDirector;

//leaving this somewhere for now
//CutsceneMovieVoice possible values
// 0 = JP
// 1 = EN
// 2 = GER
// 3 = FR
// 42944967295 = Adjust to client,could just be junk/not set if adjust to client is set,but this seem to be consistent
public enum CutsceneMovieVoiceValue : ushort
{
    Japanese = 0,
    English = 1,
    German = 2,
    French = 3
}//Adjust to client being an option seems dumb just ask the user for a prefered default

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState clientState { get; private set; } = null!;

    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig {  get; private set; } = null!;
    [PluginService] internal static IPluginLog Logger { get; private set; } = null!;
    //Test
    //map id:d2fa/00
    //map name: Thok ast Thok


    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("VoiceDirector");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    //hashmap of mapids as keys and cutsceneMovieVoice enums as values
    //sounds the most straightforward,then serialiaze for persitence/sharing?
    Dictionary<ushort, CutsceneMovieVoiceValue> replacements;
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        
        CommandManager.AddHandler("/vodir", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the config window for Voice director"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        clientState.TerritoryChanged += OnZoneChange;
        replacements = Configuration.replacements;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        clientState.TerritoryChanged -= OnZoneChange;
        CommandManager.RemoveHandler("/vodir");
    }

    private void OnCommand(string command, string args)
    {

        if (command == "/vodir")
        {
            try
            {
                ConfigWindow.Toggle();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

        }     
    }

    private void OnZoneChange(uint e)//e is territoryType
    {
        //GetCurrentContentId does not get updated in time to get so have to find it in the sheets
        uint currContent;
        try
        {
            currContent = DataManager.GetExcelSheet<TerritoryType>().First(x => x.RowId == e).ContentFinderCondition.Value.Content.RowId;

        } catch(Exception ee)
        
        {
            Logger.Error("Could not find a matching TerritoryType");
            Logger.Error(ee.ToString());
            return;
        }
        
        if (replacements.ContainsKey((ushort)currContent))
        {
            Logger.Debug("Attempting config change cutscene voice to {0}, true value:{1}, for content id:{2}", [ConfigWindow.GetNameFromEnum(replacements[(ushort)currContent]), (ushort)replacements[(ushort)currContent], (ushort)currContent]);
            GameConfig.System.Set("CutsceneMovieVoice", ((ushort)replacements[(ushort)currContent]));
        }
        else if (GameConfig.System.GetUInt("CutsceneMovieVoice") != ((ushort)Configuration.defaultLanguage))
        {
            Logger.Debug("No changes found for content and language does not match default so set it back to default");
            GameConfig.System.Set("CutsceneMovieVoice",(ushort)Configuration.defaultLanguage);
        }else
        {
            Logger.Debug("No changes found but language already match default,no config changes necessary");
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
