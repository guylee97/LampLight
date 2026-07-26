using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
	[SerializeField]
	float _maxStamina = 5.0f;

	[SerializeField]
	float _runStaminaCost = 1.0f;

	[SerializeField]
	float _staminaRecoverSpeed = 0.8f;

	[SerializeField, Range(0.0f, 1.0f)]
	float _runRecoveryThreshold = 0.25f;

	[SerializeField]
	float _stamina;

	bool _isExhausted;

	public float Stamina { get { return _stamina; } }
	public float MaxStamina { get { return _maxStamina; } }
	public bool CanRun { get { return !_isExhausted && _stamina > 0; } }
	public float StaminaRatio { get { return _maxStamina <= 0 ? 0 : _stamina / _maxStamina; } }

	void Awake()
	{
		_stamina = _maxStamina;
	}

	public void ConsumeRunStamina(float deltaTime)
	{
		_stamina = Mathf.Max(0, _stamina - _runStaminaCost * deltaTime);

		if (_stamina <= 0)
			_isExhausted = true;
	}

	public void RecoverStamina(float deltaTime)
	{
		_stamina = Mathf.Min(_maxStamina, _stamina + _staminaRecoverSpeed * deltaTime);

		if (_isExhausted && StaminaRatio >= _runRecoveryThreshold)
			_isExhausted = false;
	}
}
