using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_Dialogue : MonoBehaviour
{
	public const string TypeClip = "text_type";

	const float CharactersPerSecond = 34.0f;
	const int SortingOrder = 550;

	// 대사가 떠 있는 동안 게임은 멈춘다. 읽는 사이에 요괴가 다가오면 대사를 읽을 수 없다.
	static int s_holds;

	static UI_Dialogue s_instance;

	readonly Queue<string> _pending = new Queue<string>();

	CanvasGroup _group;
	Text _body;
	Text _hint;
	Coroutine _routine;

	public static bool IsShowing { get { return s_instance != null && s_instance._group.alpha > 0.01f; } }

	public static void Say(params string[] lines)
	{
		UI_Dialogue box = Resolve();
		if (box == null)
			return;

		foreach (string line in lines)
		{
			if (string.IsNullOrWhiteSpace(line) == false)
				box._pending.Enqueue(line);
		}

		if (box._routine == null)
			box._routine = box.StartCoroutine(box.Run());
	}

	public static void Clear()
	{
		if (s_instance == null)
			return;

		s_instance._pending.Clear();

		if (s_instance._routine != null)
		{
			s_instance.StopCoroutine(s_instance._routine);
			s_instance._routine = null;
		}

		s_instance._group.alpha = 0.0f;
		Release();
	}

	static UI_Dialogue Resolve()
	{
		if (s_instance != null)
			return s_instance;

		if (Application.isPlaying == false)
			return null;

		GameObject go = new GameObject("@Dialogue");
		s_instance = go.AddComponent<UI_Dialogue>();
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
		Build();
	}

	void OnDestroy()
	{
		if (s_instance == this)
			s_instance = null;
	}

	void Build()
	{
		GameObject canvasObject = new GameObject("DialogueCanvas");
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SortingOrder;

		CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

		GameObject root = new GameObject("Box", typeof(RectTransform));
		root.transform.SetParent(canvasObject.transform, false);

		RectTransform rect = root.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.0f);
		rect.anchorMax = new Vector2(0.5f, 0.0f);
		rect.pivot = new Vector2(0.5f, 0.0f);
		rect.anchoredPosition = new Vector2(0.0f, 56.0f);
		rect.sizeDelta = new Vector2(1360.0f, 210.0f);

		_group = root.AddComponent<CanvasGroup>();
		_group.alpha = 0.0f;
		_group.interactable = false;
		_group.blocksRaycasts = false;

		Image backdrop = root.AddComponent<Image>();
		backdrop.color = new Color(0.03f, 0.03f, 0.04f, 0.90f);
		backdrop.raycastTarget = false;

		Border(rect, new Color(0.75f, 0.62f, 0.36f, 0.85f));

		_body = Label(rect, 38, TextAnchor.UpperLeft,
			new Color(0.94f, 0.90f, 0.80f, 1.0f));
		_body.rectTransform.offsetMin = new Vector2(44.0f, 58.0f);
		_body.rectTransform.offsetMax = new Vector2(-44.0f, -36.0f);

		_hint = Label(rect, 24, TextAnchor.LowerRight,
			new Color(0.72f, 0.62f, 0.42f, 0.9f));
		_hint.rectTransform.offsetMin = new Vector2(44.0f, 20.0f);
		_hint.rectTransform.offsetMax = new Vector2(-44.0f, -36.0f);
		_hint.text = "▾ 아무 키";
		_hint.enabled = false;
	}

	void Border(RectTransform parent, Color color)
	{
		float thickness = 3.0f;

		Edge(parent, color, thickness, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -thickness));
		Edge(parent, color, thickness, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, thickness));
	}

	void Edge(RectTransform parent, Color color, float thickness,
		Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
	{
		GameObject go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.offsetMin = new Vector2(0.0f, Mathf.Min(0.0f, size.y));
		rect.offsetMax = new Vector2(0.0f, Mathf.Max(0.0f, size.y));

		Image image = go.GetComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
	}

	Text Label(RectTransform parent, int size, TextAnchor anchor, Color color)
	{
		GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;

		Text text = go.GetComponent<Text>();
		text.font = KoreanFont.Font;
		text.fontSize = size;
		text.alignment = anchor;
		text.color = color;
		text.raycastTarget = false;
		text.horizontalOverflow = HorizontalWrapMode.Wrap;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		return text;
	}

	IEnumerator Run()
	{
		_group.alpha = 1.0f;
		Hold();

		while (_pending.Count > 0)
		{
			string line = _pending.Dequeue();
			yield return Type(line);
			yield return WaitForAdvance();
		}

		_group.alpha = 0.0f;
		_routine = null;
		Release();
	}

	static void Hold()
	{
		s_holds++;

		if (s_holds == 1)
			Time.timeScale = 0.0f;
	}

	static void Release()
	{
		if (s_holds <= 0)
			return;

		s_holds--;

		if (s_holds == 0)
			Time.timeScale = 1.0f;
	}

	IEnumerator Type(string line)
	{
		_body.text = string.Empty;
		float shown = 0.0f;

		while (shown < line.Length)
		{
			if (Advance())
			{
				_body.text = line;
				yield return null;
				yield break;
			}

			shown += CharactersPerSecond * Time.unscaledDeltaTime;
			int count = Mathf.Clamp(Mathf.FloorToInt(shown), 0, line.Length);

			if (count != _body.text.Length)
			{
				_body.text = line.Substring(0, count);
				Managers.Sound.PlayOptional(TypeClip, Define.Sound.UI, 1.0f, 0.35f);
			}

			yield return null;
		}

		_body.text = line;
	}

	/// 자동으로 넘어가지 않는다. 읽고 나서 직접 넘긴다.
	IEnumerator WaitForAdvance()
	{
		_hint.enabled = true;

		while (Advance() == false)
			yield return null;

		_hint.enabled = false;
	}

	bool Advance()
	{
		return AnyKey.Down;
	}
}
