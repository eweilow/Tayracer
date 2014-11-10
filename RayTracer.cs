﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using Cloo;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using NeptuneRenderEngine.Engine.Interface.Textures;
using NeptuneRenderEngine.Engine.Utilities;
using OpenTK.Graphics;

namespace Tayracer.Raycasts
{
    public struct Vector
    {
        public float X, Y, Z;
    }

    public struct RayCast
    {
        public float dirX, dirY, dirZ;
        public float X, Y, Z;
    }
    
	public class RayTracer : GameWindow
    {
		public RayTracer(int w, int h) : base(w, h) { }

		private Texture2D _tex;

		private ComputeContextPropertyList _computeContextPropertyList;
		private ComputeContext _computeContext;
		private ComputeProgram _computeProgram;
		private ComputeKernel _computeKernel;
		private ComputeEventList _computeEventList;
		private ComputeCommandQueue _commands;
		private string _kernelSource;

        [DllImport("opengl32.dll")]
        extern static IntPtr wglGetCurrentDC();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

		    ComputePlatform platform;
		    if (ComputePlatform.Platforms.Count > 1)
		    {
		        Console.WriteLine("Select a platform.");
		        int j = 0;
		        foreach (var pl in ComputePlatform.Platforms)
		            Console.WriteLine("\t{0} - {1}", j++, pl.Name);

		        string str = Console.ReadLine();

                int choice = int.Parse(str);
                platform = ComputePlatform.Platforms[choice];
		    }
		    else 
                platform = ComputePlatform.Platforms[0];

            Console.WriteLine("Running OpenCL on '{0}'", platform.Name);
		    Console.WriteLine("Platform ComputeDeviceTypes: ");
            foreach(var d in platform.Devices)
                Console.WriteLine(" -{0}", d.Type);

		    //Console.ReadLine();

            //IntPtr wglHandle = wglGetCurrentDC();
            //IntPtr glHandle = (GraphicsContext.CurrentContext as IGraphicsContextInternal).Context.Handle;
            ComputeContextProperty p1 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle.Value);

		    /*if (platform.Devices[0].Type == ComputeDeviceTypes.Gpu)
            {
                ComputeContextProperty p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, glHandle);
                ComputeContextProperty p3 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, wglHandle);
            
		        _computeContextPropertyList = new ComputeContextPropertyList(new ComputeContextProperty[] {p1, p2, p3});
		    }
		    else*/
		    {
                _computeContextPropertyList = new ComputeContextPropertyList(new ComputeContextProperty[] { p1 });
            }
            _computeContext = new ComputeContext(platform.Devices[0].Type, _computeContextPropertyList, null, IntPtr.Zero);


		    _kernelSource = File.ReadAllText("raytrace.c");

			Console.WriteLine("Creating program");
			_computeProgram = new ComputeProgram(_computeContext, new string[] {_kernelSource});
			try
			{
				Console.WriteLine("Building program");
                _computeProgram.Build(platform.Devices, null, null, IntPtr.Zero);

				Console.WriteLine("Creating kernel");
				_computeKernel = _computeProgram.CreateKernel("MatrixMultiply");

				Console.WriteLine("Creating events");
				_computeEventList = new ComputeEventList();

				Console.WriteLine("Creating commands");
				_commands = new ComputeCommandQueue(_computeContext, _computeContext.Devices[0],
					ComputeCommandQueueFlags.None);
					
				/*
				var mat = Matrix4.LookAt(Vector3.Zero, new Vector3(5f, 1f, -2f), Vector3.UnitY);
				var vec = new Vector4(1f, 2f, 3f, 1f);
				var buf = new ComputeBuffer<Matrix4>(_computeContext, ComputeMemoryFlags.ReadOnly |
					ComputeMemoryFlags.CopyHostPointer, new Matrix4[]{mat});
				var buf2 = new ComputeBuffer<Vector4>(_computeContext, ComputeMemoryFlags.ReadOnly |
					ComputeMemoryFlags.CopyHostPointer, new Vector4[]{vec});

				var buf3 = new ComputeBuffer<float>(_computeContext, ComputeMemoryFlags.WriteOnly, 4*4 + 4);

				var testKernel = _computeProgram.CreateKernel("MatrixMultiply");
				testKernel.SetMemoryArgument(0, buf);
				testKernel.SetMemoryArgument(1, buf2);
				testKernel.SetMemoryArgument(2, buf3);

				_commands.Execute(testKernel, null, new long[] {1}, null, _computeEventList);

				var arrM = new float[4*4+4];
				GCHandle arrMHandle = GCHandle.Alloc(arrM, GCHandleType.Pinned);

				_commands.Read(buf3, false, 0, 4*4+4, arrMHandle.AddrOfPinnedObject(), _computeEventList);

				arrMHandle.Free();
				buf.Dispose();
				buf2.Dispose();
				buf3.Dispose();


				Console.WriteLine(mat);
				for(int x = 0; x < 4; x++)
				{
					for(int y = 0; y < 4; y++)
					{
						Console.Write("{0} ", arrM[y*4 + x]);
					}
					Console.Write("\n");
				}
				Console.WriteLine(Vector4.Transform(vec, mat));
				for(int i = 4*4; i < 4*4+4; i++)
					Console.Write("{0} ", arrM[i]);*/



				//ResultDataBuffer = new ComputeBuffer<byte>(_computeContext, ComputeMemoryFlags.WriteOnly, 1);
				MatrixBuffer = new ComputeBuffer<Matrix4>(_computeContext, 
					ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer,
					new Matrix4[1]);

				DataBuffer = new ComputeBuffer<float>(_computeContext, 
					ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer,
					new float[5]);
				ResultDataBuffer = new ComputeBuffer<float>(_computeContext, ComputeMemoryFlags.WriteOnly, 1);
				Console.WriteLine("Success!");
			}
			catch (BuildProgramFailureComputeException ex)
			{
				Console.WriteLine("Build error");
				Console.WriteLine(_computeProgram.GetBuildLog(_computeContext.Devices[0]));
				Exit();
				Console.Read();
				//throw ex;

			}

			_tex = new Texture2D();
			_tex.TexImage(1, 1);
			_tex.SetTextureMagFilter(TextureMagFilter.Nearest);
            _tex.SetTextureMinFilter(TextureMinFilter.Nearest);

			/*
		    buf = GL.GenBuffer();
		    GL.BindBuffer(BufferTarget.TextureBuffer, buf);
            GL.BufferData(BufferTarget.TextureBuffer, new IntPtr(Width * Height * 3), IntPtr.Zero, BufferUsageHint.StreamDraw);
            ResultDataBuffer = ComputeBuffer<float>.CreateFromGLBuffer<float>(_computeContext, ComputeMemoryFlags.ReadWrite, buf);
            */
		}

	    private int buf;

		private int count = 0;

		private int p_width = -1, p_height = -1;
		private Vector3 p_origin;
		private Matrix4 p_matrix;

		public ComputeBuffer<float> DataBuffer;
		public ComputeBuffer<Matrix4> MatrixBuffer;
		public ComputeBuffer<float> ResultDataBuffer;

		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);
			DataBuffer.Dispose();
			MatrixBuffer.Dispose();
			ResultDataBuffer.Dispose();
		}

		public float[] ExecuteGpu(int width, int height, Vector3 origin, Matrix4 matrix)
        {
			if(ResultDataBuffer == null || p_width != width || p_height != height)
			{
				p_width = width;
				p_height = height;
				count = p_width * p_height;

				ResultDataBuffer.Dispose();
				ResultDataBuffer = new ComputeBuffer<float>(_computeContext, ComputeMemoryFlags.WriteOnly, count*3);

				//_res = new float[count * 3];
			}

			if(MatrixBuffer == null || p_matrix != matrix)
			{
				p_matrix = matrix;

				var mat = new Matrix4[]{ matrix};
				_commands.WriteToBuffer(mat, MatrixBuffer, false, _computeEventList); 
			}
				
			_computeKernel.SetValueArgument(0, p_width);
			_computeKernel.SetValueArgument(1, p_height);
			_computeKernel.SetValueArgument(2, new Vector4(origin, 0));
			_computeKernel.SetMemoryArgument(3, MatrixBuffer);
			_computeKernel.SetMemoryArgument(4, ResultDataBuffer);

			_commands.Finish();
            var swnew = Stopwatch.StartNew();
			_commands.Execute(_computeKernel, null, new long[] { Width, Height }, null, _computeEventList);
			_commands.Finish();
			swnew.Stop();
			_avrgExc.AddValue(swnew.ElapsedMilliseconds);

			if(_col== null || _col.Length != count * 3) _col = new float[count * 3];
			_commands.ReadFromBuffer(ResultDataBuffer, ref _col, true, _computeEventList);
            //_commands.Read(ResultDataBuffer, false, 0, count * 3, arrCHandle.AddrOfPinnedObject(), _computeEventList);
            _commands.Finish();
		

			return new float[0];
        }

		private float[] _res;

		private Vector3[] _origins = new Vector3[0];
		private Vector3[] _targets = new Vector3[0];
		private Matrix4 _inverseProjection, projection;
		private float _zDepth, _zMult;

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			_origins = new Vector3[Width * Height];
			_targets = new Vector3[Width * Height];

			_zDepth = 2000.1f - 0.1f;
			_zMult = 1/_zDepth;
			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)Width / Height, 0.1f, 2000.1f);
			_inverseProjection = Matrix4.Invert(projection);

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float _x = x / (float)Width * 2f - 1f;
					float _y = y / (float)Height * 2f - 1f;
					Vector4 screenVec = Vector4.Transform(new Vector4(_x, _y, 0f, 1f), _inverseProjection);
					Vector4 targetVec = Vector4.Transform(new Vector4(_x, _y, 1f, 1f), _inverseProjection);

					_origins[y * Width + x] = screenVec.Xyz / screenVec.W;
					_targets[y * Width + x] = targetVec.Xyz / targetVec.W;
				}
			}
		}

		private Average _avrgPre = new Average(), _avrgRnd = new Average(), _avrgExc = new Average();

		private long sampledPre, sampledRender;

		private Vector3 _pos = Vector3.Zero;
		private float _angle;
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			var sw = Stopwatch.StartNew();
			if(Keyboard[Key.Q])
				_angle -= 0.01f;
			else
			if(Keyboard[Key.E])
				_angle += 0.01f;

			var forward = new Vector3((float)Math.Cos(_angle), 0f, (float)Math.Sin(_angle));
			var right = Vector3.Cross(forward, Vector3.UnitY);

			if(Keyboard[Key.W]) _pos += forward * 0.1f;
			if(Keyboard[Key.S]) _pos -= forward * 0.1f;
			if(Keyboard[Key.A]) _pos -= right * 0.1f;
			if(Keyboard[Key.D]) _pos += right * 0.1f;
			var modelview = Matrix4.LookAt(
				_pos,
				_pos + forward,
				Vector3.UnitY);
			var scale = Matrix4.CreateScale((1f / Width) * 2f, (1f / Height) * 2f, 1f);
			var translate = Matrix4.CreateTranslation(-1f, -1f, 0f);
			var mv = Matrix4.Mult(modelview, projection);
			var _invModelview = Matrix4.Mult(scale, Matrix4.Mult(translate, Matrix4.Invert(mv)));


			/*Vector3[] origins = new Vector3[_origins.Length];
			Vector3[] targets = new Vector3[_targets.Length];
			Parallel.For(0, Width, (x) =>
			{
				for(int y = 0; y < Height; y++)
				{

					int i = y * Width + x;
					origins[i] = Vector3.Transform(_origins[i], _invModelview) * _zMult;
					//targets[i] = _targets[i];
					targets[i] = Vector3.Transform(_targets[i], _invModelview) * _zMult;
					targets[i] *= 1000f;
				}
			});*/
            sw.Stop();

            _avrgPre.AddValue(sw.ElapsedMilliseconds);
			//Console.WriteLine("Pre: {0} ms", sw.ElapsedMilliseconds);
			sw.Reset();
			sw.Restart();
			ExecuteGpu(Width, Height, _pos, _invModelview);
            sw.Stop();
            _avrgRnd.AddValue(sw.ElapsedMilliseconds);
			//Console.WriteLine("Gpu: {0} ms", sw.ElapsedMilliseconds);
		
			Title = string.Format("Tayracer {1} {2} {0}", _avrgPre.Format(), _avrgRnd.Format(), _avrgExc.Format());
		}

		private float[] _col;

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			_tex.Bind();
			_tex.TexImage(Width, Height, _col, PixelInternalFormat.Rgb, PixelFormat.Rgb, PixelType.Float);


			GL.ClearColor(0f, 0f, 0f, 0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);


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
			GL.BindTexture(TextureTarget.Texture2D, _tex.ID);
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

			//Title = GL.GetError().ToString();

			SwapBuffers();
		}
    }
}
