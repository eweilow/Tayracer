using System;
using OpenTK.Graphics.OpenGL;
using Cloo;

namespace Tayracer
{
	public abstract class IComputeMemoryPrefab<T> where T : struct
	{
		public abstract ComputeMemory Create(ComputeContext context);
	}

	public abstract class ComputeGLPrefab<T> : IComputeMemoryPrefab<T> where T : struct
	{
		public int GLId;
		public ComputeMemoryPrefab<T> Fallback;
	}

	public class ComputeMemoryPrefab<T> : IComputeMemoryPrefab<T> where T : struct
	{
		public int ByteLength;
	}


	public class ComputeGLTexturePrefab<T> : ComputeGLPrefab<T> where T : struct
	{
		public TextureTarget TextureTarget2D;

		public ComputeMemory Create(ComputeContext context)
		{
			ComputeBuffer<T>.CreateFromGLTex2D(context, ComputeMemoryFlags.ReadWrite, (int)TextureTarget2D, GLId);
		}
	}

	public class ComputeGLBufferPrefab<T> : ComputeGLPrefab<T> where T : struct
	{
		public ComputeMemory Create(ComputeContext context)
		{
			ComputeBuffer<T>.CreateFromGLBuffer(context, ComputeMemoryFlags.ReadWrite, GLId);
		}
	}

	public class ComputeWrapper
	{
		private ComputeContext Context;
		private ComputeProgram Program;
		private ComputeKernel Kernel;
		private ComputeEventList EventList;
		private ComputeCommandQueue Commands;

		public bool LoadKernel(string path, params string[] arguments)
		{

		}

		public void Initialize()
		{

		}
	}
}

