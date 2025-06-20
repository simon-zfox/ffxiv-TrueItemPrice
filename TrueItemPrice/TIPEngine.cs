using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace TrueItemPrice;

public class TIPEngine(TrueItemPricePlugin plugin)
{
    private const char HQIcon = '';
    private const char GilIcon = '';


    public ReadOnlySeString GenerateTIP(uint itemID, uint quantity, bool hq)
    {
        //get universalis data for item
        if (plugin.UniversalisClient.IsMarketable(itemID))
        {
            plugin.UniversalisClient.FetchIfUnbuffered(itemID);
        }


        var builder = new SeStringBuilder();

        var sheet = Service.DataManager.Excel.GetSheet<Item>();
        var item = sheet.GetRow(itemID);


        builder = AppendDebugInfo(builder, item, quantity, hq);
        builder.AppendNewLine();
        builder = AppendPurchaseInfo(builder, item, quantity, hq);
        builder.AppendNewLine();
        builder = AppendSaleInfo(builder, item, quantity, hq);


        return builder.ToReadOnlySeString();
    }

    private SeStringBuilder AppendDebugInfo(SeStringBuilder builder, Item item, uint quantity, bool hq)
    {
        builder.Append($"DEBUG: \nItemID:    {item.RowId}\nQuantity:    {quantity}\nHQ:    {hq}");
        return builder;
    }

    private SeStringBuilder AppendPurchaseInfo(SeStringBuilder builder, Item item, uint quantity, bool hq)
    {
        builder.Append("TIP Buy: ");
        var vendorBuyPrice = item.PriceMid;

        uint marketPrice = 0;
        /*if (plugin.UniversalisClient.IsBuffered(item.RowId))
        {
            var md = plugin.UniversalisClient.GetFromBuffer(item.RowId);
            //marketPrice = (int?)md?.Listings[0].TotalPrice ?? 0;
            marketPrice = plugin.UniversalisClient.CalculateSellPrice(item.RowId, quantity,402, hq);;
        }*/


        if (plugin.UniversalisClient.IsFetching(item.RowId))
        {
            builder.PushColorType(1); // white
            builder.Append(
                $"Fetching data from Universalis...");
            builder.PopColorType();
        }
        else if (marketPrice > 0)
        {
            builder.PushColorType(17); //red
            builder.Append(
                $"Buy this from the Marketboard for {marketPrice}{GilIcon} (TODO proper calc)");
            builder.PopColorType();
        }
        else if (vendorBuyPrice > 0)
        {
            builder.PushColorType(42); //light green
            builder.Append(
                $"Buy this from an NPC Vendor for {vendorBuyPrice:n0}{GilIcon} (TODO potentially restricted)");
            builder.PopColorType();
        }
        else
        {
            builder.PushColorType(66); //yellow/brown
            builder.Append("Unknown");
            builder.PopColorType();
        }

        return builder;
    }

    private SeStringBuilder AppendSaleInfo(SeStringBuilder builder, Item item, uint quantity, bool hq)
    {
        builder.Append("TIP Sell: ");
        var vendorPrice = item.PriceLow * quantity;

        //best to sell to market?
        if (plugin.UniversalisClient.IsMarketable(item.RowId) && plugin.UniversalisClient.IsBuffered(item.RowId))
        {
            //TODO world == home world
            var marketSellPrice = plugin.UniversalisClient.CalculateSellPrice(item.RowId, quantity, 402, hq);
            //only sell to market if vendor price is lower; do not sell extremely cheap items
            if (marketSellPrice > vendorPrice * 1.1 && marketSellPrice > 100) //TODO config cutoff price & factor
            {
                builder.PushColorType(17); //red
                builder.Append(
                    $"Sell this to the Market for {marketSellPrice:n0}{GilIcon}");
                builder.PopColorType();
                builder.PushColorType(1); // white
                var saleVelocity = plugin.UniversalisClient.getSaleVelocity(item.RowId, hq);
                builder.Append($"\nSell velocity: {saleVelocity:n0}");
                builder.PopColorType();
                //TODO warning if sale velocity is too low
                //TODO warning if sale price on home server is much higher than buy price (that is DC wide)
                return builder;
            }
        }

        //can it be vendored?
        if (vendorPrice > 0)
        {
            builder.PushColorType(42); //light green
            builder.Append("Vendor this ");
            if (quantity == 1)
            {
                builder.Append($"item for {vendorPrice:n0}{GilIcon}");
            }
            else
            {
                builder.Append($"stack for {vendorPrice:n0}{GilIcon}");
            }

            builder.PopColorType();
            return builder;
        }

        builder.PushColorType(66); //yellow/brown
        builder.Append("Unknown");
        builder.PopColorType();
        return builder;
    }
}
