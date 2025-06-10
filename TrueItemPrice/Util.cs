using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;

namespace TrueItemPrice;

public static class Util
{
    public static readonly Dictionary<byte, string> Regions;
    public static readonly Dictionary<uint, (string Name, string DataCenter, string Region)> Worlds = new();

    static Util()
    {
        //Hard-Code regions. Not gonna change
        Regions = new Dictionary<byte, string>
            { { 1, "Japan" }, { 2, "North-America" }, { 3, "Europe" }, { 4, "Oceania" } };
        //filter public & valid worlds
        var worldsRaw = Service.DataManager.GetExcelSheet<World>();
        foreach (var worldRow in worldsRaw)
        {
            if (!Regions.ContainsKey(worldRow.Region))
                continue;
            if (!worldRow.IsPublic)
                continue;
            Worlds[worldRow.RowId] = (worldRow.Name.ExtractText(), worldRow.DataCenter.Value.Name.ExtractText(),
                                          Regions[worldRow.Region]);
        }
        Service.PluginLog.Debug("Available Worlds:");
        foreach (var world in Util.Worlds)
        {
            Service.PluginLog.Debug(world.Value.ToString());
        }
    }

    public static uint GetHoverItem()
    {
        var id = Service.GameGui.HoveredItem;
        return id > 1000000 ? (uint)id - 1000000 : (uint)id;
    }

    public static bool GetHoverHQ()
    {
        var id = Service.GameGui.HoveredItem;
        return id > 1000000;
    }

    public static (uint, bool) GetHoverItemHQ()
    {
        var id = Service.GameGui.HoveredItem;
        return id > 1000000 ? ((uint)id - 1000000, true) : ((uint)id, false);
    }
}
