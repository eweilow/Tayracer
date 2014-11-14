struct Matrix4x4
{
	float a; float e; float i; float m;
	float b; float f; float j; float n; 
	float c; float g; float k; float o; 
	float d; float h; float l; float p; 
};

void applyMat(global struct Matrix4x4*, float3*);
void applyMat(global struct Matrix4x4* matrix, float3* vector)
{
	float w = 1.0 / (vector->x * matrix->m + vector->y * matrix->n + vector->z * matrix->o + matrix->p);
	float x = (vector->x * matrix->a + vector->y * matrix->b + vector->z * matrix->c + matrix->d) * w;
	float y = (vector->x * matrix->e + vector->y * matrix->f + vector->z * matrix->g + matrix->h) * w;
	float z = (vector->x * matrix->i + vector->y * matrix->j + vector->z * matrix->k + matrix->l) * w;

	vector->x = x;
	vector->y = y;
	vector->z = z;
}

float2 findInter(float3, float3, float);
float2 findInter(float3 toSphere, float3 rayDir, float r)
{
    float b = dot(toSphere, rayDir);
    float det = b*b - (toSphere.x*toSphere.x + toSphere.y*toSphere.y + toSphere.z*toSphere.z) + r*r;

    if(det < 0) return -1;

    float sqr = sqrt(det);
    float2 f = { -b - sqr, -b + sqr };
	
	return f;
}

float findInterPlane(float3, float3, float3, float3);
float findInterPlane(float3 pNormal, float3 pPos, float3 rayPos, float3 rayDir)
{
	float dotProduct = dot(rayDir,pNormal);
	if ( dotProduct == 0){
		return -1;
	}
	float t1 = dot(pNormal,pPos-rayPos) / dotProduct ;

	return t1;
}

float3 reflect(float3, float3);
float3 reflect(float3 V, float3 N){
	return V - 2.0f * dot( V, N ) * N;
}

float3 refract(float3, float3, float);
float3 refract(float3 V, float3 N, float refrIndex)
{
	float cosI = -dot( N, V );
	float cosT2 = 1.0f - refrIndex * refrIndex * (1.0f - cosI * cosI);
	return (refrIndex * V) + (refrIndex * cosI - sqrt( cosT2 )) * N;
}

#define MAX_STEPS 0

float3 grid(float3);
float3 grid(float3 pos)
{
	float w = floor(pos.x) + floor(pos.z);
	float f = fmod(w,2) < 1 ? 0.3 : 1;
	//float f = (fmod(floor(pos.x) + floor(pos.z), 2) < 1) ? 0.3 : 1;
	float3 checker = {f,f,f};
	return checker;
}

struct Ray //24 + 24 + 8 + 8 = 64
{
	float3 source;
	float3 target;
	short skip;
};

void refractiveTrace(float3*, float3*, float4, float4, float3, float3, float, float3);
void refractiveTrace(float3* out, float3* outTarg, float4 sphere, float4 sphereCol, float3 incident, float3 normal, float IOR, float3 inbound)
{
	float3 refr = normalize(refract(inbound, normal, 1/IOR));

	float3 toSp = (incident - sphere.xyz);
	float2 f = findInter(toSp, refr, sphere.w);


	float3 incidentOut = refr * f.y + incident;
	float3 normalOut = normalize(incidentOut - sphere.xyz);
	float3 refrOut = refract(refr, -normalOut, IOR);

	*out = incidentOut;
	*outTarg = refrOut;
}

float3* trace(float4*, float4*, int*, ushort steps, struct Ray* ray);
float3* trace(float4* spheres, float4* cols, int* c, ushort steps, struct Ray* ray)
{
	float depth = -1;
	float3 col[1] = {0,0,0};

	float3 targ = normalize(ray->target - ray->source);
	for(int i = 0; i < *c; i++)
	{
		if(i == ray->skip) continue;

		float4 sp = spheres[i];

		float3 sphere_pos = sp.xyz;
		float3 toSp = (ray->source - sphere_pos);
		if(dot(toSp, targ) > 0) continue;

		float4 spcol = cols[i];
		float sphere_radius = sp.w;

		float2 f = findInter(toSp, targ, sphere_radius);
		if(f.x >= 0) 
		{
			float3 incident = targ * f.x + ray->source;
			float3 normal = normalize(incident - sphere_pos);

			if(depth < 0) depth = f.x;
			else if(f.x >= depth) continue;
			else depth = f.x;

			if(steps < MAX_STEPS) { 
			//refractiveTrace(float3*, float3*, float4, float4, float3, float3, float, float3);
				float3 refrOut;
				float3 refrTarg;

				refractiveTrace(&refrOut, &refrTarg, sp, spcol, incident, normal, 1.5, targ);

				//float3 refrOut = incident;
				float3 reflTarg = reflect(targ, normal);
		

				struct Ray reflRay[1];
				reflRay->source = incident;
				reflRay->target = incident+reflTarg;
				reflRay->skip = i;

				struct Ray refrRay[1];
				refrRay->source = refrOut;
				refrRay->target = refrOut+refrTarg;
				refrRay->skip = i;

				float3* reflCol = trace(spheres, cols, c, steps+1, reflRay);
				float3* refrCol = trace(spheres, cols, c, steps+1, refrRay);

				float3 spCol = spcol.xyz;

				float fac = spcol.w;
				float oneFac = 1.0 - fac;
			 
				col->x = spCol.x * oneFac + (refrCol->x * 0.5 + refrCol->x * 0.5) * fac;
				col->y = spCol.y * oneFac + (refrCol->y * 0.5 + refrCol->y * 0.5) * fac;
				col->z = spCol.z * oneFac + (refrCol->z * 0.5 + refrCol->z * 0.5) * fac;
			}
			else
			{
				*col = grid(incident);
			}
		}
	}

	if(ray->skip == -2) return col;

	float3 pnormal = { 0, 1, 0 };
	float3 ppos = { 0, -1, 0 };

	float fPlane = findInterPlane(pnormal, ppos, ray->source, targ);
	if(fPlane >= 0) 
	{
		float3 incident = targ * fPlane + ray->source;
		float3 normal = pnormal;

		if(depth < 0) depth = fPlane;
		else if(fPlane >= depth) return col;
		else depth = fPlane;

		float3 tex = grid(incident);
		const float planeFac = 0.5;
		const float onePlaneFac = 1.0 - planeFac;

		if(steps < MAX_STEPS) { 
			float3 refl = reflect(targ, normal);

			struct Ray newRay[1];
			newRay->source = incident;
			newRay->target = incident+refl;
			newRay->skip = -2;

			float3* reflCol = trace(spheres, cols, c, steps+1, newRay);

			col->x = tex.x * onePlaneFac + reflCol->x * planeFac;
			col->y = tex.y * onePlaneFac + reflCol->y * planeFac;
			col->z = tex.z * onePlaneFac + reflCol->z * planeFac;
		}
		else
		{
			col->x = tex.x;
            col->y = tex.y;
            col->z = tex.z;
		}
	}
	return col;
}

kernel void MatrixMultiply(
	const int w,
	const int h,
	const float4 origin,
	global struct Matrix4x4* matrix,
	global write_only float* output
)
{
	size_t width = w;
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);

	float3 ori = origin.xyz;

	float3 target = { x, y, 1.0 };

    int c = 4;
    float4 spheres[4] = { { 5, 0, 5, 1 }, { 15, 0, 5, 1 }, { 5, 0, 15, 1}, { 25, 0, 5, 1 }};
    float4 spherecolors[4] = { { 1, 0.2, 0.1, 0.5 }, { 0.1, 1, 0.1, 0.7 },{ 1.0, 0.1, 0.1, 0.95 },{ 0.1, 0.2, 1.0, 0.5 }};

	//struct Matrix4x4 mat[1] = { matrix[0] };

	//struct Matrix4x4 mat[1];
	applyMat(matrix, &target);


	struct Ray ray[1];
	ray->source = ori;
	ray->target = target;
	ray->skip = -1;

	float3* col = trace(spheres, spherecolors, &c, 0, ray);

	int index = (x + w * y) * 3;
	output[index]   = col->x;
	output[index+1] = col->y;
	output[index+2] = col->z;
}
