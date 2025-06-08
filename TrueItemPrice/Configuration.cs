using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TrueItemPrice;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    [NonSerialized]
    private IDalamudPluginInterface pluginInterface = null!;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }

    public static Configuration Get(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.pluginInterface = pluginInterface;
        //config.Migrate();
        return config;
    }
}
