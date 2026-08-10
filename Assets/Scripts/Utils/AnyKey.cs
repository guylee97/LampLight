using UnityEngine.InputSystem;

public static class AnyKey
{
	public static bool Down
	{
		get
		{
			if (KeyOrPadDown)
				return true;

			Mouse mouse = Mouse.current;
			return mouse != null && mouse.leftButton.wasPressedThisFrame;
		}
	}

	public static bool KeyOrPadDown
	{
		get
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
				return true;

			Gamepad pad = Gamepad.current;
			return pad != null && (pad.buttonSouth.wasPressedThisFrame
				|| pad.buttonEast.wasPressedThisFrame
				|| pad.startButton.wasPressedThisFrame);
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
