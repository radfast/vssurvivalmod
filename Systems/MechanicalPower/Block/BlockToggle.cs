﻿using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics
{

    public class BlockToggle : BlockMPBase
    {
        public bool IsOrientedTo(BlockFacing facing)
        {
            string dirs = LastCodePart();

            return dirs[0] == facing.Code[0] || (dirs.Length > 1 && dirs[1] == facing.Code[0]);
        }


        public override bool HasConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return IsOrientedTo(face);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            foreach (BlockFacing face in BlockFacing.HORIZONTALS)
            {
                BlockPos pos = blockSel.Position.AddCopy(face);

                IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
                if (block != null)
                {
                    if (block.HasConnectorAt(world, pos, face.GetOpposite()))
                    {
                        AssetLocation loc = new AssetLocation(FirstCodePart() + "-" + face.GetOpposite().Code[0] + face.Code[0]);
                        Block toPlaceBlock = world.GetBlock(loc);
                        if (toPlaceBlock == null)
                        {
                            loc = new AssetLocation(FirstCodePart() + "-" + face.Code[0] + face.GetOpposite().Code[0]);
                            toPlaceBlock = world.GetBlock(loc);
                        }

                        if (toPlaceBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
                        {
                            block.DidConnectAt(world, pos, face.GetOpposite());
                            WasPlaced(world, blockSel.Position, face);
                            return true;
                        }
                    }
                }
            }


            if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
            {
                WasPlaced(world, blockSel.Position, null);
                return true;
            }
            return false;
        }


        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            BEBehaviorMPToggle bemptoggle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPToggle>();
            if (bemptoggle != null && !bemptoggle.IsAttachedToBlock())
            {
                foreach (BlockFacing face in BlockFacing.HORIZONTALS)
                {
                    BlockAngledGears blockagears = world.BlockAccessor.GetBlock(pos.AddCopy(face)) as BlockAngledGears;
                    if (blockagears == null) continue;
                    if (blockagears.Facings.Contains(face.GetOpposite()) && blockagears.Facings.Length == 1)
                    {
                        world.BlockAccessor.BreakBlock(pos.AddCopy(face), null);
                    }
                }
            }

            base.OnNeighourBlockChange(world, pos, neibpos);
        }


        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            
        }
    }
}
