using System;
using OpenTK.Input;
using OpenTK;

namespace Tayracer.old
{
	public class CLCamera
	{
		public CLCamera()
		{
		}

		private float _angle;
		public  Vector3 Location;

		public Matrix4 ModelView { get; private set; }

		public void Update(KeyboardDevice keyboard, float delta)
		{
			const Key Forward = Key.W;
			const Key Backward = Key.S;
			const Key Left = Key.A;
			const Key Right = Key.D;
			const Key TurnLeft = Key.Q;
			const Key TurnRight = Key.E;

			if(keyboard[Key.Q])
				_angle -= 2f * delta;
			if(keyboard[Key.E])
				_angle += 2f * delta;

			var forward = new Vector3((float)Math.Cos(_angle), 0f, (float)Math.Sin(_angle));
			var right = Vector3.Cross(forward, Vector3.UnitY);

			if(keyboard[Key.W]) Location += forward * 3f * delta;
			if(keyboard[Key.S]) Location -= forward * 3f * delta;
			if(keyboard[Key.A]) Location -= right * 3f * delta;
			if(keyboard[Key.D]) Location += right * 3f * delta;

			ModelView = Matrix4.LookAt(
				Location,
				Location + forward,
				Vector3.UnitY);
		}
	}
}

