using UnityEngine;

public interface IInteractable
{
	bool CanInteract { get; }
	string Prompt { get; }

	/// 홀드가 도는 동안 화면에 뜨는 말. 무엇을 하고 있는지는 대상만 안다.
	string HoldingLabel { get; }
	Vector3 Position { get; }
	float HoldSeconds { get; }
	void Interact(PlayerController player);
}
