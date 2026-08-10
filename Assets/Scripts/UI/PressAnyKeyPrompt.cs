using UnityEngine;
using UnityEngine.UI;

public class PressAnyKeyPrompt : MonoBehaviour
{
	public const string PressAnyKeyArt = "Art/UI/Common/press_any_key";
	public const string EscTitleArt = "Art/UI/Common/esc_title";

	const float BlinkSeconds = 1.15f;
	const float MinAlpha = 0.22f;

	Graphic _graphic;

	public static PressAnyKeyPrompt Attach(
		Transform parent,
		string resource,
		float anchorY,
		float width)
	{
		GameObject go = new GameObject("Prompt", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, anchorY);
		rect.anchorMax = new Vector2(0.5f, anchorY);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;

		Image image = go.GetComponent<Image>();
		image.sprite = Resources.Load<Sprite>(resource);
		image.raycastTarget = false;
		image.preserveAspect = true;

		if (image.sprite == null)
		{
			Debug.LogWarning($"PressAnyKeyPrompt: Resources/{resource} 로드 실패");
			rect.sizeDelta = new Vector2(width, width * 0.125f);
		}
		else
		{
			float aspect = image.sprite.rect.height / Mathf.Max(1.0f, image.sprite.rect.width);
			rect.sizeDelta = new Vector2(width, width * aspect);
		}

		PressAnyKeyPrompt prompt = go.AddComponent<PressAnyKeyPrompt>();
		prompt._graphic = image;
		return prompt;
	}

	void Update()
	{
		if (_graphic == null)
			return;

		float phase = Mathf.Repeat(Time.unscaledTime, BlinkSeconds) / BlinkSeconds;
		float alpha = Mathf.Lerp(MinAlpha, 1.0f, Ease.SmootherStep(Mathf.PingPong(phase * 2.0f, 1.0f)));

		Color color = _graphic.color;
		color.a = alpha;
		_graphic.color = color;
	}
}
