using System;
using OpenTK;

namespace Tayracer
{
	public class ScreenRender
	{
		private int _height, _width;
		private float[] _rawBuffer; 

		public void Resize(int w, int h)
		{
			_rawBuffer = new float[w*h];
		}

		public void Set(int x, int y, float r, float g, float b)
		{
			int pos = y * _width + x;
			if(pos < 0 || pos >= _rawBuffer.Length) return;

			_rawBuffer[pos] = r;
			_rawBuffer[pos + 1] = g;
			_rawBuffer[pos + 2] = b;
		}
	}
}

