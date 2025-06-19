using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Dalamud.Networking.Http;
using Lumina.Excel.Sheets;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace TrueItemPrice;

public class UniversalisClient
{
    private readonly HttpClient httpClient;
    private readonly HappyEyeballsCallback happyEyeballsCallback;
    private readonly TrueItemPricePlugin plugin;

    private List<uint> marketableItemIDs = [];
    private readonly List<uint> fetchingInProgress = [];
    private readonly Dictionary<uint, MarketData> marketDataBuffer = new();

    public UniversalisClient(TrueItemPricePlugin plugin)
    {
        this.plugin = plugin;
        //setup http client
        happyEyeballsCallback = new HappyEyeballsCallback(); //apparently this ist fast
        httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All, ConnectCallback = happyEyeballsCallback.ConnectCallback
        });

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"Dalamud/{Dalamud.Utility.Util.AssemblyVersion} TrueItemPrice/{TrueItemPricePlugin.PluginVersion} ({Environment.OSVersion})");
        httpClient.BaseAddress = new Uri("https://universalis.app");
    }

    public async void Initialize()
    {
        try
        {
            //get marketable items
            Service.PluginLog.Info("Populating marketable item list");
            marketableItemIDs = await httpClient.GetFromJsonAsync<List<uint>>($"/api/v2/marketable") ??
                                throw new InvalidOperationException();

            //debug
            Service.PluginLog.Debug($"Available worlds: {string.Join("\n", Util.Worlds)}");
            Service.PluginLog.Info($"World Alpha is 402 {Util.Worlds[402]}");
        }
        catch (Exception e)
        {
            Service.PluginLog.Error($"Failed to initialize! {e.Message}");
            Service.PluginLog.Error(e.ToString());
        }
    }

    public async void FetchIfUnbuffered(uint itemID)
    {
        try
        {
            //only fetch each item once
            if (fetchingInProgress.Contains(itemID))
                return;
            fetchingInProgress.Add(itemID);
            //only fetch items periodically
            if (marketDataBuffer.TryGetValue(itemID, out var value) &&
                value.FetchTime.AddSeconds(10) > DateTime.Now)
            {
                //TODO make this configurable
                fetchingInProgress.Remove(itemID);
                return;
            }


            Service.PluginLog.Info($"Fetching item {itemID}");
            string requestURL =
                $"/api/v2/Europe/{itemID}?listings=100&entries=200&fields=listings%2CrecentHistory%2CnqSaleVelocity%2ChqSaleVelocity%2CworldUploadTimes%2CunitsForSale%2CunitsSold";
            var marketData = await httpClient.GetFromJsonAsync<MarketData>(requestURL) ??
                             throw new InvalidOperationException();

            Service.PluginLog.Info($"Cheapest price w/o tax: {marketData.Listings[0].TotalPrice}");

            marketDataBuffer[itemID] = marketData;
            fetchingInProgress.Remove(itemID);
            plugin.ItemTooltip.Refresh(itemID);
        }
        catch (Exception e)
        {
            fetchingInProgress.Remove(itemID);
            Service.PluginLog.Error($"Failed to fetch item! {e.Message}");
            Service.PluginLog.Error(e.StackTrace ?? "");
        }
    }

    public uint CalculateSellPrice(uint itemID, uint quantity, uint worldID, bool hq)
    {
        if (!IsBuffered(itemID))
        {
            Service.PluginLog.Error($"Item {itemID} is not buffered!");
            return 0;
        }

        var filtered = marketDataBuffer[itemID].RecentHistory.Where(o => o.WorldID == worldID && o.Hq == hq);
        var unitPrices = filtered.OrderByDescending(o => o.Timestamp).Select(o => o.PricePerUnit).ToList();
        //Service.PluginLog.Debug($"UnitPrices (history) {string.Join("\n", unitPrices)}");
        var weightedMedian = Util.CalcWeightedMedianExp(unitPrices);
        Service.PluginLog.Debug($"Weighted median {weightedMedian}");
        return 1;
    }

    public bool IsMarketable(uint itemID)
    {
        return marketableItemIDs.Contains(itemID);
    }

    public bool IsBuffered(uint itemID)
    {
        return marketDataBuffer.ContainsKey(itemID);
    }

    public MarketData? GetFromBuffer(uint itemID)
    {
        return IsBuffered(itemID) ? marketDataBuffer[itemID] : null;
    }

    public bool IsFetching(uint itemID)
    {
        return fetchingInProgress.Contains(itemID);
    }

    ~UniversalisClient()
    {
        //TODO httpClient.Dispose();
    }
}
