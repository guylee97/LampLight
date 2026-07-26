using UnityEngine;
using UnityEngine.UI;

public class UI_Stamina : MonoBehaviour
{
	[SerializeField]
	PlayerStatus _playerStatus;

	[SerializeField]
	Image _fill;

	[SerializeField]
	float _fillSpeed = 5.0f;

	void Awake()
	{
		if (_playerStatus == null)
			_playerStatus = FindFirstObjectByType<PlayerStatus>();
	}

	void Update()
	{
		if (_playerStatus == null || _fill == null)
			return;

		_fill.fillAmount = Mathf.MoveTowards(
			_fill.fillAmount,
			_playerStatus.StaminaRatio,
			_fillSpeed * Time.unscaledDeltaTime
		);
	}
}
