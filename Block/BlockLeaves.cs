﻿using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class BlockLeaves : Block
    {
        bool[] leavesWaveTileSide = new bool[6];
        RoomRegistry roomreg;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            roomreg = api.ModLoader.GetModSystem<RoomRegistry>();
        }

        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            if (VertexFlags.LeavesWindWave)
            {
                for (int tileSide = 0; tileSide < BlockFacing.ALLFACES.Length; tileSide++)
                {
                    BlockFacing facing = BlockFacing.ALLFACES[tileSide];
                    Block nBlock = world.BlockAccessor.GetBlock(pos.AddCopy(facing));

                    leavesWaveTileSide[tileSide] = !nBlock.SideSolid[BlockFacing.ALLFACES[tileSide].GetOpposite().Index] || nBlock.BlockMaterial == EnumBlockMaterial.Leaves;
                }

                bool waveoff = false;
                int groundOffset = 0;

                waveoff = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) < 14;

                //int yrain = api.World.BlockAccessor.GetRainMapHeightAt(pos);

                // This is too expensive. We need more efficient system for this
                // Small optimization: Don't need to do a room search when exposed to the rain map
                /*if (pos.Y == yrain || (pos.Y + 1 == yrain && api.World.BlockAccessor.GetBlock(pos.UpCopy()).BlockMaterial == EnumBlockMaterial.Leaves))
                {
                    waveoff = false;
                }
                else
                {

                    if (roomreg != null)
                    {
                        Room room = roomreg.GetRoomForPosition(pos);

                        waveoff = ((float)room.SkylightCount / room.NonSkylightCount) < 0.1f;
                    }
                }*/

                if (!waveoff)
                {
                    // Todo: Improve the performance of this, somehow?
                    for (; groundOffset < 7; groundOffset++)
                    {
                        Block block = api.World.BlockAccessor.GetBlock(pos.X, pos.Y - groundOffset, pos.Z);
                        if (block.BlockMaterial != EnumBlockMaterial.Leaves && block.SideSolid[BlockFacing.UP.Index])
                        {
                            break;
                        }
                    }
                }

                setLeaveWaveFlags(decalMesh, leavesWaveTileSide, waveoff, groundOffset);
            }
        }

        public override void OnJsonTesselation(ref MeshData sourceMesh, BlockPos pos, int[] chunkExtIds, ushort[] chunkLightExt, int extIndex3d)
        {
            if (VertexFlags.LeavesWindWave)
            {
                for (int tileSide = 0; tileSide < TileSideEnum.SideCount; tileSide++)
                {
                    int nBlockId = chunkExtIds[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
                    Block nblock = api.World.Blocks[nBlockId];
                    leavesWaveTileSide[tileSide] = !nblock.SideSolid[BlockFacing.ALLFACES[tileSide].GetOpposite().Index] || nblock.BlockMaterial == EnumBlockMaterial.Leaves;
                }

                int sunLightLevel = chunkLightExt[extIndex3d] & 31; 
                bool waveoff = sunLightLevel < 14;
                int groundOffset = 0;


                // This is too expensive. We need more efficient system for this
                

                /*int yrain = api.World.BlockAccessor.GetRainMapHeightAt(pos);

                // Small optimization: Don't need to do a room search when exposed to the rain map
                if (pos.Y == yrain || (pos.Y + 1 == yrain && api.World.Blocks[chunkExtIds[extIndex3d + TileSideEnum.MoveIndex[5]]].BlockMaterial == EnumBlockMaterial.Leaves))
                {
                    waveoff = false;
                }
                else
                {

                    if (roomreg != null)
                    {
                        Room room = roomreg.GetRoomForPosition(pos);

                        waveoff = ((float)room.SkylightCount / room.NonSkylightCount) < 0.1f;
                    }
                }*/

                if (!waveoff)
                {
                    // Todo: Improve the performance of this, somehow?
                    for (; groundOffset < 7; groundOffset++)
                    {
                        Block block = api.World.BlockAccessor.GetBlock(pos.X, pos.Y - groundOffset, pos.Z);
                        if (block.BlockMaterial != EnumBlockMaterial.Leaves && block.SideSolid[BlockFacing.UP.Index])
                        {
                            break;
                        }
                    }
                }

                setLeaveWaveFlags(sourceMesh, leavesWaveTileSide, waveoff, groundOffset);
            }
        }


        void setLeaveWaveFlags(MeshData sourceMesh, bool[] leavesWaveTileSide, bool off, int groundOffset)
        {
            int leaveWave = VertexFlags.LeavesWindWaveBitMask;
            int clearFlags = (~VertexFlags.LeavesWindWaveBitMask) & (~VertexFlags.GroundDistanceBitMask);

            // Iterate over each element face
            for (int vertexNum = 0; vertexNum < sourceMesh.GetVerticesCount(); vertexNum++)
            {
                float x = sourceMesh.xyz[vertexNum * 3 + 0];
                float y = sourceMesh.xyz[vertexNum * 3 + 1];
                float z = sourceMesh.xyz[vertexNum * 3 + 2];

                // Is there some pretty math formula for this? :<
                bool notwaving =
                    off ||
                    (y > 0.5 && !leavesWaveTileSide[BlockFacing.UP.Index]) ||
                    (y < 0.5 && !leavesWaveTileSide[BlockFacing.DOWN.Index]) ||
                    (z < 0.5 && !leavesWaveTileSide[BlockFacing.NORTH.Index]) ||
                    (z > 0.5 && !leavesWaveTileSide[BlockFacing.SOUTH.Index]) ||
                    (x > 0.5 && !leavesWaveTileSide[BlockFacing.EAST.Index]) ||
                    (x < 0.5 && !leavesWaveTileSide[BlockFacing.WEST.Index])
                ;

                sourceMesh.Flags[vertexNum] &= clearFlags;

                if (!notwaving)
                {
                    sourceMesh.Flags[vertexNum] |= leaveWave | (groundOffset << 28);
                }
            }
        }


        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            return offThreadRandom.NextDouble() < 0.15;
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("x", pos.X);
            tree.SetInt("y", pos.Y);
            tree.SetInt("z", pos.Z);
            world.Api.Event.PushEvent("testForDecay", tree);
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            BakedCompositeTexture tex = Textures?.First().Value?.Baked;
            int color = capi.BlockTextureAtlas.GetRandomColor(tex.TextureSubId);
            color = capi.ApplyColorTintOnRgba(1, color, pos.X, pos.Y, pos.Z);

            return color;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(world.GetBlock(CodeWithParts("placed", LastCodePart())));
        }

        public override int GetColor(ICoreClientAPI capi, BlockPos pos)
        {
            return capi.ApplyColorTintOnRgba(1, base.GetColorWithoutTint(capi, pos), pos.X, pos.Y, pos.Z, false);
        }
    }
}
