
module oriole.math
{
	class Math
	{
		static field pi;

		static
		{
			Math::pi = 3.141592653532365;
		}

		static method square(x)
		{
			return x * x;
		}

		static method abs(x)
		{
			return x < 0 ? -x : x;
		}
	}
} 