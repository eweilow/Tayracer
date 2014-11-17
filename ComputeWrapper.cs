using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using Cloo;
using System.Runtime.InteropServices;
using OpenTK.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Tayracer
{
	public class ComputeWrapper : IDisposable
	{
		[DllImport("opengl32.dll")]
		extern static IntPtr wglGetCurrentDC();
		public static readonly bool CanInterop;

		static ComputeWrapper()
		{
			try 
			{
				wglGetCurrentDC();
				CanInterop = true;
			}
			catch(Exception)
			{
				CanInterop = false;
			}
		}

		public ComputeContext Context;
		public ComputeProgram Program;
		public ComputeKernel Kernel;
		public ComputeEventList EventList;
		public ComputeCommandQueue Commands;

		public void Dispose()
		{
			if(Context != null) Context.Dispose();
			if(Program != null) Program.Dispose();
			if(Kernel != null) Kernel.Dispose();
			if(Commands != null) Commands.Dispose();
		}

		private List<ComputeDevice> _devices;

		public void CreateContext(ComputePlatform platform, ComputeDevice device, bool interop = true)
		{
			CreateContext(platform, new List<ComputeDevice>(){ device }, interop);
		}

		public void CreateContext(ComputePlatform platform, List<ComputeDevice> devices = null, bool interop = true)
		{
			_devices = devices ?? new List<ComputeDevice>(){ platform.Devices[0] };
			if(interop && CanInterop)
			{
				var curDC = wglGetCurrentDC();
				var ctx = (IGraphicsContextInternal)GraphicsContext.CurrentContext;
				var rawContextHandle = ctx.Context.Handle;
				var p1 = new ComputeContextProperty(ComputeContextPropertyName.CL_GL_CONTEXT_KHR, rawContextHandle);
				var p2 = new ComputeContextProperty(ComputeContextPropertyName.CL_WGL_HDC_KHR, curDC);
				var p3 = new ComputeContextProperty(ComputeContextPropertyName.Platform, platform.Handle.Value); 
				var props = new List<ComputeContextProperty>() { p1, p2, p3 };

				var properties = new ComputeContextPropertyList(props);
				Context = new ComputeContext(_devices, properties, null, IntPtr.Zero);
			}
			else
			{
				var properties = new ComputeContextPropertyList(platform);
				Context = new ComputeContext(_devices, properties, null, IntPtr.Zero);
			}
		}

		public bool LoadAndBuildKernel(string file, string kernel = "main", params string[] args)
		{
			var source = File.ReadAllText(file);
            Program = new ComputeProgram(Context, new[] { args.Aggregate((s, s1) => s + "\n" + s1), source });
			try
			{
				Program.Build(_devices, null, null, IntPtr.Zero);
			}
			catch(BuildProgramFailureComputeException bex)
			{
				Console.WriteLine("Build error:");
				Console.WriteLine(Program.GetBuildLog(_devices[0]));
				return false;
			}

			Kernel = Program.CreateKernel("MatrixMultiply");
			EventList = new ComputeEventList();
			Commands = new ComputeCommandQueue(Context, _devices[0], ComputeCommandQueueFlags.None);
			return true;
		}
	}
}

