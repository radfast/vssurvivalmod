﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Vintage Story Default Server Mods")]
[assembly: AssemblyDescription("www.vintagestory.at")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Tyron Madlener (Anego Studios)")]
[assembly: AssemblyProduct("Vintage Story")]
[assembly: AssemblyCopyright(GameVersion.CopyRight)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7d22278b-7ffc-403a-92d0-fd87c7609912")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(GameVersion.AssemblyVersion)]
[assembly: AssemblyFileVersion(GameVersion.OverallVersion)]
[assembly: InternalsVisibleTo("VSSurvivalModTests")]

[assembly: ModInfo("Survival Mode", "survival",
    Version = GameVersion.ShortGameVersion,
    NetworkVersion = GameVersion.NetworkVersion,
    Description = "The Vintage Story Survival experience. Contains all standard Blocks, Items, Creatures and pretty world generation",
    Authors = new[] { "Tyron" },
    RequiredOnClient = true,
    WorldConfig = @"
    {
	    playstyles: [
		    {
			    code: ""surviveandbuild"",
                langcode: ""preset-surviveandbuild"",
			    requestMods: [""game"", ""survival""],
                listOrder: 5,
			    worldType: ""standard"",
			    worldConfig: {
				    worldClimate: ""realistic"",
				    gameMode: ""survival"",
                    temporalStability: ""true"",
                    temporalStorms: ""sometimes"",
                    graceTimer: ""0"",
			    }
		    },
		    {
			    code: ""exploration"",
                langcode: ""preset-exploration"",
			    requestMods: [""game"", ""survival""],
                listOrder: 6,
			    worldType: ""standard"",
			    worldConfig: {
				    worldClimate: ""realistic"",
				    gameMode: ""survival"",
                    microblockChiseling: ""true"",
                    deathPunishment: ""keep"",
                    graceTimer: ""5"",
                    creatureHostility: ""passive"",
                    playerHealthPoints: ""20"",
                    playerHungerSpeed: ""0.5"",
                    foodSpoilSpeed: ""0.5"",
                    toolDurability: ""2"",
                    saplingGrowthDays: ""5"",
                    playerMoveSpeed: ""1.25"",
                    temporalStability: ""false"",
                    temporalStorms: ""off"",
                    surfaceCopperDeposits: ""0.2"",
                    surfaceTinDeposits: ""0.03"",
                }
			},
            {
                code: ""wildernesssurvival"",
                langcode: ""preset-wildernesssurvival"",
			    requestMods: [""game"", ""survival""],
                listOrder: 7,
			    worldType: ""standard"",
			    worldConfig: {
				    worldClimate: ""realistic"",
				    gameMode: ""survival"",
                    microblockChiseling: ""false"",
                    deathPunishment: ""drop"",
                    blockGravity: ""sandgravelsoil"",
                    creatureHostility: ""aggressive"",
                    playerHealthPoints: ""10"",
                    creatureStrength: ""1.5"",
                    playerHungerSpeed: ""1.25"",
                    foodSpoilSpeed: ""1.25"",
                    graceTimer: ""0"",
                    allowCoordinateHud: ""false"",
                    allowMap: ""false"",
                    allowLandClaiming: ""false"",
                    surfaceCopperDeposits: ""0.05"",
                    surfaceTinDeposits: ""0"",
                    saplingGrowthDays: ""24"",
                    temporalStorms: ""often""
			    }
		    }
	    ],
	    worldConfigAttributes: [
            { code: ""gameMode"", dataType: ""dropdown"", values: [""survival"", ""creative""], names: [""Survival"", ""Creative""], default: ""survival"" },
            { code: ""worldClimate"", dataType: ""dropdown"", values: [""realistic"", ""patchy""], names: [""Realistic"", ""Patchy""], default: ""realistic"" },
            { code: ""worldWidth"", dataType: ""dropdown"", values: [""1024000"", ""600000"", ""512000"", ""102400"", ""51200"", ""10240"", ""5120"", ""1024"", ""512"", ""384"", ""256"", ""128"", ""64"", ""32"" ], names: [""1 mil blocks"", ""600k blocks"", ""512k blocks"", ""102k blocks"", ""51k blocks"", ""10k blocks"", ""5120 blocks"", ""1024 blocks"", ""512 blocks"", ""384 blocks"", ""256 blocks"", ""128 blocks"", ""64 blocks"", ""32 blocks""], default: ""1024000"" },
            { code: ""worldLength"", dataType: ""dropdown"", values: [""1024000"", ""600000"", ""512000"", ""102400"", ""51200"", ""10240"", ""5120"", ""1024"", ""512"", ""384"", ""256"", ""128"", ""64"", ""32"" ], names: [""1 mil blocks"", ""600k blocks"", ""512k blocks"", ""102k blocks"", ""51k blocks"", ""10k blocks"", ""5120 blocks"", ""1024 blocks"", ""512 blocks"", ""384 blocks"", ""256 blocks"", ""128 blocks"", ""64 blocks"", ""32 blocks""], default: ""1024000"" },
            { code: ""worldEdge"", dataType: ""dropdown"", values: [""blocked"", ""traversable"" ], names: [""Blocked"", ""Traversable (Can fall down)""], default: ""traversable"" },

            { code: ""globalTemperature"", dataType: ""dropdown"", values: [""4"", ""2"", ""1.5"", ""1"", ""0.5"", ""0.25"", ""0.15""], names: [""Scorching hot"", ""Very hot"", ""Hot"", ""Normal"", ""Cold"", ""Very Cold"", ""Snowball earth""], default: ""1"" },
            { code: ""globalPrecipitation"", dataType: ""dropdown"", values: [""4"", ""2"", ""1.5"", ""1"", ""0.5"", ""0.25"", ""0.1""], names: [""Super humid"", ""Very humid"", ""Humid"", ""Normal"", ""Semi-Arid"", ""Arid"", ""Hyperarid""], default: ""1"" },
            
            { code: ""microblockChiseling"", dataType: ""bool"", default: ""false"" },
            { code: ""deathPunishment"", dataType: ""dropdown"", values: [""drop"", ""keep""], names: [""Drop inventory contents"", ""Keep inventory contents""], default: ""drop"" },
            { code: ""spawnRadius"", onCustomizeScreen: false, dataType: ""dropdown"", values: [""10000"", ""5000"", ""2500"", ""1000"", ""500"", ""250"", ""100"", ""50"", ""25"", ""0""], names: [""10000 blocks"", ""5000 blocks"", ""2500 blocks"", ""1000 blocks"", ""500 blocks"", ""250 blocks"", ""100 blocks"", ""50 blocks"", ""25 blocks"",""0 blocks""], default: ""50"" },
            { code: ""graceTimer"", dataType: ""dropdown"", values: [""10"", ""5"", ""4"", ""3"", ""2"", ""1"", ""0""], names: [""10 days before monsters appear"", ""5 days before monsters appear"", ""4 days before monsters appear"", ""3 days before monsters appear"", ""2 days before monsters appear"", ""1 day before monsters appear"", ""No timer. Monsters spawn right away.""], default: ""0"" },
            { code: ""creatureHostility"", dataType: ""dropdown"", values: [""aggressive"", ""passive"", ""off""], names: [""Aggressive"", ""Passive"", ""Never hostile""], default: ""aggressive"" },
            { code: ""creatureStrength"", dataType: ""dropdown"", values: [""4"", ""2"", ""1.5"", ""1"", ""0.5"", ""0.25""], names: [""Deadly (400%)"", ""Very Strong (200%)"", ""Strong (150%)"", ""Normal (100%)"", ""Weak (50%)"", ""Very weak (25%)""], default: ""1"" },
            { code: ""playerHealthPoints"", dataType: ""dropdown"", values: [""5"", ""10"", ""15"", ""20"", ""25"", ""30"", ""35""], names: [""5 hp"", ""10 hp"", ""15 hp"", ""20 hp"", ""25 hp"", ""30 hp"", ""35 hp""], default: ""15"" },
            { code: ""playerHungerSpeed"", dataType: ""dropdown"", values: [""2"", ""1.5"", ""1.25"", ""1"", ""0.75"", ""0.5"", ""0.25""], names: [""Very fast (200%)"", ""Fast (150%)"", ""Slightly faster (125%)"", ""Normal (100%)"", ""Slightly slower (75%)"", ""Slower (50%)"", ""Much slower (25%)""], default: ""1"" },
            { code: ""playerMoveSpeed"", dataType: ""dropdown"", values: [""2"", ""1.75"", ""1.5"", ""1.25"", ""1"", ""0.75""], names: [""Fast"", ""Slightly faster"", ""Normal"", ""Slightly slower"", ""Slower"", ""Much slower""], default: ""1.5"" },

            { code: ""blockGravity"", dataType: ""dropdown"", values: [""sandgravel"", ""sandgravelsoil""], names: [""Sand and gravel"", ""Sand, gravel and soil with sideways instability""], default: ""sandgravel"" },

            { code: ""foodSpoilSpeed"", dataType: ""dropdown"", values: [""4"", ""3"", ""2"", ""1.5"", ""1.25"", ""1"", ""0.75"", ""0.5"", ""0.25""], names: [""400%"", ""300%"", ""200%"", ""150%"", ""125%"", ""100%"", ""75%"", ""50%"", ""25%""], default: ""1"" },
            { code: ""saplingGrowthDays"", dataType: ""dropdown"", values: [""1.5"", ""3"", ""5"", ""6.5"", ""8"", ""10"", ""12"", ""24"", ""48"", ""96""], names: [""1.5 days"", ""3 days"", ""5 days"", ""6.5 days"", ""8 days"", ""10 days"", ""12 days"", ""24 days"", ""48 days"", ""96 days""], default: ""8"" },
            { code: ""toolDurability"", dataType: ""dropdown"", values: [""4"", ""3"", ""2"", ""1.5"", ""1.25"", ""1"", ""0.75"", ""0.5""], names: [""400%"", ""300%"", ""200%"", ""150%"", ""125%"", ""100%"", ""75%"", ""50%""], default: ""1"" },
            { code: ""toolMiningSpeed"", dataType: ""dropdown"", values: [""3"", ""2"", ""1.5"", ""1.25"", ""1"", ""0.75"", ""0.5"", ""0.25""], names: [""300%"", ""200%"", ""150%"", ""125%"", ""100%"", ""75%"", ""50%"", ""25%""], default: ""1"" },

            { code: ""allowCoordinateHud"", dataType: ""bool"", default: ""true"" },
            { code: ""allowMap"", dataType: ""bool"", default: ""true"" },
            { code: ""allowLandClaiming"", dataType: ""bool"", default: ""true"" },
            { code: ""temporalStability"", dataType: ""bool"", default: ""true"" },
            { code: ""temporalStorms"", dataType: ""dropdown"", values: [""off"", ""veryrare"", ""rare"", ""sometimes"", ""often"", ""veryoften""], names: [""Off"", ""Every 30-40 days, increase strength/frequency by 2.5% each time, capped at +25%"", ""Approx. every 20-30 days, increase strength/frequency by 5% each time, capped at +50%"", ""Approx. every 10-20 days, increase strength/frequency by +10% each time, capped at 100%"", ""Approx. every 5-10 days, increase strength/frequency by 15% each time, capped at +150%"", ""Approx. every 3-6 days, increase strength/frequency by 20% each time, capped at +200%""], default: ""sometimes"" },
            

            { code: ""surfaceCopperDeposits"", dataType: ""dropdown"", values: [""1"", ""0.5"", ""0.2"", ""0.12"", ""0.05"", ""0.015"", ""0""], names: [""Very common"", ""Common"", ""Uncommon"", ""Rare"", ""Very Rare"", ""Extremly rare"", ""Never""], default: ""0.12"" },
            { code: ""surfaceTinDeposits"", dataType: ""dropdown"", values: [""0.5"", ""0.25"", ""0.12"", ""0.03"", ""0.014"", ""0.007"", ""0""], names: [""Very common"", ""Common"", ""Uncommon"", ""Rare"", ""Very Rare"", ""Extremly rare"", ""Never""], default: ""0.007"" },

	    ]
    }"
)]

[assembly: ModDependency("game")]
