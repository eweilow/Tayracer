using System;
using OpenTK;

namespace Tayracer
{
	public interface ITraceable
	{
		Vector3 GetNormal(Vector3 origin);
		bool Intersection(Ray ray);
		Tuple<bool, Ray[]> TryFor(Ray ray);
	}

	public static class VExt 
	{
		public static Vector3 Reflect(this Vector3 vec, Vector3 normal)
		{
			return vec - Vector3.Dot(vec, normal)*2f*normal;
		}

		public static Vector3 Refract(this Vector3 vec, double n1, double n2, Vector3 normal)
		{
			double eta = n1 / n2;
			double c1 = -Vector3.Dot(vec, normal);
			double cs2 = 1 - eta * eta * (1 - c1 * c1);

			if(cs2 < 0) return vec;

			return (float)eta * vec + (float)(eta * c1 - Math.Sqrt(cs2)) * normal;
		}

        private static Random _rnd = new Random();
        public static Vector3 Spread(this Vector3 vec, float amount = 1f)
        {
            vec.Normalize();
            var vec1 = Vector3.Cross(new Vector3(vec.Z, vec.X, vec.Y), vec);
            var vec2 = Vector3.Cross(vec, vec1);

            double rnd = _rnd.NextDouble()*Math.PI;
            return Vector3.Normalize(vec + (float)Math.Cos(rnd) * vec1 * amount + (float)Math.Sin(rnd) * vec2 * amount) * vec.Length;
        }
	}

	public class Sphere : ITraceable
	{
	    public Material Material = Material.Diffuse;
		public Vector3 Origin;

		private float _radius, _sqrRadius;
		public float Radius { get { return _radius; } set { _radius = value; _sqrRadius = value * value; }}

		public Vector3 GetNormal(Vector3 origin)
		{
			return Vector3.NormalizeFast(origin - Origin);
		}

		public bool Intersection(Ray ray)
		{
			return (Origin - ray.A).LengthSquared <= _sqrRadius || (Origin - ray.B).LengthSquared <= _sqrRadius;
		}

	    public Tuple<bool, Ray[]> TryFor(Ray ray)
	    {
	        throw new NotImplementedException();
	    }

	    public int IntersectsShell(Ray ray)
		{
			float aDist = (Origin - ray.A).LengthSquared;
			float bDist = (Origin - ray.B).LengthSquared;

			if(aDist <= _sqrRadius && bDist > _sqrRadius)
				return -1;
			else if(bDist <= _sqrRadius && aDist > _sqrRadius)
				return 1;
			return 0;
		}

        public int Inside(RayCast ray)
        {
            return (ray.Origin - Origin).LengthSquared < _sqrRadius ? -1 : 1;
        }

		/// <summary>
		/// Gets the intersection. 
		/// -1 is going out, 1 is going in, 0 is nothing
		/// </summary>
		/// <returns>int</returns>
		/// <param name="ray">Ray.</param>
		public Vector3[] GetIntersection(ref RayCast ray)
		{
		    float A, B, C;
            float D = Discriminant(ref ray, out A, out B, out C);


            if (D < 0) return new Vector3[0];

		    float DSqrt = (float) Math.Sqrt(D);
		    float A2 = 2f*A;

            float t1 = (-B - DSqrt) / A2;
            if (t1 < 0) return new Vector3[0];

            var sol1 = new Vector3(ray.A.X * (1 - t1) + t1 * ray.B.X, ray.A.Y * (1 - t1) + t1 * ray.B.Y, ray.A.Z * (1 - t1) + t1 * ray.B.Z);
            
            if(D == 0)
                return new [] { sol1 };

            float t2 = (-B + DSqrt) / A2;
            if (t2 < 0) return new Vector3[0];
            var sol2 = new Vector3(ray.A.X * (1 - t2) + t2 * ray.B.X, ray.A.Y * (1 - t2) + t2 * ray.B.Y, ray.A.Z * (1 - t2) + t2 * ray.B.Z);

            if (Math.Abs(t1 - 0.5f) < Math.Abs(t2 - 0.5f))
            {
                return new [] { sol1, sol2 };
            }

            return new [] { sol2, sol1 };
            /*
		    var x0 = Origin;
		    var x1 = ray.Origin;
		    var x2 = ray.Origin + ray.Step * 1000f;

		    var cross = Vector3.Cross((x2 - x1), (x1 - x0)).LengthSquared;
		    float closestDist = cross / (x2 - x1).LengthSquared;
            
            if (closestDist <= Radius)
            {
                float dist = (Origin - ray.A).LengthSquared;
                if (dist > _sqrRadius) return 1;
                return 1;
            }

		    return 0;*/
            /*
			float aDist = (Origin - ray.A).LengthSquared;
			float bDist = (Origin - ray.B).LengthSquared;

			if(aDist <= _sqrRadius && bDist > _sqrRadius)
				return -1;
			else if(bDist <= _sqrRadius && aDist > _sqrRadius)
				return 1;
			return 0;*/
		}

        public float Discriminant(ref RayCast ray, out float A, out float B, out float C)
        {
            float cx = Origin.X;
            float cy = Origin.Y;
            float cz = Origin.Z;

            float px = ray.A.X;
            float py = ray.A.Y;
            float pz = ray.A.Z;

            float vx = ray.B.X - px;
            float vy = ray.B.Y - py;
            float vz = ray.B.Z - pz;

            A = vx * vx + vy * vy + vz * vz;
            B = 2f * (px * vx + py * vy + pz * vz - vx * cx - vy * cy - vz * cz);
            C = px * px - 2 * px * cx + cx * cx + py * py - 2 * py * cy + cy * cy +
                       pz * pz - 2 * pz * cz + cz * cz - Radius * Radius;

            return B * B - 4 * A * C;
        }

		/*public Tuple<bool, Ray[]> TryFor(Ray ray)
		{
			int inter = IntersectsShell(ray); //-1 is going out, 1 is going in, 0 is nothing
			var n = Vector3.Normalize(ray.B - Origin);
			if(inter == 1)
			{
				return new Tuple<bool, Ray[]>(true, new Ray[] { 
					new Ray(ray.B, ray.Direction.Refract(1.0, this.IOR, n)) { IOR = this.IOR },
					new Ray(ray.B, ray.Direction.Reflect(n)) { IOR = 1f }
				});
			}
			else if(inter == -1)
			{
				return new Tuple<bool, Ray[]>(true, new Ray[]{ new Ray(ray.B, ray.Direction.Refract(this.IOR, 1.0, -n)) { IOR = 1f } });
			}
			return new Tuple<bool, Ray[]>(false, null); 
		}*/

		/*public bool TryCast(RayCast ray)
		{

		}*/
	}
}

