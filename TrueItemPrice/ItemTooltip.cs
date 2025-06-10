using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.Payloads;

namespace TrueItemPrice;

public class ItemTooltip(TrueItemPricePlugin plugin) : IDisposable
{
    private const int NodeId = 32654;
    private const int InsertNodeID = 2;
    private const int TemplateNodeID = 44;
    private const int SpaceY = 6;
    private const char HQIcon = '';
    private const char GilIcon = '';

    public int? LastItemQuantity;

    private static readonly CultureInfo FormatProvider =
        CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator == "\u2009"
            ? CultureInfo.InvariantCulture
            : CultureInfo.CurrentCulture;

    /**
     * Resets the Item tooltip to the state without this mod (hopefully)
     * Stolen from https://github.com/Kouzukii/ffxiv-priceinsight/blob/main/ItemPriceTooltip.cs
     */
    public static unsafe void RestoreToNormal(AtkUnitBase* itemTooltip)
    {
        for (var i = 0; i < itemTooltip->UldManager.NodeListCount; i++)
        {
            var n = itemTooltip->UldManager.NodeList[i];
            if (n->NodeId != NodeId || !n->IsVisible())
                continue;
            n->ToggleVisibility(false);
            var insertNode = itemTooltip->GetNodeById(InsertNodeID);
            if (insertNode == null)
                return;
            itemTooltip->WindowNode->AtkResNode.SetHeight(
                (ushort)(itemTooltip->WindowNode->AtkResNode.Height - n->Height - SpaceY));
            itemTooltip->WindowNode->Component->UldManager.RootNode->SetHeight(
                itemTooltip->WindowNode->AtkResNode.Height);
            itemTooltip->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetHeight(
                itemTooltip->WindowNode->AtkResNode.Height);
            insertNode->SetYFloat(insertNode->Y - n->Height - SpaceY);
            break;
        }
    }

    /**
     * Event handler
     */
    public unsafe void OnItemTooltip(AtkUnitBase* itemTooltip)
    {
        UpdateItemTooltip(itemTooltip);
    }

    /**
     * Actual OnTooltip EventHandler
     */
    private unsafe void UpdateItemTooltip(AtkUnitBase* itemTooltip)
    {
        //Grab the tipNode if it has been generated already
        AtkTextNode* tipNode = null;
        for (var i = 0; i < itemTooltip->UldManager.NodeListCount; i++)
        {
            var node = itemTooltip->UldManager.NodeList[i];
            if (node == null || node->NodeId != NodeId)
                continue;
            tipNode = (AtkTextNode*)node;
            break;
        }

        //Grab the node to insert the tipNode next to
        var insertNode = itemTooltip->GetNodeById(InsertNodeID);
        if (insertNode == null)
            return;

        //If no tipNode has been generated yet, create a new one
        if (tipNode == null)
        {
            //Grab a template to copy text settings from
            var templateNode = itemTooltip->GetTextNodeById(TemplateNodeID);
            if (templateNode == null)
                return;

            //Actually generate the new node
            tipNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            GenerateTIPNode(tipNode, templateNode, insertNode);
            itemTooltip->UldManager.UpdateDrawNodeList();
        }

        //Refresh the text to be displayed in the new node
        tipNode->AtkResNode.ToggleVisibility(true);
        UpdateTIPNode(tipNode);

        //Resize the tooltip. Stolen from https://github.com/Kouzukii/ffxiv-priceinsight/blob/main/ItemPriceTooltip.cs
        tipNode->ResizeNodeForCurrentText();
        tipNode->AtkResNode.SetYFloat(itemTooltip->WindowNode->AtkResNode.Height - SpaceY - 2);
        itemTooltip->WindowNode->SetHeight((ushort)(itemTooltip->WindowNode->AtkResNode.Height +
                                                    tipNode->AtkResNode.Height + SpaceY));
        itemTooltip->WindowNode->AtkResNode.SetHeight(itemTooltip->WindowNode->Height);
        itemTooltip->WindowNode->Component->UldManager.RootNode->SetHeight(itemTooltip->WindowNode->Height);
        itemTooltip->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetHeight(
            itemTooltip->WindowNode->Height);
        itemTooltip->RootNode->SetHeight(itemTooltip->WindowNode->Height);
        var remainingSpace = ImGuiHelpers.MainViewport.WorkSize.Y - itemTooltip->Y -
                             itemTooltip->GetScaledHeight(true) - 36;
        if (remainingSpace < 0)
        {
            plugin.Hooks.ItemDetailSetPositionPreservingOriginal(itemTooltip, itemTooltip->X,
                                                                 (short)(itemTooltip->Y + remainingSpace), 1);
        }

        insertNode->SetYFloat(insertNode->Y + tipNode->AtkResNode.Height + SpaceY);
    }

    /**
     * Generates the structure/layout of the new tooltip node.
     * Does not generate the content.
     * Inspired by https://github.com/Kouzukii/ffxiv-priceinsight/blob/main/ItemPriceTooltip.cs
     */
    private unsafe void GenerateTIPNode(AtkTextNode* tipNode, AtkTextNode* templateNode, AtkResNode* insertNode)
    {
        tipNode->AtkResNode.Type = NodeType.Text;
        tipNode->AtkResNode.NodeId = NodeId;
        tipNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
        tipNode->AtkResNode.X = 16;
        tipNode->AtkResNode.Width = 50;
        tipNode->AtkResNode.Color = templateNode->AtkResNode.Color;
        tipNode->TextColor = templateNode->TextColor;
        tipNode->EdgeColor = templateNode->EdgeColor;
        tipNode->LineSpacing = 18;
        tipNode->FontSize = 12;
        tipNode->TextFlags =
            (byte)((TextFlags)templateNode->TextFlags | TextFlags.MultiLine | TextFlags.AutoAdjustNodeSize);
        var prev = insertNode->PrevSiblingNode;
        tipNode->AtkResNode.ParentNode = insertNode->ParentNode;
        insertNode->PrevSiblingNode = (AtkResNode*)tipNode;
        if (prev != null)
            prev->NextSiblingNode = (AtkResNode*)tipNode;
        tipNode->AtkResNode.PrevSiblingNode = prev;
        tipNode->AtkResNode.NextSiblingNode = insertNode;
    }

    /**
     * Uptades the content of the custom node
     */
    private unsafe void UpdateTIPNode(AtkTextNode* tipNode)
    {
        //var hoveredItemID = (uint)(Service.GameGui.HoveredItem % 500000);
        var hoveredItemID = Service.GameGui.HoveredItem;
        var hq = false;
        if (hoveredItemID > 1000000)
        {
            hoveredItemID -= 1000000;
            hq = true;
        }

        var tipString = plugin.TIPEngine.GenerateTIP((uint)hoveredItemID, (uint)(LastItemQuantity ?? 1), hq);
        tipNode->SetText(tipString);
        //Service.PluginLog.Info("Item: " + hoveredItemID);
    }

    public void Refresh(uint itemID)
    {
        var hoverItem = Util.GetHoverItem();

        if (hoverItem == itemID)
        {
            Service.PluginLog.Info($"Refreshing item tooltip {hoverItem}");
        }
        Service.Framework.RunOnFrameworkThread(() =>
        {
            try
            {
                var tooltip = Service.GameGui.GetAddonByName("ItemDetail");
                unsafe
                {
                    if (tooltip == nint.Zero || !((AtkUnitBase*)tooltip)->IsVisible)
                        return;
                    RestoreToNormal((AtkUnitBase*)tooltip);
                    UpdateItemTooltip((AtkUnitBase*)tooltip);
                }
            }
            catch (Exception e)
            {
                Service.PluginLog.Error(e, "Failed to update tooltip");
            }
        });
    }

    /**
     * Again, stolen from https://github.com/Kouzukii/ffxiv-priceinsight/blob/main/ItemPriceTooltip.cs
     */
    private unsafe void Cleanup()
    {
        var atkUnitBase = (AtkUnitBase*)Service.GameGui.GetAddonByName("ItemDetail");
        if (atkUnitBase == null)
            return;

        for (var n = 0; n < atkUnitBase->UldManager.NodeListCount; n++)
        {
            var node = atkUnitBase->UldManager.NodeList[n];
            if (node == null)
                continue;
            if (node->NodeId != NodeId)
                continue;
            if (node->ParentNode != null && node->ParentNode->ChildNode == node)
                node->ParentNode->ChildNode = node->PrevSiblingNode;
            if (node->PrevSiblingNode != null)
                node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
            if (node->NextSiblingNode != null)
                node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
            atkUnitBase->UldManager.UpdateDrawNodeList();
            node->Destroy(true);
            break;
        }
    }


    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~ItemTooltip()
    {
        Cleanup();
    }
}
