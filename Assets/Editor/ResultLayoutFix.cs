using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ResultLayoutFix
{
	const string PrefabPath = "Assets/Resources/Prefabs/UI/Popup/UI_Result.prefab";

	static readonly (string Name, float Y, float Width, float Height)[] Layout =
	{
		("ResultTitleText", 150.0f, 700.0f, 76.0f),
		("ResultDetailText", -10.0f, 700.0f, 220.0f),
		("RetryButton", -165.0f, 400.0f, 64.0f),
		("TitleButton", -245.0f, 400.0f, 64.0f),
	};

	const float PanelWidth = 760.0f;
	const float PanelHeight = 600.0f;

	[MenuItem("LampLight/Fix Result Popup Layout")]
	public static void Run()
	{
		GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

		try
		{
			int moved = 0;

			RectTransform panel = Find(root, "Panel");
			if (panel != null)
			{
				panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
				moved++;
			}

			foreach ((string name, float y, float width, float height) in Layout)
			{
				RectTransform rect = Find(root, name);
				if (rect == null)
				{
					Debug.LogError($"ResultLayoutFix: {name} 없음");
					continue;
				}

				rect.anchorMin = new Vector2(0.5f, 0.5f);
				rect.anchorMax = new Vector2(0.5f, 0.5f);
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(0.0f, y);
				rect.sizeDelta = new Vector2(width, height);
				moved++;
			}

			foreach (Text text in root.GetComponentsInChildren<Text>(true))
			{
				text.verticalOverflow = VerticalWrapMode.Overflow;
				text.horizontalOverflow = HorizontalWrapMode.Overflow;
				text.alignment = TextAnchor.MiddleCenter;
			}

			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Debug.Log($"ResultLayoutFix: {moved}개 재배치, 패널 {PanelWidth}x{PanelHeight}");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	static RectTransform Find(GameObject root, string name)
	{
		foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
		{
			if (rect.name == name)
				return rect;
		}

		return null;
	}
}
