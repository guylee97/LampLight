using UnityEngine;

public interface IInteractable
{
	bool CanInteract { get; }
	string Prompt { get; }
	Vector3 Position { get; }
	float HoldSeconds { get; }
	void Interact(PlayerController player);
}
