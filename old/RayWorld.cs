using System;
using OpenTK;
using System.Collections.Generic;

namespace Tayracer
{
	public class Camera 
	{
		private Matrix4 _mvp, _invMvp;

		public Matrix4 Matrix { get { return _mvp; } set { _mvp = value; _invMvp = Matrix4.Invert(value); }}
		public Matrix4 InverseMatrix { get { return _invMvp; } set { _invMvp = value; _mvp = Matrix4.Invert(value); }}

		public Vector2 SensorSize;
	}

	public struct RayCast 
	{
		public Vector3 Origin;

		public int SteppedSteps;
		public Vector3 Step;

		public Vector3 A { get { return Origin + Step * SteppedSteps; }}
		public Vector3 B { get { return Origin + Step * (SteppedSteps + 1); }}

		public void DoStep()
		{
			SteppedSteps = SteppedSteps + 1;
		}
	}

	public struct Material
	{
		public static Material Diffuse = new Material(Vector3.One, Vector3.Zero, Vector3.One, 0f, 1f);
		public static Material Glossy = new Material(Vector3.One, Vector3.Zero, Vector3.One, 1f, 1f);
        public static Material Transparent = new Material(Vector3.One, Vector3.Zero, Vector3.UnitX, 0f, 0f);

        public static Material Air = new Material(Vector3.One, Vector3.Zero, Vector3.UnitX, 0f, 0f);

	    public readonly float IOR;
		public readonly float Opacity;
		public readonly float Reflectivity;
		public readonly Vector3 Transmission;
		public readonly Vector3 VolumeColor;
		public readonly Vector3 DiffuseColor;

		public readonly bool IsOpaque;
		public readonly bool IsReflective;
		public readonly bool IsTransmissive;

		public Material(Vector3 diffuse, Vector3 transmission, Vector3 volume, float reflectivity = 0f, float opacity = 1f, float ior = 1f)
		{
			DiffuseColor = diffuse;
			VolumeColor = volume;
			Opacity = opacity;
			Reflectivity = reflectivity;
			Transmission = transmission;
		    IOR = ior;

			IsOpaque = opacity >= 1f;
			IsReflective = reflectivity > 0f;
			IsTransmissive = Transmission.LengthSquared > 0f;
		}
	}

	public struct CastResult
	{
		public Vector3 Inbound;
		public Vector3 Normal;
		public Vector3 StartPoint;
		public Vector3[] Incidents;
		public Material Material;
		public int Depth;
		public int State;
	}

    public struct Incident
    {
        public Vector3 Inbound;
        public Vector3 Normal;
        public Vector3 Source;
        public Vector3 Origin;
        public Material Material;
        public Sphere Sphere;
        public int State;
        public int Depth;
    }

	public struct Light
	{
		public Vector3 Origin;
		public float Intensity;
		public Vector4 Color;
	}

    public enum CastFilter
    {
        Exclude,
        Include
    }

	public delegate void RayTraceDelegate(Vector3 start, Vector3 end, Vector3 color);

	public class RayWorld
	{
		public event RayTraceDelegate OnRayTraced;

		public List<Sphere> Spheres = new List<Sphere>();
		public List<Light> Lights = new List<Light>();

		public Vector4 LightTrace(Vector3 origin, Vector3 normal, Sphere sphere = null)
		{
			Vector4 sum = Vector4.Zero;
			foreach(var light in Lights)
			{
				var step = (light.Origin - origin);
				var dist = step.LengthFast;
				var D = step.Normalized();
				Incident[] incidents;
			    var ray = new RayCast() {Origin = origin + normal*0.1f, Step = D};
				if(!CastRay(ref ray, 1000, out incidents))
				{
					sum += light.Color * light.Intensity * MathHelper.Clamp(Vector3.Dot(D, normal), 0f, 1f);
				}
			}
			return sum;
		}

        public bool CastRay(ref RayCast ray, int maxdepth, out Incident[] result, Sphere[] filterSpheres = null, CastFilter filter = CastFilter.Exclude)
        {
            result = null;
		    {
		        float _mDist = float.MaxValue;

		        ray.Origin += ray.Step*0.1f;
				foreach(var sphere in filter==CastFilter.Include&&filterSpheres!=null?filterSpheres:Spheres.ToArray())
				{
                    bool _continue = false;
                    if (filter == CastFilter.Exclude && filterSpheres != null)
                    {
                        foreach (var sph in filterSpheres)
                        {
                            if (sph == sphere)
                                _continue = true;
                        }
                    }
                    if(_continue) continue;

                    var intersections = sphere.GetIntersection(ref ray);
                    if (intersections.Length > 0)
                    {
                        float d = (ray.Origin - intersections[0]).LengthSquared;
                        if (d < _mDist)
                        {
                            _mDist = d;
                            var insideStart = sphere.Inside(ray);
                            if (intersections.Length == 1)
                            {
                                result = new[] { new Incident() { Sphere = sphere, State = insideStart, Source = ray.Origin, Origin = intersections[0], Normal = sphere.GetNormal(intersections[0]), Inbound = ray.Step, Depth = ray.SteppedSteps, Material = sphere.Material } };
                                //result = new CastResult() { State = insideStart, StartPoint = ray.Origin, Inbound = ray.Step, Normal = sphere.GetNormal(ray.B), Incidents = intersections, Depth = ray.SteppedSteps, Material = Material.Transparent };

                            }
                            if (intersections.Length > 0)
                            {
                                result = new[] { 
                            new Incident() { Sphere = sphere, State = insideStart, Source = ray.Origin, Origin = intersections[0], Normal = sphere.GetNormal(intersections[0]), Inbound = ray.Step, Depth = ray.SteppedSteps, Material = sphere.Material },
                            new Incident() { Sphere = sphere, State = insideStart, Source = ray.Origin, Origin = intersections[1], Normal = sphere.GetNormal(intersections[1]), Inbound = ray.Step, Depth = ray.SteppedSteps, Material = sphere.Material }
                        };
                            }
                        }
                    }
				}
			}
            if (result != null)
                return true;
            result = new [] { new Incident() { State = 0, Source = ray.Origin, Depth = ray.SteppedSteps, Material = Material.Air } };
			return false;
		}
	}
}

