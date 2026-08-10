using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
	const string ImagePath = "Art/UI/popup";
	const int SortingOrder = 600;

	bool _holdsTime;
	Sprite _runtimeSprite;

	public static IEnumerator Show()
	{
		if (Application.isBatchMode)
			yield break;

		GameObject go = new GameObject("@Tutorial Popup");
		TutorialPopup popup = go.AddComponent<TutorialPopup>();
		popup.Build();
		popup.HoldTime();

		// 직전 대사를 넘긴 키가 팝업까지 곧바로 닫지 않게 새 입력을 기다린다.
		yield return null;
		while (AnyKey.Down == false)
			yield return null;

		popup.ReleaseTime();
		Destroy(go);
	}

	void Build()
	{
		GameObject canvasObject = new GameObject("TutorialCanvas");
		canvasObject.transform.SetParent(transform, false);

		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SortingOrder;

		CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;

		GameObject backdrop = Stretch("Backdrop", canvasObject.transform);
		Image black = backdrop.AddComponent<Image>();
		black.color = Color.black;
		black.raycastTarget = true;

		Texture2D texture = Resources.Load<Texture2D>(ImagePath);
		if (texture == null)
		{
			Debug.LogWarning($"TutorialPopup: Resources/{ImagePath} 이미지 없음");
			return;
		}

		_runtimeSprite = Sprite.Create(
			texture,
			new Rect(0.0f, 0.0f, texture.width, texture.height),
			new Vector2(0.5f, 0.5f),
			100.0f);

		GameObject picture = Stretch("HowToPlay", canvasObject.transform);
		Image image = picture.AddComponent<Image>();
		image.sprite = _runtimeSprite;
		image.preserveAspect = true;
		image.raycastTarget = false;
	}

	static GameObject Stretch(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);

		RectTransform rect = go.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		return go;
	}

	void HoldTime()
	{
		_holdsTime = true;
		Time.timeScale = 0.0f;
	}

	void ReleaseTime()
	{
		if (_holdsTime == false)
			return;

		_holdsTime = false;
		Time.timeScale = 1.0f;
	}

	void OnDestroy()
	{
		if (_runtimeSprite != null)
			Destroy(_runtimeSprite);

		ReleaseTime();
	}
}
