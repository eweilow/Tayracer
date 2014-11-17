using System;
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using ComputeAddict;

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
			Console.Clear();

			ComputePlatform platform = ComputeHelper.PromptPlatform();
			var device = platform.PromptDevice();

			Console.WriteLine("Running OpenCL on '{0}'\n - Device: {1}\n - Type: {2}", platform.Name, device.Name, device.Type);

			var devices = new List<ComputeDevice>(){device};

            if (device.Type == ComputeDeviceTypes.Gpu && Directory.Exists("C:"))
            {
                Console.WriteLine("Booting up CL/GL Interop.");
                var curDC = wglGetCurrentDC();
                var ctx = (IGraphicsContextInternal)GraphicsContext.CurrentContext;
                var raw_context_handle = ctx.Context.Handle;
                var p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, raw_context_handle);
				var p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, curDC);
                var p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle.Value); 
                var props = new List<ComputeContextProperty>() { p1, p2, p3 };

                ComputeContextPropertyList Properties = new ComputeContextPropertyList(props);
                _computeContext = new ComputeContext(devices, Properties, null, IntPtr.Zero);
            }
            else
            {
                var properties = new ComputeContextPropertyList(platform);
                _computeContext = new ComputeContext(devices, properties, null, IntPtr.Zero);
            }

		    _kernelSource = File.ReadAllText("kernel/raytracer.c");

			Console.WriteLine("Creating program");
			_computeProgram = new ComputeProgram(_computeContext, new string[] {_kernelSource});
			try
			{
				Console.WriteLine("Building program");
                _computeProgram.Build(devices, null, null, IntPtr.Zero);

				Console.WriteLine("Creating kernel");
				_computeKernel = _computeProgram.CreateKernel("MatrixMultiply");

				Console.WriteLine("Creating events");
				_computeEventList = new ComputeEventList();


				Console.WriteLine("Creating commands");
				_commands = new ComputeCommandQueue(_computeContext, device, ComputeCommandQueueFlags.None);

			}
			catch (BuildProgramFailureComputeException ex)
			{
				Console.WriteLine("Build error");
				Console.WriteLine(_computeProgram.GetBuildLog(_computeContext.Devices[0]));
				Exit();
				Console.Read();
            }
            MatrixBuffer = _computeContext.AllocateBuffer<Matrix4>(ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, new Matrix4[1]);
            DataBuffer = _computeContext.AllocateBuffer<float>(ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, new float[5]);


            _tex = new Texture2D();
            _tex.TexImage(512, 512, IntPtr.Zero, PixelInternalFormat.Rgba32f, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.Float, 0);
            _tex.SetTextureMagFilter(TextureMagFilter.Linear);
            _tex.SetTextureMinFilter(TextureMinFilter.Linear);
            //ResultDataBuffer = _computeContext.AllocateBuffer<float>(ComputeMemoryFlags.WriteOnly, 1);
            _tex.Bind(0);

            ResultDataBuffer = ComputeBuffer<float>.CreateFromGLTex2D<float>(_computeContext, ComputeMemoryFlags.WriteOnly, (int)_tex.TextureTarget, _tex.ID);

		}

		private int count = 0, byteCount = 0;

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

		private Stopwatch _sw = new Stopwatch();
		public void ExecuteGpu(int width, int height, Vector3 origin, Matrix4 matrix)
        {
            List<ComputeMemory> c = new List<ComputeMemory>() { ResultDataBuffer };
		    try
		    {

			if(ResultDataBuffer == null || p_width != width || p_height != height)
			{
				p_width = width;
				p_height = height;
				count = p_width * p_height;
				byteCount = count * 4;

				_computeKernel.SetValueArgument(0, p_width);
				_computeKernel.SetValueArgument(1, p_height);

                //ResultDataBuffer.Dispose();
                _tex.TexImage(p_width, p_height, IntPtr.Zero, PixelInternalFormat.Rgba32f, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.Float, 0);
			    _tex.Bind(0);
                //ResultDataBuffer = ComputeBuffer<float>.CreateFromGLTex2D<float>(_computeContext, ComputeMemoryFlags.WriteOnly, (int)_tex.TextureTarget, _tex.ID);
				//ResultDataBuffer.Dispose();
				//ResultDataBuffer = new ComputeBuffer<float>(_computeContext, ComputeMemoryFlags.WriteOnly, byteCount);

				_computeKernel.SetMemoryArgument(4, ResultDataBuffer);
			}

			if(MatrixBuffer == null || p_matrix != matrix)
			{
				p_matrix = matrix;
				_computeKernel.SetValueArgument(3, p_matrix);
			}
			if(p_origin != origin)
			{
				p_origin = origin;
			}
			_computeKernel.SetValueArgument(2, new Vector4(p_origin, 0));

            GL.Finish();
            _commands.AcquireGLObjects(c, null);

			_commands.Finish();
			_sw.Reset();
			_sw.Start();
            _commands.Execute(_computeKernel, null, new long[] { width, height }, null, _computeEventList);
			_sw.Stop();
            _avrgExc.AddValue(_sw.ElapsedMilliseconds);

            _commands.ReleaseGLObjects(c, null);
            _commands.Finish();


			_texWidth = width;
			_texHeight = height;

			//if(_col== null || _col.Length != byteCount) _col = new float[byteCount];

			//_commands.ReadFromBuffer(ResultDataBuffer, ref _col, false, _computeEventList);
                //_commands.Finish();
            }
            catch (Exception)
            {
                _commands.ReleaseGLObjects(c, null);
                _commands.Finish();
                Exit();
                
                throw;
            }
        }

		private float[] _res;

		//private Vector3[] _origins = new Vector3[0];
		//private Vector3[] _targets = new Vector3[0];
		private Matrix4 _inverseProjection, projection;
		private float _zDepth, _zMult;

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			//_origins = new Vector3[Width * Height];
			//_targets = new Vector3[Width * Height];

			_zDepth = 2000.1f - 0.1f;
			_zMult = 1/_zDepth;
			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)Width / Height, 0.1f, 2000.1f);
			_inverseProjection = Matrix4.Invert(projection);

			/*for (int x = 0; x < Width; x++)
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
			}*/
		}

		private Average _avrgPre = new Average(), _avrgRnd = new Average(), _avrgExc = new Average();

		private long sampledPre, sampledRender;

		double mult = 1.0;

		private bool _pressed = false;
		private Vector3 _pos = Vector3.Zero;
		private float _angle;
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			var sw = Stopwatch.StartNew();
			if(Keyboard[Key.Q])
				_angle -= 2f * (float)UpdateTime;
			if(Keyboard[Key.E])
					_angle += 2f * (float)UpdateTime;

			var forward = new Vector3((float)Math.Cos(_angle), 0f, (float)Math.Sin(_angle));
			var right = Vector3.Cross(forward, Vector3.UnitY);

			if(Keyboard[Key.W]) _pos += forward * 3f * (float)UpdateTime;
			if(Keyboard[Key.S]) _pos -= forward * 3f * (float)UpdateTime;
			if(Keyboard[Key.A]) _pos -= right * 3f * (float)UpdateTime;
			if(Keyboard[Key.D]) _pos += right * 3f * (float)UpdateTime;


			if(Keyboard[Key.Number1] && mult > 0.02)
			{
				mult -= 0.01;
			}
			if(Keyboard[Key.Number2] && mult < 50)
			{
				mult += 0.01;
			}
			//double mult = 5;
			if(mult > 1)
			{
                _tex.SetTextureMagFilter(TextureMagFilter.Nearest);
                _tex.SetTextureMinFilter(TextureMinFilter.Nearest);
			}
			else
			{
				_tex.SetTextureMagFilter(TextureMagFilter.Nearest);
				_tex.SetTextureMinFilter(TextureMinFilter.Nearest);
			}

			bool _screenshot = false;
			double _tmult = mult;
			if(Keyboard[Key.Number3] && !_pressed)
			{
				_pressed = true;
				_screenshot = true;

				mult = 5;
			}
			else
			if(!Keyboard[Key.Number3] && _pressed)
				_pressed = false;

			int width = (int)(mult * Width);
			int height = (int)(mult * Height);

			var modelview = Matrix4.LookAt(
				_pos,
				_pos + forward,
				Vector3.UnitY);
			var scale = Matrix4.CreateScale((1f / width) * 2f, (1f / height) * 2f, 1f);
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


			ExecuteGpu(width, height, _pos, _invModelview);
            sw.Stop();
            _avrgRnd.AddValue(sw.ElapsedMilliseconds);
			//Console.WriteLine("Gpu: {0} ms", sw.ElapsedMilliseconds);
		
			Title = string.Format("Tayracer {1} {2} {0}", _avrgPre.Format(), _avrgRnd.Format(), _avrgExc.Format());
		
			if(_screenshot)
			{
				/*
			    _scrData = _col;
				var t = new Thread(new ParameterizedThreadStart(SaveScreenshot));
				t.Start(new Size(width, height));

				mult = _tmult;*/
			}
		}

	    private float[] _scrData;
		private void SaveScreenshot(object obj)
		{
			Size s = (Size)obj;
			const string format = "screenshots/scr{0}.png";

			if(!Directory.Exists("screenshots"))
				Directory.CreateDirectory("screenshots");

			int i = 0;
			string file = string.Format(format, i);
			while(File.Exists(file))
			{
				Console.WriteLine("File exists: '{0}'", file);
				file = string.Format(format, ++i);
			}

			Console.WriteLine("Saving screenshot to '{0}'", file);

            var _data = _scrData.ToArray();
			try 
			{
				using (Bitmap bitmap = new Bitmap(s.Width, s.Height)) {
					Console.WriteLine("Bitmap created.");


					for(int x = 0; x < s.Width; x++)
					{
						Console.WriteLine("x: {0}", x);
						for(int y = 0; y < s.Height; y++)
						{
							int index = (x * s.Width + y) * 3;

                            int r = (int)(_data[index] * 255); r = r < 0 ? 0 : (r > 255 ? 255 : r);
                            int g = (int)(_data[index + 1] * 255); g = g < 0 ? 0 : (g > 255 ? 255 : g);
                            int b = (int)(_data[index + 2] * 255); b = b < 0 ? 0 : (b > 255 ? 255 : b);
                            bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
						}
					}
					Console.WriteLine("Saving bitmap.");
                    bitmap.Save(file, ImageFormat.Png);
				}
				Console.WriteLine("Bitmap saved. Done.");
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private int _texWidth, _texHeight;
		private float[] _col;

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			//_tex.Bind();
			//_tex.TexImage(_texWidth, _texHeight, _col, PixelInternalFormat.Rgba, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.Float);


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
