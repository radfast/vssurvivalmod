﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class BlockSnow : Block
    {
        Cuboidf[] fullBox = new Cuboidf[] { new Cuboidf(0,0,0,1,1,1) };
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor world, BlockPos pos)
        {
            if (world.GetBlock(pos.X, pos.Y + 1, pos.Z) is BlockLayered)
            {
                return fullBox;
            }

            return base.GetCollisionBoxes(world, pos);
        }
    }
}
