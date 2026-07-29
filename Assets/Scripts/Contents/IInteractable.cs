using UnityEngine;

public interface IInteractable
{
	bool CanInteract { get; }
	string Prompt { get; }
	Vector3 Position { get; }
	void Interact(PlayerController player);
}
