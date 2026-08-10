using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 전각과 전각 사이를 검게 덮는다. 봉인이 끝나자마자 다음 맵이 튀어나오면
/// 방금 무슨 일이 있었는지 앉을 자리가 없다.
///
/// 대사가 timeScale 을 0 으로 잡으므로 전부 unscaled 로 센다.
public class ScreenFade : MonoBehaviour
{
	const int SortingOrder = 900;

	static ScreenFade s_instance;

	CanvasGroup _group;

	public static bool IsBlack
	{
		get { return s_instance != null && s_instance._group.alpha > 0.99f; }
	}

	public static IEnumerator To(float alpha, float seconds)
	{
		ScreenFade fade = Resolve();
		if (fade == null)
			yield break;

		float from = fade._group.alpha;
		float target = Mathf.Clamp01(alpha);

		if (seconds <= 0.0f)
		{
			fade._group.alpha = target;
			yield break;
		}

		for (float t = 0.0f; t < seconds; t += Time.unscaledDeltaTime)
		{
			fade._group.alpha = Mathf.Lerp(from, target, t / seconds);
			yield return null;
		}

		fade._group.alpha = target;
	}

	public static IEnumerator HoldBlack(float seconds)
	{
		ScreenFade fade = Resolve();
		if (fade == null)
			yield break;

		fade._group.alpha = 1.0f;

		for (float t = 0.0f; t < seconds; t += Time.unscaledDeltaTime)
			yield return null;
	}

	static ScreenFade Resolve()
	{
		if (s_instance != null)
			return s_instance;

		if (Application.isPlaying == false)
			return null;

		GameObject go = new GameObject("@ScreenFade");
		s_instance = go.AddComponent<ScreenFade>();
		return s_instance;
	}

	void Awake()
	{
		if (s_instance != null && s_instance != this)
		{
			Destroy(gameObject);
			return;
		}

		s_instance = this;
		DontDestroyOnLoad(gameObject);
		Build();
	}

	void OnDestroy()
	{
		if (s_instance == this)
			s_instance = null;
	}

	void Build()
	{
		GameObject canvasObject = new GameObject("FadeCanvas");
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SortingOrder;

		GameObject sheet = new GameObject("Black", typeof(RectTransform));
		sheet.transform.SetParent(canvasObject.transform, false);

		RectTransform rect = sheet.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		Image black = sheet.AddComponent<Image>();
		black.color = Color.black;
		black.raycastTarget = false;

		_group = sheet.AddComponent<CanvasGroup>();
		_group.alpha = 0.0f;
		_group.interactable = false;
		_group.blocksRaycasts = false;
	}
}
