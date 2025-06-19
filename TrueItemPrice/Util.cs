using System;
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

    public static T CalcWeightedMedianExp<T>(IEnumerable<T> values)
        where T : IComparable<T>
    {
        //chack parameters
        ArgumentNullException.ThrowIfNull(values);
        var valueArray = values.ToArray();
        if (valueArray.Length == 0)
            throw new ArgumentException("Sequence cannot be empty");

        // Create exponentially declining weights
        var weights = new double[valueArray.Length];
        for (var i = 0; i < valueArray.Length; i++)
        {
            weights[i] = Math.Exp(-0.2 * i); // Exponential decay with factor -0.5 //TODO config
        }

        // Create pairs of values and weights, then sort by values
        var pairs1 = valueArray.Zip(weights, (v, w) => new { Value = v, Weight = w });
                              

        foreach (var pair in pairs1)
        {
            Service.PluginLog.Debug($"{pair.Value} {pair.Weight}");
        }
        
        var pairs = pairs1.OrderBy(x => x.Value).ToList();

        var totalWeight = weights.Sum();
        var halfWeight = totalWeight / 2;

        // Find the value where cumulative weight sum exceeds half of total weight
        double weightSum = 0;
        foreach (var t in pairs)
        {
            weightSum += t.Weight;
            if (weightSum >= halfWeight)
                return t.Value;
        }

        return pairs.Last().Value;
    }
}
