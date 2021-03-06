﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent
{
    public enum EnumBlockContainerPacketId
    {
        OpenInventory = 5000,
        CloseInventory = 5001
    }


    public abstract class BlockEntityOpenableContainer : BlockEntityContainer, IBlockEntityContainer
    {
        protected GuiDialogBlockEntityInventory invDialog;

        public virtual AssetLocation OpenSound => new AssetLocation("sounds/block/chestopen");
        public virtual AssetLocation CloseSound => new AssetLocation("sounds/block/chestclose");

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            Inventory.ResolveBlocksOrItems();
        }
        

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

                return;
            }

            if (packetid == (int)EnumBlockContainerPacketId.CloseInventory)
            {
                if (player.InventoryManager != null)
                {
                    player.InventoryManager.CloseInventory(Inventory);
                }
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

            if (packetid == (int)EnumBlockContainerPacketId.OpenInventory)
            {
                if (invDialog != null)
                {
                    invDialog.TryClose();
                    invDialog?.Dispose();
                    invDialog = null;
                    return;
                }

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    string dialogTitle = reader.ReadString();
                    int cols = reader.ReadByte();

                    TreeAttribute tree = new TreeAttribute();
                    tree.FromBytes(reader);
                    Inventory.FromTreeAttributes(tree);
                    Inventory.ResolveBlocksOrItems();
                    
                    invDialog = new GuiDialogBlockEntityInventory(dialogTitle, Inventory, Pos, cols, Api as ICoreClientAPI);

                    Block block = Api.World.BlockAccessor.GetBlock(Pos);
                    string os = block.Attributes?["openSound"]?.AsString();
                    string cs = block.Attributes?["closeSound"]?.AsString();
                    AssetLocation opensound = os == null ? null : AssetLocation.Create(os, block.Code.Domain);
                    AssetLocation closesound = cs == null ? null : AssetLocation.Create(cs, block.Code.Domain);

                    invDialog.OpenSound = opensound ?? this.OpenSound;
                    invDialog.CloseSound = closesound ?? this.CloseSound;

                    invDialog.TryOpen();
                }
            }

            if (packetid == (int)EnumBlockContainerPacketId.CloseInventory)
            {
                clientWorld.Player.InventoryManager.CloseInventory(Inventory);
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            invDialog?.TryClose();
            invDialog?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            invDialog?.TryClose();
            invDialog?.Dispose();
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
        }



    }
}
