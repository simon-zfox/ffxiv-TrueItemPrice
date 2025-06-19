using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TrueItemPrice;

public class MarketData
{
    [JsonPropertyName("listings")]
    public required List<MarketDataListing> Listings { get; init; }

    [JsonPropertyName("recentHistory")]
    public required List<MarketDataHistoryEntry> RecentHistory { get; init; }

    [JsonPropertyName("nqSaleVelocity")]
    public required double NqSaleVelocity { get; init; }

    [JsonPropertyName("hqSaleVelocity")]
    public required double HqSaleVelocity { get; init; }

    [JsonPropertyName("worldUploadTimes")]
    public required Dictionary<uint, long> WorldUploadTimes { get; init; }

    [JsonPropertyName("unitsForSale")]
    public required uint UnitsForSale { get; init; }

    [JsonPropertyName("unitsSold")]
    public required uint UnitsSold { get; init; }

    public readonly DateTime FetchTime = DateTime.Now;
}

public class MarketDataListing
{
    [JsonPropertyName("lastReviewTime")]
    public required long LastReviewTime { get; init; }

    [JsonPropertyName("pricePerUnit")]
    public required uint PricePerUnit { get; init; }

    [JsonPropertyName("quantity")]
    public required long Quantity { get; init; }

    [JsonPropertyName("worldID")]
    public required uint WorldID { get; init; }

    [JsonPropertyName("hq")]
    public required bool Hq { get; init; }

    [JsonPropertyName("total")]
    public required long TotalPrice { get; init; }

    [JsonPropertyName("tax")]
    public required long Tax { get; init; }
}

public class MarketDataHistoryEntry
{
    [JsonPropertyName("hq")]
    public required bool Hq { get; init; }
    [JsonPropertyName("pricePerUnit")]
    public required uint PricePerUnit { get; init; }
    [JsonPropertyName("quantity")]
    public required long Quantity { get; init; }
    [JsonPropertyName("worldID")]
    public required uint WorldID { get; init; }
    [JsonPropertyName("total")]
    public required long TotalPrice { get; init; }
    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; init; }
}
