using System;
using OpenTK;
using Cloo;
using OpenTK.Graphics.OpenGL;

namespace Tayracer
{


	public class CLTracer : GameWindow 
	{
		public CLTracer(int w, int h) : base(w, h) { }

		public static void Main(string[] args)
		{
			using(var rt = new CLTracer(512, 512))
			{
				rt.VSync = VSyncMode.On;
				rt.Run(30.0);
			}
		}
	}
}

