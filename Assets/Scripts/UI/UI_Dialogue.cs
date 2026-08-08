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

	public enum Voice
	{
		Player,
		Thing,
	}

	struct Line
	{
		public string Text;
		public Voice From;
	}

	// 주인공 혼잣말은 등불 색, 사물의 말은 차가운 뼛빛. 이름표를 달지 않기로 했으니
	// 누가 말하는지 알려주는 건 색뿐이다.
	static readonly Color PlayerEdge = new Color(0.75f, 0.62f, 0.36f, 0.85f);
	static readonly Color PlayerInk = new Color(0.94f, 0.90f, 0.80f, 1.0f);
	static readonly Color PlayerBack = new Color(0.03f, 0.03f, 0.04f, 0.90f);

	static readonly Color ThingEdge = new Color(0.52f, 0.64f, 0.62f, 0.85f);
	static readonly Color ThingInk = new Color(0.80f, 0.87f, 0.85f, 1.0f);
	static readonly Color ThingBack = new Color(0.02f, 0.05f, 0.05f, 0.92f);

	readonly Queue<Line> _pending = new Queue<Line>();
	readonly List<Image> _edges = new List<Image>();

	CanvasGroup _group;
	Image _backdrop;
	Text _body;
	Text _hint;
	Coroutine _routine;

	public static bool IsShowing { get { return s_instance != null && s_instance._group.alpha > 0.01f; } }

	public static void Say(params string[] lines)
	{
		Say(Voice.Player, lines);
	}

	public static void Say(Voice from, params string[] lines)
	{
		UI_Dialogue box = Resolve();
		if (box == null || lines == null)
			return;

		foreach (string line in lines)
		{
			if (string.IsNullOrWhiteSpace(line) == false)
				box._pending.Enqueue(new Line { Text = line, From = from });
		}

		if (box._routine == null)
			box._routine = box.StartCoroutine(box.Run());
	}

	public static Voice VoiceOf(string name)
	{
		return string.Equals(name, "thing", System.StringComparison.OrdinalIgnoreCase)
			? Voice.Thing
			: Voice.Player;
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
		ReleaseAll();
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
		ReleaseAll();
		Build();
	}

	void OnDestroy()
	{
		if (s_instance != this)
			return;

		s_instance = null;
		ReleaseAll();
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

		_backdrop = root.AddComponent<Image>();
		_backdrop.color = PlayerBack;
		_backdrop.raycastTarget = false;

		Border(rect, PlayerEdge);

		_body = Label(rect, 38, TextAnchor.UpperLeft,
			new Color(0.94f, 0.90f, 0.80f, 1.0f));
		_body.rectTransform.offsetMin = new Vector2(44.0f, 58.0f);
		_body.rectTransform.offsetMax = new Vector2(-44.0f, -36.0f);

		_hint = Label(rect, 24, TextAnchor.LowerRight,
			new Color(0.72f, 0.62f, 0.42f, 0.9f));
		_hint.rectTransform.offsetMin = new Vector2(44.0f, 20.0f);
		_hint.rectTransform.offsetMax = new Vector2(-44.0f, -36.0f);
		_hint.text = "아무 키";
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
		_edges.Add(image);
	}

	void Wear(Voice from)
	{
		bool thing = from == Voice.Thing;

		if (_backdrop != null)
			_backdrop.color = thing ? ThingBack : PlayerBack;

		foreach (Image edge in _edges)
		{
			if (edge != null)
				edge.color = thing ? ThingEdge : PlayerEdge;
		}

		if (_body != null)
			_body.color = thing ? ThingInk : PlayerInk;
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
			Line line = _pending.Dequeue();
			Wear(line.From);
			yield return Type(line.Text);
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

	static void ReleaseAll()
	{
		if (s_holds == 0)
			return;

		s_holds = 0;
		Time.timeScale = 1.0f;
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
