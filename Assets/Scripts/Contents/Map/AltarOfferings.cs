using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 올린 공양물을 제단 앞에 실제로 늘어놓는다. 숫자 프롬프트만으로는
// 무엇을 몇 개 바쳤는지 화면에서 읽히지 않는다.
public class AltarOfferings : MonoBehaviour
{
	public const string LitMaterialResource = "Image/M_SpriteLit";

	const float FloorGap = 0.3f;
	const float FallbackRowY = -1.3f;
	const float Spacing = 0.55f;
	const float Scale = 0.5f;
	const float DropHeight = 0.5f;
	const float DropSeconds = 0.22f;
	const float SettleSeconds = 0.08f;
	const float SettleSquash = 0.22f;

	readonly List<Transform> _laid = new List<Transform>();

	SpriteRenderer _host;
	Material _lit;

	public int Count { get { return _laid.Count; } }

	public void Clear()
	{
		foreach (Transform laid in _laid)
		{
			if (laid != null)
				Destroy(laid.gameObject);
		}

		_laid.Clear();
	}

	public void Lay(Sprite sprite)
	{
		if (sprite == null)
			return;

		GameObject go = new GameObject($"Offering{_laid.Count}");
		go.transform.SetParent(transform, false);

		SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
		renderer.sprite = sprite;
		renderer.sortingLayerName = Host != null ? Host.sortingLayerName : "Default";

		Material lit = LitMaterial();
		if (lit != null)
			renderer.sharedMaterial = lit;

		_laid.Add(go.transform);
		Relayout();

		// 제단보다 앞(아래)에 있으니 y 정렬만으로 위에 그려진다.
		renderer.sortingOrder = WorldYSort.OrderFor(go.transform.position.y);
		StartCoroutine(Drop(go.transform));
	}

	SpriteRenderer Host
	{
		get
		{
			if (_host == null)
				_host = GetComponent<SpriteRenderer>();

			return _host;
		}
	}

	// 제단 스프라이트가 어디서 끝나는지 재서 그 바로 앞에 깐다.
	// 상수로 박으면 제단 아트나 피벗이 바뀌는 순간 공양물이 제단 위로 올라간다.
	float RowY
	{
		get
		{
			if (Host == null || Host.sprite == null)
				return FallbackRowY;

			return Host.bounds.min.y - transform.position.y - FloorGap;
		}
	}

	// 놓을 때마다 줄 전체를 다시 가운데로 맞춘다. 한쪽으로 자라나면
	// 마지막 하나만 제단 앞을 벗어난다.
	void Relayout()
	{
		float span = (_laid.Count - 1) * Spacing;
		float row = RowY;

		for (int i = 0; i < _laid.Count; i++)
		{
			if (_laid[i] == null)
				continue;

			_laid[i].localPosition = new Vector3(-span * 0.5f + i * Spacing, row, 0.0f);
			_laid[i].localScale = Vector3.one * Scale;
		}
	}

	IEnumerator Drop(Transform mark)
	{
		Vector3 rest = mark.localPosition;
		Vector3 start = rest + Vector3.up * DropHeight;

		for (float t = 0.0f; t < DropSeconds; t += Time.deltaTime)
		{
			if (mark == null)
				yield break;

			float k = t / DropSeconds;
			mark.localPosition = Vector3.Lerp(start, rest, k * k);
			yield return null;
		}

		if (mark == null)
			yield break;

		mark.localPosition = rest;

		// 닿는 순간 한 번 눌렸다 펴진다. 이게 없으면 미끄러져 내려온 것처럼 보인다.
		for (float t = 0.0f; t < SettleSeconds; t += Time.deltaTime)
		{
			if (mark == null)
				yield break;

			float k = Mathf.Sin(t / SettleSeconds * Mathf.PI);
			mark.localScale = new Vector3(
				Scale * (1.0f + SettleSquash * k),
				Scale * (1.0f - SettleSquash * k),
				1.0f);

			yield return null;
		}

		if (mark != null)
			mark.localScale = Vector3.one * Scale;
	}

	Material LitMaterial()
	{
		if (_lit == null)
			_lit = Resources.Load<Material>(LitMaterialResource);

		return _lit;
	}
}
