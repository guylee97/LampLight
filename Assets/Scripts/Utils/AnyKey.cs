using UnityEngine.InputSystem;

public static class AnyKey
{
	public static bool Down
	{
		get
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
				return true;

			Gamepad pad = Gamepad.current;
			if (pad != null && (pad.buttonSouth.wasPressedThisFrame
				|| pad.buttonEast.wasPressedThisFrame
				|| pad.startButton.wasPressedThisFrame))
			{
				return true;
			}

			Mouse mouse = Mouse.current;
			return mouse != null && mouse.leftButton.wasPressedThisFrame;
		}
	}

	public static bool EscapeDown
	{
		get
		{
			Keyboard keyboard = Keyboard.current;
			return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
		}
	}
}
