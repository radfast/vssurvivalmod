﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockFenceGate : BlockBaseDoor
    {
        public override string GetKnobOrientation()
        {
            return GetKnobOrientation(this);
        }

        public override BlockFacing GetDirection()
        {
            string[] parts = Code.Path.Split('-');
            return BlockFacing.FromFirstLetter(parts[2]);
        }

        public string GetKnobOrientation(Block block)
        {
            string[] parts = block.Code.Path.Split('-');
            return parts[parts.Length - 1];
        }

        public bool IsOpened()
        {
            return LastCodePart(1) == "opened";
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                BlockFacing[] horVer = SuggestedHVOrientation(byPlayer, blockSel);

                string face = (horVer[0] == BlockFacing.NORTH || horVer[0] == BlockFacing.SOUTH) ? "n" : "w";

                string knobOrientation = GetSuggestedKnobOrientation(world.BlockAccessor, blockSel.Position, horVer[0]);
                AssetLocation newCode = CodeWithParts(face, "closed", knobOrientation);

                world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(newCode).BlockId, blockSel.Position);
                return true;
            }

            return false;
        }

        private string GetSuggestedKnobOrientation(IBlockAccessor ba, BlockPos pos, BlockFacing facing)
        {
            string leftOrRight = "left";

            Block nBlock1 = ba.GetBlock(pos.AddCopy(facing.GetCCW()));
            Block nBlock2 = ba.GetBlock(pos.AddCopy(facing.GetCW()));

            bool isDoor1 = IsSameDoor(nBlock1);
            bool isDoor2 = IsSameDoor(nBlock2);
            if (isDoor1 && isDoor2)
            {
                leftOrRight = "left";
            }
            else
            {
                if (isDoor1)
                {
                    leftOrRight = GetKnobOrientation(nBlock1) == "right" ? "left" : "right";
                }
                else if (isDoor2)
                {
                    leftOrRight = GetKnobOrientation(nBlock2) == "right" ? "left" : "right";
                }
            }
            return leftOrRight;
        }

        protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
        {
            AssetLocation newCode = CodeWithParts(IsOpened() ? "closed" : "opened", GetKnobOrientation());

            world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(newCode).BlockId, pos);
        }

        protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
        {
            string knob = GetKnobOrientation();
            BlockFacing dir = GetDirection();
            return knob == "right" ? pos.AddCopy(dir.GetCW()) : pos.AddCopy(dir.GetCCW());
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithPath(CodeWithoutParts(3) + "-n-closed-left"));
            return new ItemStack[] { new ItemStack(block) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithPath(CodeWithoutParts(3) + "-n-closed-left"));
            return new ItemStack(block);
        }

        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            BlockFacing nowFacing = BlockFacing.FromFirstLetter(LastCodePart(2));
            BlockFacing rotatedFacing = BlockFacing.HORIZONTALS_ANGLEORDER[(nowFacing.HorizontalAngleIndex + angle / 90) % 4];

            string part = LastCodePart(2);

            if (nowFacing.Axis != rotatedFacing.Axis)
            {
                part = (part == "n" ? "w" : "n");
            }

            return CodeWithParts(part, IsOpened() ? "opened" : "closed", GetKnobOrientation());
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }
    }
}
