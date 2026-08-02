using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IDragHandler, IPointerEnterHandler
{
	public const string ClickClip = "ui_click";
	public const string HoverClip = "ui_hover";

	public Action<PointerEventData> OnClickHandler = null;
	public Action<PointerEventData> OnDragHandler = null;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (OnClickHandler == null)
			return;

		Managers.Sound.PlayOptional(ClickClip, Define.Sound.UI);
		OnClickHandler.Invoke(eventData);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (OnClickHandler != null)
			Managers.Sound.PlayOptional(HoverClip, Define.Sound.UI);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (OnDragHandler != null)
			OnDragHandler.Invoke(eventData);
	}
}
