using NUnit.Framework;
using UnityEngine;

public class ItemSpecTests
{
	[Test]
	public void NoiseRadiiMatchTheSpec()
	{
		Assert.AreEqual(8.0f, DecoSpec.GlassNoiseTiles, 0.001f, "깨진 유리 8타일");
		Assert.AreEqual(0.5f, DecoSpec.GlassSneakScale, 0.001f, "유리 살금 -50%");
		Assert.AreEqual(7.0f, DecoSpec.PlankNoiseTiles, 0.001f, "나무 판자 7타일");
		Assert.AreEqual(5.0f, DecoSpec.NoisyFloorTiles, 0.001f, "noisy 바닥 5타일");
		Assert.AreEqual(8.0f, DecoSpec.StoneNoiseTiles, 0.001f, "돌멩이 8타일");
	}

	[Test]
	public void ConcealmentMatchesTheSpec()
	{
		Assert.AreEqual(1.0f, ConcealmentRules.RadiusScale(0), 0.001f);
		Assert.AreEqual(0.75f, ConcealmentRules.RadiusScale(1), 0.001f);
		Assert.AreEqual(0.55f, ConcealmentRules.RadiusScale(2), 0.001f);

		Assert.AreEqual(0.0f, ConcealmentRules.HoldSeconds(0), 0.001f);
		Assert.AreEqual(2.0f, ConcealmentRules.HoldSeconds(1), 0.001f);
		Assert.AreEqual(3.0f, ConcealmentRules.HoldSeconds(2), 0.001f);

		Assert.AreEqual(6.0f, ConcealmentRules.NoiseRadius(0), 0.001f);
		Assert.AreEqual(7.0f, ConcealmentRules.NoiseRadius(1), 0.001f);
		Assert.AreEqual(10.0f, ConcealmentRules.NoiseRadius(2), 0.001f);
	}

	[Test]
	public void ConcealmentLevelGatingMatchesTheSpec()
	{
		for (int index = 0; index < 4; index++)
		{
			Assert.AreEqual(0, ConcealmentRules.ForLevel(1, index), "L1 은닉도는 0 뿐");

			int two = ConcealmentRules.ForLevel(2, index);
			Assert.IsTrue(two == 0 || two == 1, $"L2 은닉도 0~1 인데 {two}");

			int three = ConcealmentRules.ForLevel(3, index);
			Assert.IsTrue(three == 1 || three == 2, $"L3 은닉도 1~2 인데 {three}");
		}
	}

	[Test]
	public void ArtifactCountsMatchTheSpec()
	{
		(int placed, int required, float radius)[] want =
		{
			(2, 1, 12.0f),
			(3, 2, 9.0f),
			(4, 3, 7.0f),
		};

		for (int level = LevelTable.MinLevel; level <= LevelTable.MaxLevel; level++)
		{
			LevelConfig config = LevelTable.Get(level);
			(int placed, int required, float radius) = want[level - 1];

			Assert.AreEqual(placed, config.ArtifactsPlaced, $"L{level} 유물 배치");
			Assert.AreEqual(required, config.ArtifactsRequired, $"L{level} 유물 필요");
			Assert.AreEqual(radius, config.ArtifactRadiusTiles, 0.001f, $"L{level} 소리 반경");
		}
	}

	[Test]
	public void ArtifactSpritesFollowConcealment()
	{
		TempleManifest.Invalidate();

		for (int index = 0; index < 4; index++)
		{
			string exposed = DecoSpec.ArtifactKey(index, 0);
			string buried = DecoSpec.ArtifactKey(index, 1);
			string deep = DecoSpec.ArtifactKey(index, 2);

			Assert.IsNotNull(TempleManifest.Catalog.Object(exposed), $"노출형 {exposed} 없음");
			Assert.IsNotNull(TempleManifest.Catalog.Object(buried), $"반매몰형 {buried} 없음");

			Assert.IsFalse(exposed.EndsWith(DecoSpec.BuriedSuffix), "은닉도 0 은 노출형");
			Assert.IsTrue(buried.EndsWith(DecoSpec.BuriedSuffix), "은닉도 1 은 반매몰형");
			Assert.IsTrue(deep.EndsWith(DecoSpec.BuriedSuffix), "은닉도 2 도 반매몰형");
		}
	}

	[Test]
	public void ExitSpritesExistInTheCatalog()
	{
		TempleManifest.Invalidate();

		Assert.IsNotNull(TempleManifest.Catalog.Object(DecoSpec.ExitLocked),
			$"잠긴 출구 {DecoSpec.ExitLocked} 없음");
		Assert.IsNotNull(TempleManifest.Catalog.Object(DecoSpec.ExitOpen),
			$"열린 출구 {DecoSpec.ExitOpen} 없음");

		TempleObject locked = TempleManifest.Catalog.Object(DecoSpec.ExitLocked);
		Assert.AreEqual(96, locked.w, "출구는 96px(3타일)이어야 한다");
		Assert.AreEqual(96, locked.h, "출구는 96px(3타일)이어야 한다");
	}

	[Test]
	public void StonesAreLevelThreeOnly()
	{
		Assert.AreEqual(0, LevelTable.Get(1).Stones);
		Assert.AreEqual(0, LevelTable.Get(2).Stones);
		Assert.AreEqual(2, LevelTable.Get(3).Stones, "돌멩이는 L3 에서 2개");
	}

	[Test]
	public void OilCanisterIsLevelThreeOnly()
	{
		Assert.AreEqual(0, LevelTable.Get(1).OilCanisters);
		Assert.AreEqual(0, LevelTable.Get(2).OilCanisters);
		Assert.AreEqual(1, LevelTable.Get(3).OilCanisters, "기름통은 L3 에만 1개");
	}
}
