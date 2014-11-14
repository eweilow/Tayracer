using System;
using OpenTK;

namespace Tayracer
{
	public struct Ray
	{
		public Vector3 Origin;
		public Vector3 Direction;
		public float Magnitude;

		public Vector3 A, B;

		public float IOR;

		public Ray(Vector3 origin, Vector3 target)
		{
			Origin = origin;
			Direction = Vector3.Normalize(target);
			Magnitude = target.Length;
			A = origin;
			B = origin + target;
			IOR = 1f;

		}
	}
}

