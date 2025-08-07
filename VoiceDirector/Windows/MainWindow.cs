using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace VoiceDirector.Windows;

public class MainWindow : Window, IDisposable
{

    private Plugin Plugin;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Voice Director##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Use the settings button or type /vodir in chat to configure Voice Director");
    }
}
