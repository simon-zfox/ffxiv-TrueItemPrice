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
        if (vendorBuyPrice > 0)
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
        var vendorPrice = item.PriceLow;
        //get market price
        if (!item.IsUntradable) { }

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
                builder.Append($"stack for {vendorPrice * quantity:n0}{GilIcon}");
            }

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
}
