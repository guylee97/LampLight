public static class Determinism
{
	public const int Seed = 20260807;

	public static System.Random Stream(int salt)
	{
		return new System.Random(Seed + salt);
	}
}
