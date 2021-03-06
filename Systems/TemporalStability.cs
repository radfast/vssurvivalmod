﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent
{
    public enum EnumTempStormStrength
    {
        Light, Medium, Heavy
    }

    class TemporalStormConfig
    {
        public NatFloat Frequency;
        public float StrengthIncreaseCap;
        public float StrengthIncrease;
    }

    class TemporalStormText
    {
        public string Approaching;
        public string Imminent;
        public string Waning;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    class TemporalStormRunTimeData
    {
        public bool nowStormActive;
        
        public int stormDayNotify = 99;
        public float stormGlitchStrength;
        public double stormActiveTotalDays = 0;


        public double nextStormTotalDays = 5;
        public EnumTempStormStrength nextStormStrength = 0;
        public double nextStormStrDouble;
    }

    public class SystemTemporalStability : ModSystem
    {
        IServerNetworkChannel serverChannel;
        IClientNetworkChannel clientChannel;

        SimplexNoise stabilityNoise;
        ICoreAPI api;
        ICoreServerAPI sapi;

        EntityProperties[] drifterTypes;
        bool temporalStabilityEnabled;

        Dictionary<string, TemporalStormConfig> configs;
        Dictionary<EnumTempStormStrength, TemporalStormText> texts;

        TemporalStormConfig config;
        TemporalStormRunTimeData data = new TemporalStormRunTimeData();






        string worldConfigStorminess;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            this.api = api;

            texts = new Dictionary<EnumTempStormStrength, TemporalStormText>()
            {
                { EnumTempStormStrength.Light, new TemporalStormText() { Approaching = Lang.Get("A light temporal storm is approaching"), Imminent = Lang.Get("A light temporal storm is imminent"), Waning = Lang.Get("The temporal storm seems to be waning") } },
                { EnumTempStormStrength.Medium, new TemporalStormText() { Approaching = Lang.Get("A medium temporal storm is approaching"), Imminent = Lang.Get("A medium temporal storm is imminent"), Waning = Lang.Get("The temporal storm seems to be waning") } },
                { EnumTempStormStrength.Heavy, new TemporalStormText() { Approaching = Lang.Get("A heavy temporal storm is approaching"), Imminent = Lang.Get("A heavy temporal storm is imminent"), Waning = Lang.Get("The temporal storm seems to be waning") } },
            };

            configs = new Dictionary<string, TemporalStormConfig>()
            {
                {  "veryrare", new TemporalStormConfig() {
                    Frequency = NatFloat.create(EnumDistribution.UNIFORM, 30, 5),
                    StrengthIncrease = 2.5f/100,
                    StrengthIncreaseCap = 25f/100
                } },

                {  "rare", new TemporalStormConfig() {
                    Frequency = NatFloat.create(EnumDistribution.UNIFORM, 25, 5),
                    StrengthIncrease = 5f/100,
                    StrengthIncreaseCap = 50f/100
                } },

                {  "sometimes", new TemporalStormConfig() {
                    Frequency = NatFloat.create(EnumDistribution.UNIFORM, 15, 5),
                    StrengthIncrease = 10f/100,
                    StrengthIncreaseCap = 100f/100
                } },

                {  "often", new TemporalStormConfig() {
                    Frequency = NatFloat.create(EnumDistribution.UNIFORM, 7.5f, 2.5f),
                    StrengthIncrease = 15f/100,
                    StrengthIncreaseCap = 150f/100
                } },

                {  "veryoften", new TemporalStormConfig() {
                    Frequency = NatFloat.create(EnumDistribution.UNIFORM, 4.5f, 1.5f),
                    StrengthIncrease = 20f/100,
                    StrengthIncreaseCap = 200f/100
                } }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            api.Event.BlockTexturesLoaded += LoadNoise;


            clientChannel =
                api.Network.RegisterChannel("temporalstability")
                .RegisterMessageType(typeof(TemporalStormRunTimeData))
                .SetMessageHandler<TemporalStormRunTimeData>(onServerData)
            ;
        }

        private void onServerData(TemporalStormRunTimeData data)
        {
            this.data = data;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.sapi = api;

            serverChannel =
               api.Network.RegisterChannel("temporalstability")
               .RegisterMessageType(typeof(TemporalStormRunTimeData))
            ;

            api.Event.SaveGameLoaded += () =>
            {
                bool prepNextStorm = sapi.WorldManager.SaveGame.IsNew;

                // Init old saves
                if (!sapi.World.Config.HasAttribute("temmporalStability"))
                {
                    string playstyle = sapi.WorldManager.SaveGame.PlayStyle;
                    if (playstyle == "surviveandbuild" || playstyle == "wildernesssurvival")
                    {
                        sapi.WorldManager.SaveGame.WorldConfiguration.SetBool("temmporalStability", true);
                    }
                }

                if (!sapi.World.Config.HasAttribute("temporalStorms"))
                {
                    string playstyle = sapi.WorldManager.SaveGame.PlayStyle;
                    if (playstyle == "surviveandbuild" || playstyle == "wildernesssurvival")
                    {
                        sapi.WorldManager.SaveGame.WorldConfiguration.SetString("temporalStorms", playstyle == "surviveandbuild" ? "sometimes" : "often");
                    }
                }

                byte[] bytedata = sapi.WorldManager.SaveGame.GetData("temporalStormData");
                if (bytedata != null)
                {
                    try
                    {
                        data = SerializerUtil.Deserialize<TemporalStormRunTimeData>(bytedata);
                    }
                    catch (Exception e)
                    {
                        api.World.Logger.Notification("Failed loading temporal storm data, will initialize new data set");
                        data = new TemporalStormRunTimeData();
                        prepNextStorm = true;
                    }
                } else
                {
                    data = new TemporalStormRunTimeData();
                    prepNextStorm = true;
                }

                LoadNoise();

                if (prepNextStorm)
                {
                    prepareNextStorm();
                }
            };


            api.Event.GameWorldSave += Event_GameWorldSave;
            api.Event.PlayerJoin += Event_PlayerJoin;
            api.Event.PlayerNowPlaying += Event_PlayerNowPlaying;
            api.Event.RegisterGameTickListener(onTempStormTick, 2000);
        }

        private void Event_PlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (sapi.WorldManager.SaveGame.IsNew)
            {
                double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("{0} days until the first temporal storm.", (int)nextStormDaysLeft), EnumChatType.Notification);
            }
        }

        private void Event_PlayerJoin(IServerPlayer byPlayer)
        {
            byPlayer.Entity.OnCanSpawnNearby = (type, spawnPos, sc) =>
            {
                return CanSpawnNearby(byPlayer, type, spawnPos, sc);
            };

            serverChannel.SendPacket(data, byPlayer);
        }

        private void Event_GameWorldSave()
        {
            sapi.WorldManager.SaveGame.StoreData("temporalStormData", SerializerUtil.Serialize(data));
        }

        private void onTempStormTick(float dt)
        {
            if (config == null) return;

            double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;

            if (nextStormDaysLeft > 0.03 && nextStormDaysLeft < 0.35 && data.stormDayNotify > 1)
            {
                data.stormDayNotify = 1;
                sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Approaching, EnumChatType.Notification);
            }

            if (nextStormDaysLeft <= 0.02 && data.stormDayNotify > 0)
            {
                data.stormDayNotify = 0;
                sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Imminent, EnumChatType.Notification);
            }

            if (nextStormDaysLeft <= 0)
            {
                double stormActiveDays = 0.1f + data.nextStormStrDouble * 0.1f;

                // Happens when time is fast forwarded
                if (!data.nowStormActive && nextStormDaysLeft + stormActiveDays < 0)
                {
                    prepareNextStorm();
                    serverChannel.BroadcastPacket(data);
                    return;
                }
                
                if (!data.nowStormActive)
                {
                    data.stormActiveTotalDays = api.World.Calendar.TotalDays + stormActiveDays;
                    data.stormGlitchStrength = 0.58f;
                    if (data.nextStormStrength == EnumTempStormStrength.Medium) data.stormGlitchStrength = 0.7f;
                    if (data.nextStormStrength == EnumTempStormStrength.Heavy) data.stormGlitchStrength = 1f;
                    data.nowStormActive = true;

                    serverChannel.BroadcastPacket(data);
                }

                double activeDaysLeft = data.stormActiveTotalDays - api.World.Calendar.TotalDays;
                if (activeDaysLeft < 0.03 && data.stormDayNotify == 0)
                {
                    data.stormDayNotify = -1;
                    sapi.BroadcastMessageToAllGroups(texts[data.nextStormStrength].Waning, EnumChatType.Notification);
                }

                if (activeDaysLeft < 0)
                {
                    data.stormGlitchStrength = 0;
                    data.nowStormActive = false;
                    data.stormDayNotify = 99;
                    prepareNextStorm();

                    serverChannel.BroadcastPacket(data);
                }
            }
        }


        private void prepareNextStorm()
        {
            if (config == null) return;

            double addStrength = Math.Min(config.StrengthIncreaseCap, config.StrengthIncrease * api.World.Calendar.TotalDays / config.Frequency.avg);

            data.nextStormTotalDays = api.World.Calendar.TotalDays + config.Frequency.nextFloat(1, api.World.Rand) / (1 + addStrength/3);

            double stormStrength = addStrength + (api.World.Rand.NextDouble() * api.World.Rand.NextDouble()) * (float)addStrength * 5f;

            int index = (int)Math.Min(2, stormStrength);
            data.nextStormStrength = (EnumTempStormStrength)index;

            data.nextStormStrDouble = Math.Max(0, addStrength);
        }

        private bool Event_OnTrySpawnEntity(ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            if (!properties.Code.Path.StartsWithFast("drifter")) return true;

            IPlayer plr = api.World.NearestPlayer(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);
            double stab = plr.Entity.WatchedAttributes.GetDouble("temporalStability", 1);

            stab = Math.Min(stab, 1 - data.stormGlitchStrength);

            if (stab < 0.25f)
            {
                int index = -1;
                for (int i = 0; i < drifterTypes.Length; i++)
                {
                    if (drifterTypes[i].Code.Equals(properties.Code))
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1) return true;

                int hardnessIncrease = (int)Math.Round((0.25f - stab) * 13);

                int newIndex = Math.Min(index + hardnessIncrease, drifterTypes.Length - 1);
                properties = drifterTypes[newIndex];
            }

            return true;
        }

        private void LoadNoise()
        {
            if (api.Side == EnumAppSide.Server) updateOldWorlds();

            temporalStabilityEnabled = api.World.Config.GetBool("temporalStability", true);
            if (!temporalStabilityEnabled) return;


            stabilityNoise = SimplexNoise.FromDefaultOctaves(4, 0.1, 0.9, api.World.Seed);


            if (api.Side == EnumAppSide.Server)
            {
                worldConfigStorminess = api.World.Config.GetString("temporalStorms");

                if (worldConfigStorminess != null && configs.ContainsKey(worldConfigStorminess))
                {
                    config = configs[worldConfigStorminess];
                } else 
                {
                    string playstyle = sapi.WorldManager.SaveGame.PlayStyle;
                    if (playstyle == "surviveandbuild" || playstyle == "wildernesssurvival")
                    {
                        config = configs["rare"];
                    } else
                    {
                        config = null;
                    }
                }

                sapi.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
                sapi.Event.OnEntityDeath += Event_OnEntityDeath;
                

                drifterTypes = new EntityProperties[]
                {
                    sapi.World.GetEntityType(new AssetLocation("drifter-normal")),
                    sapi.World.GetEntityType(new AssetLocation("drifter-deep")),
                    sapi.World.GetEntityType(new AssetLocation("drifter-tainted")),
                    sapi.World.GetEntityType(new AssetLocation("drifter-corrupt")),
                    sapi.World.GetEntityType(new AssetLocation("drifter-nightmare"))
                };
            }
        }


        float curStormGlitchStrength;

        internal float GetGlitchEffectExtraStrength()
        {
            return data.stormGlitchStrength;
        }

        private void Event_OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (damageSource?.SourceEntity == null || !damageSource.SourceEntity.WatchedAttributes.HasAttribute("temporalStability")) return;
            if (entity.Properties.Attributes == null) return;

            float stabrecovery = entity.Properties.Attributes["onDeathStabilityRecovery"].AsFloat(0);
            double ownstab = damageSource.SourceEntity.WatchedAttributes.GetDouble("temporalStability", 1);
            damageSource.SourceEntity.WatchedAttributes.SetDouble("temporalStability", Math.Min(1, ownstab + stabrecovery));
        }

        public float GetTemporalStability(BlockPos pos)
        {
            return GetTemporalStability(pos.X, pos.Y, pos.Z);
        }

        public float GetTemporalStability(Vec3d pos)
        {
            return GetTemporalStability(pos.X, pos.Y, pos.Z);
        }


        public bool CanSpawnNearby(IPlayer byPlayer, EntityProperties type, Vec3d spawnPosition, RuntimeSpawnConditions sc)
        {
            // Moved from EntitySpawner to here. Make drifters spawn at any light level if temporally unstable. A bit of an ugly hack, i know
            int lightLevel = api.World.BlockAccessor.GetLightLevel((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z, sc.LightLevelType);

            if (api.World.Config.GetBool("temporalStability", true) && type.Attributes?["spawnCloserDuringLowStability"].AsBool() == true)
            {
                // Below 25% begin reducing range
                double mod = Math.Min(1, 4 * byPlayer.Entity.WatchedAttributes.GetDouble("temporalStability", 1));

                mod = Math.Min(mod, Math.Max(0, 1 - 2 * data.stormGlitchStrength));

                float minl = GameMath.Mix(0, sc.MinLightLevel, (float)mod);
                float maxl = GameMath.Mix(32, sc.MaxLightLevel, (float)mod);
                if (minl > lightLevel || maxl < lightLevel)
                {
                    return false;
                }

                double sqdist = byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition);

                // Force a maximum distance
                if (mod < 0.5)
                {
                    return sqdist < 10 * 10;
                }

                // Force a minimum distance
                return sqdist > sc.MinDistanceToPlayer * sc.MinDistanceToPlayer * mod;
            }

            if (sc.MinLightLevel > lightLevel || sc.MaxLightLevel < lightLevel) return false;

            return byPlayer.Entity.ServerPos.SquareDistanceTo(spawnPosition) > sc.MinDistanceToPlayer * sc.MinDistanceToPlayer;
        }

        public float GetTemporalStability(double x, double y, double z)
        {
            if (!temporalStabilityEnabled) return 2;

            float noiseval = (float)GameMath.Clamp(stabilityNoise.Noise(x/80, y/80, z/80)*1.2f + 0.1f, -1f, 2f);

            float sealLevelDistance = (float)(TerraGenConfig.seaLevel - y);

            // The deeper you go, the more the stability varies. Surface 100% to 80%. Deep below down 100% to 0%
            float surfacenoiseval = GameMath.Clamp(1.6f + noiseval, 0.8f, 1.5f);

            float l = (float)GameMath.Clamp(Math.Pow(Math.Max(0, (float)y) / TerraGenConfig.seaLevel, 2), 0, 1);
            noiseval = GameMath.Mix(noiseval, surfacenoiseval, l);

            // The deeper you go, the lower the stability. Up to -25% stability
            noiseval -= Math.Max(0, sealLevelDistance / api.World.BlockAccessor.MapSizeY) / 3.5f;

            noiseval = GameMath.Clamp(noiseval, 0, 1.5f);

            return GameMath.Clamp(noiseval - GetGlitchEffectExtraStrength(), 0, 1.5f);
        }




        private void updateOldWorlds()
        {
            // Pre v1.12 worlds

            if (!api.World.Config.HasAttribute("temporalStorms"))
            {
                if (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival")
                {
                    api.World.Config.SetString("temporalStorms", "often");
                }
                if (sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild")
                {
                    api.World.Config.SetString("temporalStorms", "rare");
                }
            }

            if (!api.World.Config.HasAttribute("temporalStability"))
            {
                if (sapi.WorldManager.SaveGame.PlayStyle == "wildernesssurvival" || sapi.WorldManager.SaveGame.PlayStyle == "surviveandbuild")
                {
                    api.World.Config.SetBool("temporalStability", true);
                }
            }
        }
    }
}
