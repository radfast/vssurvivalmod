﻿using System;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics
{
    public class BEBehaviorWindmillRotor : BEBehaviorMPBase
    {
        WeatherSystem weatherSystem;
        double windSpeed;
        double torqueNow;

        int sailLength = 0;
        double lastMsAngle;

        BlockFacing ownFacing;

        public int SailLength => sailLength;

        public override float Angle
        {
            get
            {
                if (network?.Speed > 0 && Api.World.ElapsedMilliseconds - lastMsAngle > 500 / network.Speed)
                {
                    Api.World.PlaySoundAt(new AssetLocation("sounds/effect/swoosh"), Position.X + 0.5, Position.Y + 0.5, Position.Z + 0.5, null, false, 20, (0.5f + 0.5f * (float)windSpeed) * sailLength / 3f);
                    lastMsAngle = Api.World.ElapsedMilliseconds;
                }

                return base.Angle;
            }
        }

        public BEBehaviorWindmillRotor(BlockEntity blockentity) : base(blockentity)
        {
            string orientation = Blockentity.Block.Variant["side"];
            ownFacing = BlockFacing.FromCode(orientation);
            OutFacingForNetworkDiscovery = ownFacing.GetOpposite();

            turnDir = ownFacing == BlockFacing.WEST || ownFacing == BlockFacing.NORTH ? EnumTurnDirection.Counterclockwise : EnumTurnDirection.Clockwise;
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            switch (ownFacing.Code)
            {
                case "north":
                    AxisMapping = new int[] { 0, 1, 2 };
                    AxisSign = new int[] { -1, -1, -1 };
                    break;

                case "east":
                    AxisMapping = new int[] { 2, 1, 0 };
                    AxisSign = new int[] { -1, -1, -1 };
                    break;

                case "south":
                    AxisMapping = new int[] { 0, 1, 2 };
                    AxisSign = new int[] { -1, -1, -1 };
                    break;

                case "west":
                    AxisMapping = new int[] { 2, 1, 0 };
                    AxisSign = new int[] { -1, -1, -1 };
                    break;
            }

            weatherSystem = Api.ModLoader.GetModSystem<WeatherSystem>();
            Blockentity.RegisterGameTickListener(CheckWindSpeed, 1000);

            if (Api.Side == EnumAppSide.Client)
            {
                updateShape();
            }
        }

        private void CheckWindSpeed(float dt)
        {
            windSpeed = weatherSystem.GetWindSpeed(Blockentity.Pos);
            //windSpeed = 0.75f;

            if (Api.Side == EnumAppSide.Server && sailLength > 0 && Api.World.Rand.NextDouble() < 0.1)
            {
                if (obstructed(sailLength + 1))
                {
                    Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), Position.X + 0.5, Position.Y + 0.5, Position.Z + 0.5, null, false, 20, 1f);
                    while (sailLength-- > 0)
                    {
                        ItemStack stacks = new ItemStack(Api.World.GetItem(new AssetLocation("sail")), 4);
                        Api.World.SpawnItemEntity(stacks, Blockentity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                    sailLength = 0;
                    Blockentity.MarkDirty(true);
                }
            }
        }

        public override float GetResistance()
        {
            return torqueNow - network.Speed <= 0 ? 0.003f : 0;
        }

        public override float GetTorque()
        {
            torqueNow += (windSpeed - torqueNow) / 20.0;

            int dir = (2 * (int)GetTurnDirection(ownFacing) - 1);

            return Math.Max(0, (float)torqueNow - network.Speed) * sailLength / 3f * dir;
        }

        public override void OnBlockBroken()
        {
            while (sailLength-- > 0)
            { 
                ItemStack stacks = new ItemStack(Api.World.GetItem(new AssetLocation("sail")), 4);
                Api.World.SpawnItemEntity(stacks, Blockentity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            base.OnBlockBroken();
        }


        public override void WasPlaced(BlockFacing connectedOnFacing, IMechanicalPowerBlock connectedToBlock)
        {
            // Don't run this behavior for power producers. Its done in initialize instead
        }


        internal bool OnInteract(IPlayer byPlayer)
        {
            if (sailLength >= 3) return false;

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack sailStack = new ItemStack(Api.World.GetItem(new AssetLocation("sail")));
            if (slot.Empty || !slot.Itemstack.Equals(Api.World, sailStack)) return false;

            if (slot.StackSize < 4) return false;

            int len = sailLength + 2;

            if (obstructed(len))
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    (Api as ICoreClientAPI).TriggerIngameError(this, "notenoughspace", Lang.Get("Cannot add more sails. Make sure there's space for the sails to rotate freely"));
                }
                return false;
            }

            slot.TakeOut(4);
            sailLength++;
            updateShape();
            return true;
        }


        bool obstructed(int len)
        {
            BlockPos tmpPos = new BlockPos();

            for (int dxz = -len; dxz <= len; dxz++)
            {
                for (int dy = -len; dy <= len; dy++)
                {
                    if (dxz == 0 && dy == 0) continue;

                    int dx = ownFacing.Axis == EnumAxis.Z ? dxz : 0;
                    int dz = ownFacing.Axis == EnumAxis.X ? dxz : 0;
                    tmpPos.Set(Position.X + dx, Position.Y + dy, Position.Z + dz);

                    Block block = Api.World.BlockAccessor.GetBlock(tmpPos);
                    Cuboidf[] collBoxes = block.GetCollisionBoxes(Api.World.BlockAccessor, tmpPos);
                    if (collBoxes != null && collBoxes.Length > 0)
                    {
                        
                        return true;
                    }
                }
            }

            return false;
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            sailLength = tree.GetInt("sailLength");
            
            if (worldAccessForResolve.Side == EnumAppSide.Client && Block != null)
            {
                updateShape();
            }

            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("sailLength", sailLength);
            base.ToTreeAttributes(tree);
        }


        void updateShape()
        {
            if (sailLength == 0)
            {
                Shape = new CompositeShape()
                {
                    Base = new AssetLocation("block/wood/windmillrotor"),
                    rotateY = Block.Shape.rotateY
                };
            }
            else
            {
                Shape = new CompositeShape()
                {
                    Base = new AssetLocation("block/wood/windmill-" + sailLength + "blade"),
                    rotateY = Block.Shape.rotateY
                };
            }
        }

        protected override MechPowerPath[] GetMechPowerPaths(BlockFacing fromFacing, EnumTurnDirection turnDir)
        {
            // This is a one way road, baby
            return new MechPowerPath[0];
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);

            sb.AppendLine(string.Format("Wind speed: {0}", windSpeed));
        }
    }
}