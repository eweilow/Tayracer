using System;
using OpenTK;
using Cloo;
using OpenTK.Graphics.OpenGL;
using ComputeAddict;
using System.Collections.Generic;

using CMF = Cloo.ComputeMemoryFlags;
using NeptuneRenderEngine.Engine.Interface.Textures;
using NeptuneRenderEngine.Engine.Utilities;
using Tayracer.old;
using System.Runtime.InteropServices;


namespace Tayracer
{
	public class CLTracer : GameWindow 
	{
		public static void Main(string[] args)
		{
			using(var rt = new CLTracer(512, 512))
			{
				rt.VSync = VSyncMode.Off;
				rt.Run(30.0);
			}
		    Console.Read();
		}

		public CLTracer(int w, int h) : base(w, h) { }

		public ComputeWrapper Compute;
		public ComputeBuffer<float> DataBuffer;
		public Texture2D Texture;
		public Matrix4 InverseProjection, Projection, Scale, Translation, InverseMatrix;
		public Average Init, DataWriteAvrg, ExecuteAvrg, DataReadAvrg, RenderFreq;
		public CLCamera Camera;

	    private PixelInternalFormat _texPIF;
	    private PixelFormat _texPF;
	    private PixelType _texPT;

	    private int _previousWidth, _previousHeight;
	    private int _byteSize;
		public void Execute(int width, int height, Vector3 vec, Matrix4 matrix)
        {

            var glObjects = new List<ComputeMemory> { DataBuffer };
            if(_previousWidth != width || _previousHeight != height)
            {
                _previousWidth = width;
                _previousHeight = height;
                if (ComputeWrapper.CanInterop)
                {
                    Texture.TexImage(width, height, IntPtr.Zero, _texPIF, _texPF, _texPT, 0);
                    Texture.Bind(0);
                }
                else
                {
                    _byteSize = width*height*4;

                    DataBuffer.Dispose();
                    DataBuffer = Compute.Context.AllocateBuffer<float>(CMF.WriteOnly, _byteSize);
                }
                Compute.Kernel.SetValueArgument(0, width);
                Compute.Kernel.SetValueArgument(1, height);
                Compute.Kernel.SetMemoryArgument(4, DataBuffer);
            }

			Compute.Kernel.SetValueArgument(2, new Vector4(vec, 1));
			Compute.Kernel.SetValueArgument(3, matrix);

            if (ComputeWrapper.CanInterop)
            {
                GL.Finish();
                Compute.Commands.AcquireGLObjects(glObjects, null);
            }
            ExecuteAvrg.StartTiming();
            Compute.Commands.Execute(Compute.Kernel, null, new long[] { width, height }, null, Compute.EventList);
            if (ComputeWrapper.CanInterop) Compute.Commands.ReleaseGLObjects(glObjects, null);
            
            if (!ComputeWrapper.CanInterop)
            {
                IntPtr ptr = Marshal.AllocHGlobal(_byteSize);
                Compute.Commands.Read(DataBuffer, true, 0, _byteSize, ptr, Compute.EventList);
			    Compute.Commands.Finish();

                Texture.TexImage(width, height, ptr, _texPIF, _texPF, _texPT, 0);
			    Texture.Bind(0);

                Marshal.FreeHGlobal(ptr);
            }
            Compute.Commands.Finish();
            ExecuteAvrg.StopTiming();
		}

		protected override void OnLoad(EventArgs e)
        {
            Init = new Average();
            DataWriteAvrg = new Average();
            ExecuteAvrg = new Average();
            DataReadAvrg = new Average();
            RenderFreq = new Average();

            _texPIF = PixelInternalFormat.Rgba;
            _texPF = PixelFormat.Rgba;
            _texPT = PixelType.Float;

			Compute = new ComputeWrapper();
			Camera = new CLCamera();

			Console.Clear();
			ComputePlatform platform = ComputeHelper.PromptPlatform();
			ComputeDevice   device 	 = platform.PromptDevice();

			Console.WriteLine("Running OpenCL on '{0}'\n - Device: {1}\n - Type: {2}", platform.Name, device.Name, device.Type);
			Compute.CreateContext(platform, device);
			if(!Compute.LoadAndBuildKernel("kernel/raytracer.c", "MatrixMultiply", ComputeWrapper.CanInterop ? "#define INTEROP\n" : "")) Exit();

            Console.WriteLine(GL.GetError());
			Texture = new Texture2D();
            Texture.TexImage(Width, Height, IntPtr.Zero, _texPIF, _texPF, _texPT, 0);
			Texture.SetTextureMagFilter(TextureMagFilter.Linear);
			Texture.SetTextureMinFilter(TextureMinFilter.Linear);
			Texture.Bind(0);

            Console.WriteLine(GL.GetError());

			if(!ComputeWrapper.CanInterop)
			{
				DataBuffer = Compute.Context.AllocateBuffer<float>(CMF.WriteOnly, 1);
			}
			else
			{
                DataBuffer = ComputeBuffer<float>.CreateFromGLTex2D<float>(Compute.Context, ComputeMemoryFlags.WriteOnly, (int)Texture.TextureTarget, Texture.ID);
			}

			Compute.EventList = new ComputeEventList();

		}

		protected override void OnUnload(EventArgs e)
		{
			if(DataBuffer != null) DataBuffer.Dispose();
			if(Compute != null) Compute.Dispose();
		}

		protected override void OnResize(EventArgs e)
		{
            if (!ComputeWrapper.CanInterop)
            {
                Texture.TexImage(Width, Height, IntPtr.Zero, _texPIF, _texPF, _texPT, 0);
                Texture.Bind(0);
            }

			Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)Width / Height, 0.1f, 2000.1f);
			InverseProjection = Matrix4.Invert(Projection);

			Scale = Matrix4.CreateScale((1f / Width) * 2f, (1f / Height) * 2f, 1f);
			Translation = Matrix4.CreateTranslation(-1f, -1f, 0f);
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
            Init.StartTiming();
            Camera.Update(Keyboard, (float)UpdatePeriod);

			var mv = Matrix4.Mult(Camera.ModelView, Projection);
            InverseMatrix = Matrix4.Mult(Scale, Matrix4.Mult(Translation, Matrix4.Invert(mv)));
            Init.StopTiming();

            Execute(Width, Height, Camera.Location, InverseMatrix);

		    RenderFreq.AddValue(RenderFrequency);

            Title = string.Format("Tayracer {4} {2} {1} {3} {0}", Init, DataWriteAvrg, ExecuteAvrg, DataReadAvrg, RenderFreq);
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
            base.OnRenderFrame(e);

			GL.ClearColor(0f, 0f, 0f, 0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.Viewport(0, 0, Width, Height);

			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0.0, Width, 0.0, Height, -1.0, 1.0);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Disable(EnableCap.Lighting);
			GL.Color3(1f, 1f, 1f);

			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, Texture.ID);
			GL.Begin(PrimitiveType.Quads);
			GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
			GL.TexCoord2(0, 1); GL.Vertex3(0, Height, 0);
			GL.TexCoord2(1, 1); GL.Vertex3(Width, Height, 0);
			GL.TexCoord2(1, 0); GL.Vertex3(Width, 0, 0);
			GL.End();
			GL.Disable(EnableCap.Texture2D);
			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);

			SwapBuffers();
		}
	}
}

