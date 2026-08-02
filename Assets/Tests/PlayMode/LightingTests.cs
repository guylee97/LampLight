using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class LightingTests
{
	[UnityTest]
	public IEnumerator EveryTilemapReactsToLight()
	{
		yield return QaScene.Load();

		List<string> unlit = new List<string>();

		foreach (TilemapRenderer renderer in Object.FindObjectsByType<TilemapRenderer>(
			FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			Material material = renderer.sharedMaterial;

			bool wantsLight = renderer.name != "Wall";
			bool hasLight = material != null && material.shader != null
				&& material.shader.name.Contains("Lit");

			if (wantsLight && hasLight == false)
				unlit.Add($"{renderer.name}: 조명을 받아야 하는데 {(material == null ? "재질 없음" : material.shader.name)}");

			if (wantsLight == false && hasLight)
				unlit.Add($"{renderer.name}: 벽은 빛을 받으면 안 된다 ({material.shader.name})");
		}

		Assert.IsEmpty(unlit,
			"조명을 안 받는 타일맵이 있으면 등불과 무관하게 밝게 보인다:\n"
			+ string.Join("\n", unlit));
	}
}
