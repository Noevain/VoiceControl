using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using Maps = Lumina.Excel.Sheets.Map;
using ContentFinderCondition = Lumina.Excel.Sheets.ContentFinderCondition;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using Dalamud.Interface.Utility;
using Lumina.Excel;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Extensions;
using Dalamud.Bindings.ImGui;
namespace VoiceDirector.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private CutsceneMovieVoiceValue language_sel = CutsceneMovieVoiceValue.English;
    public string _filter = string.Empty;
    public ContentFinderCondition _selected;
    public ExcelSheet<ContentFinderCondition> contents = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>();
    public bool _error;
    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Voice Director Config###VoiceDirectorConfig")
    {
       

        

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {

    }

    public static string GetNameFromEnum(CutsceneMovieVoiceValue csValue)
    {
        switch (csValue)
        {
            case CutsceneMovieVoiceValue.Japanese:return "Japanese";
            case CutsceneMovieVoiceValue.English:return "English";
            case CutsceneMovieVoiceValue.German:return "German";
            case CutsceneMovieVoiceValue.French:return "French";
            default:return "How did you do that";
        }
    }
    public override void Draw()
    {
        if (ImGui.BeginCombo("Default Language", GetNameFromEnum(Configuration.defaultLanguage)))
        {
            foreach (CutsceneMovieVoiceValue csVoice in Enum.GetValues(typeof(CutsceneMovieVoiceValue)))
            {
                if (ImGui.Selectable(GetNameFromEnum(csVoice), Configuration.defaultLanguage == csVoice))
                {
                    Plugin.Logger.Debug("selected:" + GetNameFromEnum(csVoice));
                    Configuration.defaultLanguage = csVoice;
                    Configuration.Save();
                }
            }
            ImGui.EndCombo();

        }
        ImGui.Separator();
        //Based on the plugin filter combo in the dalamud console
        //https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L705
        string resolvedName = _selected.RowId != 0 ? _selected.Name.ToString() : "Duty name";
        if (ImGui.BeginCombo("Duty Picker",resolvedName, ImGuiComboFlags.HeightLarge))
        {
            var sourceNames = contents.Where(c => c.Name != "")//remove empty or null entries
                              .Where(c => c.Name.ToString().IndexOf(_filter,StringComparison.OrdinalIgnoreCase) != -1)
                              .ToList();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##ContentSearchFilter", "Search duties...", ref _filter, 300);
            ImGui.Separator();

            if (!sourceNames.Any())
            {
                ImGui.Text("No matches found");
            }

            foreach (ContentFinderCondition selectable in sourceNames)
            {
                if (ImGui.Selectable(selectable.Name.ToString(),selectable.RowId == _selected.RowId))
                    {
                    _selected = selectable;

                }
            }
            ImGui.EndCombo();
        }
        
        
        if (ImGui.BeginCombo("Language picker", GetNameFromEnum(language_sel)))
        {
            foreach (CutsceneMovieVoiceValue csVoice in Enum.GetValues(typeof(CutsceneMovieVoiceValue)))
            {
                if (ImGui.Selectable(GetNameFromEnum(csVoice), csVoice == language_sel))
                    {
                        Plugin.Logger.Debug("selected:" + GetNameFromEnum(csVoice));
                    language_sel = csVoice;
                    }
            }
            ImGui.EndCombo();
        }
        if (ImGui.Button("Add changes"))
        {
            try
            {
                Dictionary<ushort, CutsceneMovieVoiceValue> rep = Configuration.replacements;
                rep.Add((ushort)_selected.Content.RowId, language_sel);
                Configuration.replacements = rep;
                Configuration.Save();
                Plugin.Logger.Debug("Added replacement for content id:{0} with language {1}", [_selected.Content.RowId, language_sel]);
                _error = false;
            }
            catch (ArgumentException e)
            {
                _error = true;
                Plugin.Logger.Debug("Tried to add replacement for content id:{0} but a key already exist:{1}", _selected,e.ToString());
            }
        }
        if (_error)
        {
            ImGui.Text("Could not add your change because a change for this duty already exist,delete the existing one");
        }
        ImGui.Separator();
        ImGui.Text("List of current changes");
        if (ImGui.BeginTable("changetable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableNextColumn();
            ImGui.Text("Maps");
            ImGui.TableNextColumn();
            ImGui.Text("Voice");
            foreach (KeyValuePair<ushort,CutsceneMovieVoiceValue> entry in Configuration.replacements)
            {
                ImGui.TableNextColumn();
                var itemCFC = contents.FirstOrNull(x => x.Content.RowId == entry.Key);
                var itemName = itemCFC?.Name.ToString() ?? "This zone is no longer in the game,You can safely delete this";
                ImGui.Text(itemName);
                ImGui.TableNextColumn();
                ImGui.Text(GetNameFromEnum(entry.Value));
                ImGui.SameLine();
                ImGui.PushID(entry.Key);
                if (ImGui.SmallButton("Remove"))
                {
                    Dictionary<ushort, CutsceneMovieVoiceValue> rep = Configuration.replacements;
                    rep.Remove(entry.Key);
                    Configuration.replacements = rep;
                    Configuration.Save();
                    var cfc = itemCFC?.Content.RowId ?? 0;
                    Plugin.Logger.Debug("Removed replacement for map id:{0} with language {1}", [cfc , entry.Value]);
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }


        }

    }
