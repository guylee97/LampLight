using System.Collections.Generic;
using UnityEngine;

public static class DecoSpec
{
	public const float GlassNoiseTiles = 8.0f;
	public const float GlassSneakScale = 0.5f;
	public const float PlankNoiseTiles = 7.0f;
	public const float NoisyFloorTiles = 5.0f;
	public const float StoneNoiseTiles = 8.0f;

	public const float DebrisRatio = 0.012f;
	public const float DebrisWallWeight = 0.70f;

	public const float MossRatio = 0.045f;
	public const int MossCornerWeight = 3;

	public const int PillarMinRoom = 6;
	public const int PillarWallGap = 2;
	public const int PillarPairsPerRoomMax = 2;

	public const int SarcophagusMinRoom = 6;
	public const int SarcophagusPerMapMax = 2;

	public const int DrawerPerRoomMin = 1;
	public const int DrawerPerRoomMax = 2;

	public const int GlassGapTiles = 3;
	public const int GlassStartClearance = 3;

	public const int ArtifactWallGap = 1;
	public const int WallPatternPerRoomMax = 1;

	public static int GlassCount(int level)
	{
		switch (LevelTable.Clamp(level))
		{
			case 1: return 3;
			case 2: return 4;
			default: return 5;
		}
	}

	public static int PlankCount(int level)
	{
		switch (LevelTable.Clamp(level))
		{
			case 2: return 2;
			case 3: return 3;
			default: return 0;
		}
	}

	public static bool HasPillars(int level) { return LevelTable.Clamp(level) >= 2; }
	public static bool HasPlanks(int level) { return LevelTable.Clamp(level) >= 2; }
	public static bool HasWallPattern(int level) { return LevelTable.Clamp(level) >= 2; }
	public static bool HasSarcophagus(int level) { return LevelTable.Clamp(level) >= 3; }
	public static bool HasDrawer(int level) { return LevelTable.Clamp(level) >= 3; }

	public static readonly string[] DebrisKeys =
	{
		"debris_gravel_a", "debris_gravel_b", "debris_fragments", "debris_shards",
		"debris_stone", "debris_masonry", "debris_blocks", "debris_pile",
	};

	public static readonly string[] MossKeys =
	{
		"debris_mossy",
	};

	public static readonly string[] PillarIntactKeys =
	{
		"prop_pillar_intact", "prop_pillar_column",
	};

	public static readonly string[] PillarBrokenKeys =
	{
		"prop_pillar_broken", "prop_pillar_stump",
	};

	public static readonly string[] GlassKeys = { "noise_glass" };
	public static readonly string[] PlankKeys = { "noise_planks" };

	public static readonly string[] WallPatternKeys =
	{
		"walldeco_arch", "walldeco_arch_mirror",
		"walldeco_arcade", "walldeco_arcade_mirror",
		"walldeco_hole", "walldeco_hole_mirror",
		"walldeco_chains", "walldeco_chains_mirror",
		"walldeco_sag", "walldeco_sag_mirror",
		"walldeco_plain",
	};

	public const int WallPatternSpan = 3;
	public const int WallPatternExitGap = 6;

	public static readonly string[] ArtifactKeys =
	{
		"artifact_bell", "artifact_crest", "artifact_mask", "artifact_seal",
	};

	public const string BuriedSuffix = "_buried";

	public const string ExitLocked = "door_broken";
	public const string ExitOpen = "door_open";

	public static string ArtifactKey(int index, int concealment)
	{
		if (ArtifactKeys.Length == 0)
			return null;

		string key = ArtifactKeys[Mathf.Abs(index) % ArtifactKeys.Length];
		return concealment >= 1 ? key + BuriedSuffix : key;
	}

	public const string DrawerClosed = "container_drawer_closed";
	public const string SarcophagusClosed = "container_sarcophagus_closed";

	public static readonly string[] Banned =
	{
		"large_statue_kneeling",
		"large_carpet_round_seal",
		"large_carpet",
		"walldeco_brickband",
	};

	public static bool IsBanned(string key)
	{
		foreach (string banned in Banned)
		{
			if (key == banned)
				return true;
		}

		return false;
	}

	public static List<string> Available(IReadOnlyList<string> keys)
	{
		List<string> usable = new List<string>();
		if (TempleManifest.IsReady == false)
			return usable;

		foreach (string key in keys)
		{
			if (IsBanned(key) == false && TempleManifest.Catalog.Object(key) != null)
				usable.Add(key);
		}

		return usable;
	}
}
