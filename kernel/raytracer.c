struct Matrix4x4
{
	float a; float e; float i; float m;
	float b; float f; float j; float n; 
	float c; float g; float k; float o; 
	float d; float h; float l; float p; 
};

void applyMat(struct Matrix4x4*, float3*);
void applyMat(struct Matrix4x4* matrix, float3* vector)
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

    float2 f = { -b - sqrt(det), -b + sqrt(det) };
	
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
	float3 checker = {f,f,f};
	return checker;
}

struct Ray
{
	float3 source;
	float3 direction;
	short mask;
};

struct CastResult
{
	float3 incident;
	float3 inbound;
	float3 normal;
	float3 color;
	int cont;
};

struct RayCast
{
	float2 depth;
	int id;
};

struct RayCast castRay(float4* spheres, int* sphereCount, struct CastResult* result, struct Ray* ray)
{
	struct RayCast cast;
	cast.depth = (float2)(-1, -1);
	for(int j = 0; j < *sphereCount; j++)
	{
		float4* sphere = &spheres[j];
		float2 res = findInter(ray->source - sphere->xyz, ray->direction, sphere->w);

		if(res.x < 0) { continue; }
		if(cast.depth.x < 0 || res.x < cast.depth.x) { cast.depth.x = res.x; cast.id = j; }
	}
	return cast;
}

void castRays(float4* spheres, int* sphereCount, struct CastResult* results, struct Ray* rays, int count)
{
	for(int i = 0; i < count; i++)
	{
		if(results[i].cont == 0) continue;

		struct RayCast cast = castRay(spheres, sphereCount, &results[i], &rays[i]);
		/*
		float4* closestSphere;
		for(int j = 0; j < *sphereCount; j++)
		{
			float4 sphere = spheres[j];
			float2 res = findInter(rays[i].source - sphere.xyz, rays[i].direction, sphere.w);
			if(res.x < 0) { continue; }
			if(depth < 0) depth = res.x;
			else if(res.x < depth) { depth = res.x; closestSphere = &sphere; }
		}*/

		if(cast.depth.x < 0 || cast.id < 0) { results[i].cont = 0; continue; }
		
		results[i].inbound = rays[i].direction;
		results[i].incident = rays[i].source + rays[i].direction * cast.depth.x;
		results[i].normal = normalize(spheres[cast.id].xyz - results[i].incident);
		results[i].color = results[i].normal;
	}
}

void applyRay(struct CastResult* result, struct Ray* stack, int* index, float4* spheres, int* sphereCount)
{	
	/* Reflection */
	stack[*index].source = result->incident;
	stack[*index].direction = reflect(result->inbound, result->normal);

	/* Refraction */
	stack[*index+1].source = result->incident;
	stack[*index+1].direction = refract(result->inbound, result->normal, 1.45);

	//Use this to cast 2 rays
}

#define BOUNCES 2 //
#define BRANCHES 2 //One for reflection and one for refraction

#define STACK_SIZE BOUNCES*BRANCHES
float3 idea(struct Ray source, float4* spheres, int* sphereCount)
{
	struct Ray stack			[STACK_SIZE];
	struct CastResult results	[STACK_SIZE];

	stack[0] = source;

	for(int i = 0; i < STACK_SIZE; i++) results[i].cont = 1;

	float3 col = (float3)(0,0,0);
	for(int max = 1; max <= BOUNCES; max++)
	{
		//int max = 1;
		castRays(spheres, sphereCount, results, stack, max);
		for(int i = 0; i < max; i++)
		{
			struct CastResult rayResult = results[i];
			if(rayResult.cont == 0) continue;

			applyRay(&rayResult, stack, &i, spheres, sphereCount);
			col = col + rayResult.color;
		}
	}

	
	return col;
}

kernel void MatrixMultiply(
	const int w,
	const int h,
	const float4 origin,
	const struct Matrix4x4 matrix,
	global write_only float* output
)
{
	size_t width = w;
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);
	
	float3 ori = origin.xyz;

	float3 target = (float3)(x,y,1);

    int c = 4;
    float4 spheres[4] = { { 5, 0, 5, 1 }, { 15, 0, 5, 1 }, { 5, 0, 15, 1}, { 25, 0, 5, 1 }};
    //float4 spherecolors[4] = { { 1, 0.2, 0.1, 0.5 }, { 0.1, 1, 0.1, 0.7 },{ 1.0, 0.1, 0.1, 0.95 },{ 0.1, 0.2, 1.0, 0.5 }};

	//struct Matrix4x4 mat[1] = { matrix[0] };

	//struct Matrix4x4 mat[1];
	applyMat(&matrix, &target);


	struct Ray ray;
	ray.source = ori;
	ray.direction = normalize(target);
	ray.mask = -1;
	
	//float3* col = trace(spheres, spherecolors, &c, 0, ray);

	//struct Ray ray;
	float3 sta = idea(ray, spheres, &c);

	int index = (x + w * y) * 3;
	output[index]   = sta.x;
	output[index+1] = sta.y;
	output[index+2] = sta.z;
}
