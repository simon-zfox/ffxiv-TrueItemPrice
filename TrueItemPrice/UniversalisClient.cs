using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Dalamud.Networking.Http;
using Lumina.Excel.Sheets;
using System.Net.Http.Json;

namespace TrueItemPrice;

public class UniversalisClient
{
    private readonly HttpClient httpClient;
    private readonly HappyEyeballsCallback happyEyeballsCallback;

    private List<uint> marketableItemIDs = [];

    public UniversalisClient(TrueItemPricePlugin plugin)
    {
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
            marketableItemIDs = await httpClient.GetFromJsonAsync<List<uint>>($"/api/v2/marketable") ??
                                throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            Service.PluginLog.Error($"Failed to initialize! {e.Message}");
            Service.PluginLog.Error(e.ToString());
        }
    }

    public void FetchIfUnbuffered(uint itemID)
    {
        Service.PluginLog.Info($"Fetching item {itemID}");
    }

    public bool IsMarketable(uint itemID)
    {
        return marketableItemIDs.Contains(itemID);
    }

    ~UniversalisClient()
    {
        //TODO httpClient.Dispose();
    }
}
