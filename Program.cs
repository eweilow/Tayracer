using System;
using OpenTK;
using NeptuneRenderEngine.Engine.Interface.Buffers;
using NeptuneRenderEngine.Engine.Interface.Textures;
using OpenTK.Graphics.OpenGL;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using OpenTK.Input;
using System.Threading.Tasks;

using Tayracer.Raycasts;
using System.IO;

namespace Tayracer
{
	public class Program
	{
	    private const int N = 256;

		/*[Cudafy]
        public static void Calc(GThread thread, float[] a, float[] b, float[] c)
        {
		    int id = thread.get_global_id(0);

		    c[id] = c[id] + id;
            /*
            for (int i = 0; i < size; i++ )
                c[id + i] = size;* /

        }*/

		public static void Main(string[] args)
		{
			using(var rt = new RayTracer(512, 512))
			{
				rt.VSync = VSyncMode.On;
				rt.Run(30.0);
			}
			return;/*
		RayTracer.ExecuteGpu(new Vector3[]{new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)}, new Vector3[]{
			new Vector3(15f, 0f, 0f), new Vector3(15f, 15f, 0f)
		});

		    Console.Read();
		    return;*/
			//var gpu = CudafyHost.GetDevice(eGPUType.OpenCL);
    /*
			CudafyTranslator.Language = eLanguage.OpenCL;
            CudafyModule km = CudafyTranslator.Cudafy(typeof(Program), typeof(RayTracer)); // or = Cudafy(eArchitecture.OpenCL, typeof(add_loop_long));

			// Get the first CUDA device and load the module generated above.
			GPGPU gpu = CudafyHost.GetDevice(eGPUType.OpenCL, 0); // or eGPUType.OpenCL
			gpu.LoadModule(km);

		    float[] a = new float[N];
		    float[] b = new float[N];
		    float[] c = new float[N];

		    float[] gpu_a = gpu.Allocate<float>(a);
		    float[] gpu_b = gpu.Allocate<float>(b);
		    float[] gpu_c = gpu.Allocate<float>(c);

            // Fill the arrays 'a' and 'b' on the CPU
            for (int i = 0; i < N; i++)
            {
                a[i] = (float)i;
                b[i] = 2f * i;
            }

            float[] e = new float[N];
            float[] f = new float[N];
            // Copy the arrays 'a' and 'b' to the GPU
            gpu.CopyToDevice(a, gpu_a);
            gpu.CopyToDevice(b, gpu_b);
		    var sw = Stopwatch.StartNew();
            gpu.Launch(N/4, 4).Calc(gpu_a, gpu_b, gpu_c);

		    var ray = new RayCast();
		    gpu.Launch().CastRays(ref ray, 0, 10);
            sw.Stop();

            gpu.CopyFromDevice(gpu_a, e);
            gpu.CopyFromDevice(gpu_b, f);
            gpu.CopyFromDevice(gpu_c, c);

            gpu.Free(gpu_a);
            gpu.Free(gpu_b);
            gpu.Free(gpu_c);

            for (int i = 0; i < N; i++)
            {
                Console.WriteLine("{0} {1} {2}", e[i], f[i], c[i]);
            }

			Console.WriteLine("Sum of {0} took {1} ms", N, sw.ElapsedMilliseconds);
		    Console.ReadLine();
			return; */
		}
	}


	class MainClass : GameWindow
	{
		public static void MMain(string[] args)
		{
			using(var gw = new MainClass())
			{
				gw.VSync = VSyncMode.On;
				gw.Run(60);
			}
		}

        public MainClass() : base(256, 256) {}

		private Framebuffer _fbo;
		private Texture2D _tex;

		public byte[] PixelBuffer;

		public void ClearPixels(byte r = 0, byte g = 0, byte b = 0)
		{
            PixelBuffer = new byte[Width * Height * 3];
		}

		public void SetPixel(short x, short y, byte r = 255, byte g = 255, byte b = 255)
		{
			if(x < 0 || x >= Width || y < 0 || y >= Height) return;

			int offset = ((y * Width + x) * 3);
			PixelBuffer[offset]   = r;
			PixelBuffer[offset+1] = g;
			PixelBuffer[offset+2] = b;
		}


		public void DrawLine(float aX, float aY, float bX, float bY,
			byte r = 255, byte g = 255, byte b = 255)
		{
			float deltaX = (bX - aX);
			float deltaY = (bY - aY);
			if(Math.Abs(deltaY / deltaX) < 1.0)
			{
				if(aX > bX)
				{
					DrawLine(bX, bY, aX, aY, r, g, b); return;
				}
				for(float x = 0; x <= deltaX; x += 1f)
				{
					SetPixel((short)(aX + x), (short)(aY + x*(deltaY / deltaX)), r, g, b);
				}
			}
			else
			{
				if(aY > bY)
				{
					DrawLine(bX, bY, aX, aY, r, g, b); return;
				}
				for(float y = 0; y <= deltaY; y += 1f)
				{
					SetPixel((short)(aX + y*(deltaX / deltaY)), (short)(aY + y), r, g, b);
				}
			}
		}


		private void DrawCircle(short x, short y, short radius, float resolution = 0.1f, bool shade = false)
		{
			for(double d = 0; d <= Math.PI * 2.0; d+=resolution)
			{
				Vector3 origin = new Vector3(
					(float)(x + (Math.Cos(d - resolution) * radius)),
					(float)(y + (Math.Sin(d - resolution) * radius)),
					                 0f);
				//Vector3 normal = -(new Vector3(x, y, 0f) - origin).Normalized();
				//Vector3 col = _world.LightTrace(origin + normal, normal).Xyz;
				DrawLine(origin.X, origin.Y,
					x + (float)(Math.Cos(d) * radius), y + (float)(Math.Sin(d) * radius), (byte)(255f), (byte)(255f), (byte)(255f));
				//DrawLine(origin.X, origin.Y, origin.X + normal.X * 10f, origin.Y + normal.Y * 10f, 255, 0, 0);
			}
		}

		private Sphere[] _spheres = new Sphere[] {
			new Sphere(){ Origin = new Vector3(250, 250, 0f), Radius = 25f },
			new Sphere(){ Origin = new Vector3(250, 100, 0f), Radius = 55f },
			new Sphere(){ Origin = new Vector3(500, 250, 0f), Radius = 25f }
		};

		public void TraceRay(Ray ray, int depth = 0, int maxdepth = 1000)
		{

			byte c = 255;
			//DrawLine(ray.A.X, ray.A.Y, ray.B.X, ray.B.Y, c,c,c);
			if(depth > maxdepth) return;

			bool b = false;
			for(int j = 0; j < _spheres.Length; j++)
			{

				var v = _spheres[j].TryFor(ray);
				if(v.Item1)
				{
					SetPixel((short)ray.B.X, (short)ray.B.Y);
					b = true;
					foreach(var _r in v.Item2)
					{
						TraceRay(_r, depth + 1, maxdepth);
					}
					break;
				}
				//else
			}
			if(!b)
				TraceRay(new Ray(ray.B, ray.Direction * ray.Magnitude){ IOR = 1f }, depth + 1, maxdepth);
			/*
			float ior = ray.IOR;
			ray = new Ray(ray.B, ray.Direction * ray.Magnitude) { IOR = ior };
			TraceRay(ray);*/
		}

		private static RayWorld _world = new RayWorld() { 
			Spheres = new Sphere[] {
				new Sphere(){ Origin = new Vector3(250, 0f, 250), Radius = 25f, Material = new Material(new Vector3(1f, 0.8f, 0.7f), Vector3.Zero, new Vector3(1f, 0f, 0f), 0.5f, 0.5f, 1.1f)},
				new Sphere(){ Origin = new Vector3(250, 0f, 100), Radius = 55f },
				new Sphere(){ Origin = new Vector3(470, 0f, 250), Radius = 35f, Material = new Material(new Vector3(1f, 0.8f, 0.7f), Vector3.Zero, new Vector3(1f, 0f, 0f), 0.5f, 0.0f, 1.45f) },
				new Sphere(){ Origin = new Vector3(600, 0f, 250), Radius = 40f },
				new Sphere(){ Origin = new Vector3(520, 0f, 350), Radius = 15f, Material = new Material(new Vector3(0.6f, 0.4f, 0.7f), Vector3.Zero, new Vector3(0f, 1f, 0f), 0.5f, 0.5f, 1.5f) },
				new Sphere(){ Origin = new Vector3(480, 0f, 360), Radius = 5f }
			}.ToList(), 
			Lights = new Light[] { 
				new Light(){Origin = new Vector3(350, 0f, 400), 
					Intensity = 1f, Color = Vector4.One}
			}.ToList() };

		private Stopwatch _sw = new Stopwatch();
		private long _castRays = 0;



		public static Vector4 Mix(Vector4 a, Vector4 b, float d)
		{
			return a * d + b * (1f - d);
		}

		public static Vector4 Sky = new Vector4(0.3f, 0.7f, 1f, 1f);

       // [Cudafy]
		public static Vector4 CastRays(ref RayCast source, int depth = 0, int maxdepth = 10, bool draw = false, bool white = false)
		{
			if (depth >= maxdepth)
            {
			    return Sky;
			}

            Incident[] incidents;
            if (!_world.CastRay(ref source, 1000, out incidents))
			{
			    return Sky;
			}

            Vector4 diffuse = _world.LightTrace(incidents[0].Origin, incidents[0].Normal) * new Vector4(incidents[0].Material.DiffuseColor, 1f);
            Vector4 gloss = Vector4.Zero;
			Vector4 refract = Vector4.Zero;

            if (incidents[0].Material.IsReflective)
            {
                var dir = ((incidents[0].Inbound).Reflect(incidents[0].Normal));
                var ray = new RayCast() { Origin = incidents[0].Origin, Step = dir };
				gloss = CastRays(ref ray, depth + 1, maxdepth, draw, white);
                //Console.WriteLine(gloss);
			}

		    const int refractSamples = 16;
            if (!incidents[0].Material.IsOpaque)
			{
                var dir = incidents[0].Inbound.Refract(1.0, incidents[0].Sphere.Material.IOR, incidents[0].Normal).Normalized();

                for (int i = 0; i < 1; i++)
                {
                    var ndir = dir.Spread(0f);
                    var ray = new RayCast() { Origin = incidents[0].Origin - ndir, Step = ndir };

                    Incident[] interiorIncidents;
                    _world.CastRay(ref ray, 1000, out interiorIncidents, new Sphere[] { incidents[0].Sphere }, CastFilter.Include);

                    if(interiorIncidents.Length <= 1) continue;

                    var dir2 = interiorIncidents[1].Inbound.Refract(incidents[0].Sphere.Material.IOR, 1.0, -interiorIncidents[1].Normal).Normalized();
                    var ray2 = new RayCast() { Origin = interiorIncidents[1].Origin, Step = dir2 };

                    float sqrDist = (interiorIncidents[0].Origin - interiorIncidents[1].Origin).LengthFast;

                    var resultColor = CastRays(ref ray2, depth + 1, maxdepth, white);
                    refract += resultColor * new Vector4(incidents[0].Material.VolumeColor, 1f) * (sqrDist / (incidents[0].Sphere.Radius * 2f));
                }
			    refract /= 1;
			}



            Vector4 col = Mix(Mix(gloss, diffuse, incidents[0].Material.Reflectivity), refract, incidents[0].Material.Opacity);

		    return col;
		}

		protected override void OnMouseMove(OpenTK.Input.MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);

			/*ClearPixels();
			var v = new Vector3(Width / 2f, Height / 2f, 0f);
			//Ray ray = new Ray(v, Vector3.Normalize(new Vector3(Mouse.X, Height - Mouse.Y, 0f)-v) * 1f);
			RayCast ray = new RayCast(){Origin = v, Step = Vector3.Normalize(new Vector3(Mouse.X, Height - Mouse.Y, 0f)-v) };

			CastRays(ray, 0, 10);
			/*
			for(int i = 0; i < 10; i++)
			{
				CastResult result;
				if(!_world.CastRay(ray, 1000, out result))
				{
					break;
				}
				Console.WriteLine(result.State);
				if(result.Material.IsReflective && result.State == 1)
				{
					ray = new RayCast(){ Origin = result.EndPoint, Step = ((result.Inbound).Reflect(result.Normal)) };
				}
				else if(!result.Material.IsOpaque)
				{
					ray = new RayCast(){ Origin = result.EndPoint, Step = result.Inbound.Refract(result.State == 1 ? 1.0 : 1.5, result.State == 1 ? 1.5 : 1.0, -result.Normal).Normalized() };
				}
				else
				{
					break;
				}
			}*/

			//_sw.Stop();
			//_castRays++;

			//Title = ((double)_sw.ElapsedMilliseconds / (double)_castRays).ToString();

			/*
			//Console.WriteLine("{0} {1}", ray.A, ray.B);
			for(int i = 0; i < 1000; i++)
			{
				int j = 0; 
				bool collided = false;
				while(!collided && j < _spheres.Length)
					ray = _spheres[j++].TryFor(ray, out collided);

				byte r = collided ? (byte)255 : (byte)0;
				byte gb = collided ? (byte)0 : (byte)255;
				DrawLine(ray.A.X, ray.A.Y, ray.B.X, ray.B.Y, r, gb, gb);

				float ior = ray.IOR;
				ray = new Ray(ray.B, ray.Direction * ray.Magnitude) { IOR = ior };
				Console.WriteLine(" pls: {0} {1}", ior, ray.IOR);
			}* /

			foreach(var sphere in _world.Spheres)
			{
				DrawCircle((short)sphere.Origin.X, (short)sphere.Origin.Y, (short)sphere.Radius, 0.2f);
			}


			foreach(var light in _world.Lights)
			{
				DrawCircle((short)light.Origin.X, (short)light.Origin.Y, 5, 0.2f);
			}*/

			//DrawLine((short)(Width / 2), (short)(Height / 2), (short)Mouse.X, (short)Height - (short)Mouse.Y, (byte)255, (byte)255, (byte)255);
			//Console.WriteLine("{0} {1}", (short)Mouse.X, (short)Mouse.Y);
		}

		private float _angle = 0f;

		public void DrawProjectedLine(float screenX, float screenY, Matrix4 mvp, bool white = false)
		{
			Vector4 screenVec = Vector4.Transform(new Vector4(screenX, screenY, 0f, 1f), mvp);
			Vector4 targetVec = Vector4.Transform(new Vector4(screenX, screenY, 1f, 1f), mvp);
			Vector3 worldVec = screenVec.Xyz / screenVec.W;
			Vector3 normalVec = (targetVec.Xyz / targetVec.W).Normalized();

			//Console.WriteLine("{0} - {1} = {2}", worldVec, normalVec, worldVec + normalVec);
			//DrawLine(worldVec.X, worldVec.Z, worldVec.X + normalVec.X*100f, worldVec.Z + normalVec.Z*100f);

		    var rayCast = new RayCast() {Origin = worldVec, Step = normalVec};
            CastRays(ref rayCast, 0, 15, true, white);
		}

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _tex = new Texture2D();
            _tex.TexImage(1, 1);
            _tex.SetTextureMagFilter(TextureMagFilter.Nearest);
            _tex.SetTextureMinFilter(TextureMinFilter.Nearest);

            /*_world.OnRayTraced += (start, end, color) => {
                DrawLine(start.X, start.Y, end.X, end.Y, (byte)(color.X*byte.MaxValue), (byte)(color.Y*byte.MaxValue), (byte)(color.Z*byte.MaxValue));
            };*/
        }

        private Vector3[,][] _screenField = new Vector3[0,0][];
	    private Matrix4 _inverseProjection, projection;
	    private float _zDepth, _zMult;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PixelBuffer = new byte[Width * Height * 3];

            _screenField = new Vector3[Width, Height][];

            _zDepth = 2000.1f - 0.1f;
            _zMult = 1/_zDepth;
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)Width / Height, 0.1f, 2000.1f);
            _inverseProjection = Matrix4.Invert(projection);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)

                {
                    float _x = x / (float)Width * 2f - 1f;
                    float _y = y / (float)Height * 2f - 1f;
                    Vector4 screenVec = Vector4.Transform(new Vector4(_x, _y, 0f, 1f), _inverseProjection);
                    Vector4 targetVec = Vector4.Transform(new Vector4(_x, _y, 1f, 1f), _inverseProjection);

                    _screenField[x, y] = new[] { screenVec.Xyz / screenVec.W, targetVec.Xyz / targetVec.W };
                }
            }

            //ClearPixels(255, 0, 0);
        }

	    private int x = 0;
	    protected override void OnUpdateFrame(FrameEventArgs e)
	    {
	        base.OnUpdateFrame(e);

	        if (Keyboard[Key.Q])
	            _angle += 0.0025f;
	        else if (Keyboard[Key.E])
	            _angle -= 0.0025f;

	        var c = new Vector3(300f, 0f, 300f);
	        var modelview = Matrix4.LookAt(c, c + new Vector3((float) Math.Cos(_angle), 0f, (float) Math.Sin(_angle)), Vector3.UnitY);
            var _invModelview = Matrix4.Invert(modelview);

	        _sw.Start();
            Parallel.For(0, Width/8, new ParallelOptions(){MaxDegreeOfParallelism = 8}, (i) =>
            {
                RayCast ray = new RayCast();
                for (int y = 0; y < Height; y++)
                {
                    ray.Origin = c;
                    ray.Step = Vector3.Transform(_screenField[x + i, y][1], _invModelview) * _zMult;

                    var col = CastRays(ref ray, 0, 10);
                    SetPixel((short)(x+i), (short)y, (byte)(col.X * 255),
                             (byte)(col.Y * 255), (byte)(col.Z * 255));
                }
            });
	        _sw.Stop();

	        Title = (_sw.ElapsedMilliseconds/((double) (Width/8)*Height)).ToString();
            _sw.Reset();

            x += Width / 8;
            if (x >= Width) x = 0;
            /*
            var mvp = Matrix4.Mult(_inverseProjection, _invModelview);
            DrawProjectedLine(-1f, 0f, mvp, true);
            DrawProjectedLine(1f, 0f, mvp, true);

	        foreach (var sphere in _world.Spheres)
	        {
	            DrawCircle((short) sphere.Origin.X, (short) sphere.Origin.Z, (short) sphere.Radius, 0.2f);
	        }


	        foreach (var light in _world.Lights)
	        {
	            DrawCircle((short) light.Origin.X, (short) light.Origin.Z, 5, 0.2f);
	        }*/


	        _tex.Bind();
	        _tex.TexImage(Width, Height, PixelBuffer, PixelInternalFormat.Rgb, PixelFormat.Rgb, PixelType.UnsignedByte);

	    }

	    protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);


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
