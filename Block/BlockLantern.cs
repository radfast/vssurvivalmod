﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockLantern : Block, ITexPositionSource
    {
        public Size2i AtlasSize { get; set; }

        string curMat, curLining;
        ITexPositionSource glassTextureSource;
        ITexPositionSource tmpTextureSource;


        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode == "material") return tmpTextureSource[curMat];
                if (textureCode == "material-deco") return tmpTextureSource["deco-" + curMat];
                if (textureCode == "lining") return tmpTextureSource[curLining == "plain" ? curMat : curLining];
                if (textureCode == "glass") return glassTextureSource["material"];
                return tmpTextureSource[textureCode];
            }
        }

        public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
        {
            IPlayer player = (forEntity as EntityPlayer)?.Player;

            if (forEntity.AnimManager.IsAnimationActive("sleep", "wave", "cheer", "shrug", "cry", "nod", "facepalm", "bow", "laugh", "rage", "scythe"))
            {
                return null;
            }

            if (player?.InventoryManager?.ActiveHotbarSlot != null && !player.InventoryManager.ActiveHotbarSlot.Empty && hand == EnumHand.Left)
            { 
                ItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
                if (stack?.Collectible?.GetHeldTpIdleAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity, EnumHand.Right) != null) return null;

                if (player?.Entity?.Controls.LeftMouseDown == true && stack?.Collectible?.GetHeldTpHitAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity) != null) return null;
            }

            return hand == EnumHand.Left ? "holdinglanternlefthand" : "holdinglanternrighthand";
        }

        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
        {
            if (pos != null)
            {
                BELantern be = blockAccessor.GetBlockEntity(pos) as BELantern;
                if (be != null)
                {
                    return be.GetLightHsv();
                }
            }

            if (stack != null)
            {
                string lining = stack.Attributes.GetString("lining");

                byte[] lightHsv = new byte[] { this.LightHsv[0], this.LightHsv[1], (byte)(this.LightHsv[2] + (lining != "plain" ? 2 : 0)) };
                BELantern.setLightColor(this.LightHsv, lightHsv, stack.Attributes.GetString("glass"));

                return lightHsv;
            }

            return base.GetLightHsv(blockAccessor, pos, stack);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<string, MeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "blockLanternGuiMeshRefs", () =>
            {
                return new Dictionary<string, MeshRef>();
            });

            string material = itemstack.Attributes.GetString("material");
            string lining = itemstack.Attributes.GetString("lining");
            string glass = itemstack.Attributes.GetString("glass", "quartz");
            
            string key = material + "-" + lining + "-" + glass;
            MeshRef meshref;
            if (!meshrefs.TryGetValue(key, out meshref))
            {
                AssetLocation shapeloc = Shape.Base.Clone().WithPathPrefix("shapes/").WithPathAppendix(".json");
                Shape shape = capi.Assets.TryGet(shapeloc).ToObject<Shape>();

                MeshData mesh = GenMesh(capi, material, lining, glass, shape);
                mesh.Rgba2 = null;
                meshrefs[key] = meshref = capi.Render.UploadMesh(mesh);
            }
            
            renderinfo.ModelRef = meshref;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            object obj;
            if (capi.ObjectCache.TryGetValue("blockLanternGuiMeshRefs", out obj))
            {
                Dictionary<string, MeshRef> meshrefs = obj as Dictionary<string, MeshRef>;

                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }

                capi.ObjectCache.Remove("blockLanternGuiMeshRefs");
            }
        }
        

        public MeshData GenMesh(ICoreClientAPI capi, string material, string lining, string glassMaterial, Shape shape = null, ITesselatorAPI tesselator = null)
        {
            if (tesselator == null) tesselator = capi.Tesselator;

            tmpTextureSource = tesselator.GetTexSource(this);

            if (shape == null)
            {
                shape = capi.Assets.TryGet("shapes/" + this.Shape.Base.Path + ".json").ToObject<Shape>();
            }

            this.AtlasSize = capi.BlockTextureAtlas.Size;
            curMat = material;
            curLining = lining;

            Block glassBlock = capi.World.GetBlock(new AssetLocation("glass-" + glassMaterial));
            glassTextureSource = tesselator.GetTexSource(glassBlock);
            MeshData mesh;
            tesselator.TesselateShape("blocklantern", shape, out mesh, this, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));
            return mesh;
        }


        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (!ok) return false;

            BELantern be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BELantern;
            if (be != null)
            {
                string material = byItemStack.Attributes.GetString("material");
                string lining = byItemStack.Attributes.GetString("lining");
                string glass = byItemStack.Attributes.GetString("glass");
                be.DidPlace(material, lining, glass);

                BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.GetOpposite()) : blockSel.Position;
                double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                double dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                float angleHor = (float)Math.Atan2(dx, dz);

                float deg22dot5rad = GameMath.PIHALF / 4;
                float roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                be.MeshAngle = roundRad;
            }

            return true;
        }



        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(CodeWithParts("up")));

            BELantern be = world.BlockAccessor.GetBlockEntity(pos) as BELantern;
            if (be != null)
            {
                stack.Attributes.SetString("material", be.material);
                stack.Attributes.SetString("lining", be.lining);
                stack.Attributes.SetString("glass", be.glass);
            } else
            {
                stack.Attributes.SetString("material", "copper");
                stack.Attributes.SetString("lining", "plain");
                stack.Attributes.SetString("glass", "glass");
            }

            return stack;
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = new ItemStack[] { OnPickBlock(world, pos) };

                if (drops != null)
                {
                    for (int i = 0; i < drops.Length; i++)
                    {
                        world.SpawnItemEntity(drops[i], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    }
                }

                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
            }

            if (EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken();
                }
            }

            world.BlockAccessor.SetBlock(0, pos);
        }



        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!byPlayer.Entity.Controls.Sneak)
            {
                BELantern bel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BELantern;
                bel.Interact(byPlayer);
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string material = itemStack.Attributes.GetString("material");
            
            return Lang.GetMatching(Code?.Domain + AssetLocation.LocationSeparator + "block-" + Code?.Path + "-" + material);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string material = inSlot.Itemstack.Attributes.GetString("material");
            string lining = inSlot.Itemstack.Attributes.GetString("lining");
            string glass = inSlot.Itemstack.Attributes.GetString("glass");

            dsc.AppendLine(Lang.Get("Material: {0}", Lang.Get("material-" + material)));
            dsc.AppendLine(Lang.Get("Lining: {0}", lining == "plain" ? "-" : Lang.Get("material-" + lining)));
            dsc.AppendLine(Lang.Get("Glass: {0}", Lang.Get("glass-" + glass)));
        }


        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            BELantern be = capi.World.BlockAccessor.GetBlockEntity(pos) as BELantern;
            if (be != null)
            {
                CompositeTexture tex = null;
                if (Textures.TryGetValue(be.material, out tex)) return capi.BlockTextureAtlas.GetRandomColor(tex.Baked.TextureSubId);
            }

            return base.GetRandomColor(capi, pos, facing);
        }
    }
}
