using UnityEngine;
using UnityEngine.UI;

public class PressAnyKeyPrompt : MonoBehaviour
{
	const float BlinkSeconds = 1.1f;

	Text _label;
	float _minAlpha = 0.25f;

	public static PressAnyKeyPrompt Attach(
		Transform parent,
		string message,
		float anchorY,
		int fontSize = 34)
	{
		GameObject go = new GameObject("PressAnyKey", typeof(RectTransform), typeof(Text));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, anchorY);
		rect.anchorMax = new Vector2(0.5f, anchorY);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = new Vector2(900.0f, 70.0f);

		Text text = go.GetComponent<Text>();
		text.font = KoreanFont.Font;
		text.fontSize = fontSize;
		text.alignment = TextAnchor.MiddleCenter;
		text.color = new Color(0.93f, 0.86f, 0.68f, 1.0f);
		text.raycastTarget = false;
		text.text = message;

		PressAnyKeyPrompt prompt = go.AddComponent<PressAnyKeyPrompt>();
		prompt._label = text;
		return prompt;
	}

	public void SetMessage(string message)
	{
		if (_label != null)
			_label.text = message;
	}

	void Update()
	{
		if (_label == null)
			return;

		float phase = Mathf.Repeat(Time.unscaledTime, BlinkSeconds) / BlinkSeconds;
		float alpha = Mathf.Lerp(_minAlpha, 1.0f, Ease.SmootherStep(Mathf.PingPong(phase * 2.0f, 1.0f)));

		Color color = _label.color;
		color.a = alpha;
		_label.color = color;
	}
}
