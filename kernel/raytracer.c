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
	vector->z = (vector->x * matrix->i + vector->y * matrix->j + vector->z * matrix->k + matrix->l) * w;

	vector->x = x;
	vector->y = y;
}

float2 findInter(float3, float3, float);
float2 findInter(float3 toSphere, float3 rayDir, float r)
{
    float b = dot(toSphere, rayDir);
    float det = b*b - (toSphere.x*toSphere.x + toSphere.y*toSphere.y + toSphere.z*toSphere.z) + r*r;

    if(det < 0) return -1;

    float sqr = sqrt(det);	
	return (float2)(-b-sqr, -b+sqr);
}

float findInterPlane(float3, float3, float3, float3);
float findInterPlane(float3 pNormal, float3 pPos, float3 rayPos, float3 rayDir)
{
	float dotProduct = dot(rayDir,pNormal);
	if ( dotProduct == 0){
		return -1;
	}
	float t1 = dot(pNormal, pPos-rayPos) / dotProduct;
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
	int mask;
};

struct CastResult
{
	float3 incident;
	float3 backIncident;
	float3 normal;
	float3 color;
	int mask;
};

struct RayCast
{
	float2 depth;
	int id;
};

struct RayCast castRay(float4* spheres, int* sphereCount, struct CastResult* result, struct Ray* ray)
{
	struct RayCast cast;
	cast.id = -1;
	cast.depth = (float2)(-1, -1);
	for(int j = 0; j < *sphereCount; j++)
	{
		if(j == ray->mask) continue;
		float2 res = findInter(ray->source - spheres[j].xyz, ray->direction, spheres[j].w);

		if(res.x < 0) { continue; }
		if(cast.depth.x < 0 || res.x <= cast.depth.x) { cast.depth = res; cast.id = j; }
	}
	result->incident = ray->source + ray->direction * cast.depth.x;
	result->backIncident = ray->source + ray->direction * cast.depth.y;
	result->normal = normalize(result->incident - spheres[cast.id].xyz);
	result->color = clamp(result->normal, 0.0f, 1.0f);
	result->mask = cast.id;

	return cast;
}

#define BOUNCES 128 //Amount of bounces
#define BRANCHES 1 //One for reflection and one for refraction
#define STACK_SIZE BOUNCES*BRANCHES

void applyRay(struct CastResult* result, struct Ray* ray, struct Ray* writeStack, int* index, float4* spheres, int* sphereCount)
{	
	int readIndex  = *index;
	int writeIndex = readIndex * BRANCHES;

	/* Reflection */
	writeStack[writeIndex].source = result->incident;
	writeStack[writeIndex].direction = reflect(ray->direction, result->normal);
	writeStack[writeIndex].mask = result->mask;

	//struct Ray refractionRay;
	//
	//writeStack[writeIndex].source = result->incident;
	//writeStack[writeIndex].direction = refract(ray->direction, result->normal, 1.4);
	//writeStack[writeIndex].mask = result->mask;

	///* Refraction */
	//stack[*index+1].source = result->incident;
	//stack[*index+1].direction = refract(stack[*index+1].direction, result->normal, 1.45);

	//Use this to cast 2 rays
}


float3 idea(struct Ray source, float4* spheres, int* sphereCount)
{
	struct Ray write_stack		[STACK_SIZE];
	struct Ray read_stack		[STACK_SIZE];
	uchar write_skip 			[STACK_SIZE];
	uchar read_skip 			[STACK_SIZE];
	//struct CastResult results	[STACK_SIZE];

	read_stack[0] = source;

	for(int i = 0; i < STACK_SIZE; i++) { read_skip[i] = 1; }

	float fac = 1.0;

	int power = BRANCHES;
	float3 col = (float3)(0,0,0);
	for(int max = 1; max <= BOUNCES; max++) //FOR EACH BOUNCE DEPTH
	{
		for(int i = 0; i < power; i++) //FOR EACH RAY PER BOUNCE DEPTH
		{		
			for(int j = i*BRANCHES; j < i*BRANCHES + BRANCHES; j++)
			{
				write_skip[i] = read_skip[i];
			}

			if(read_skip[i] == 0) { continue; }

			struct Ray *ray = &read_stack[i];

			struct CastResult rayResult;
			struct RayCast cast = castRay(spheres, sphereCount, &rayResult, ray); //Cast a ray towards the scene.
			if(cast.id < 0) 
			{ 
				for(int j = i*BRANCHES; j < i*BRANCHES + BRANCHES; j++)
				{
					write_skip[i] = 0;
				}
				continue;
			} //If the ray doesnt hit anything

			applyRay(&rayResult, ray, write_stack, &i, spheres, sphereCount);
			col = col * (1.0-fac) + rayResult.color * fac;
		}
		power = power * power;
		for(int i = 0; i < STACK_SIZE; i++) {
			read_stack[i] = write_stack[i];
			read_skip[i]  = write_skip[i];
		}
		fac *= 0.5;
	}
	return col;
}

#ifdef INTEROP

uint4 toUint(float4 f)
{
	return (uint4)(f.x*255, f.y*255, f.z*255, f.w*255);
}

kernel void MatrixMultiply(
	const int w,
	const int h,
	const float4 origin,
	const struct Matrix4x4 matrix,
	__write_only image2d_t output
)
#else
kernel void MatrixMultiply(
	const int w,
	const int h,
	const float4 origin,
	const struct Matrix4x4 matrix,
	global write_only float* output
)
#endif
{
	size_t width = w;
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);
	
	float3 ori = origin.xyz;

	float3 target = (float3)(x,y,1);

    const int c = 4;
    const float4 spheres[4] = { { 5, 0, 5, 1 }, { 8, 0, 5, 1 }, { 11, 0, 5, 1}, { 5, 0, 15, 1 }};

	applyMat(&matrix, &target);

	struct Ray ray;
	ray.source = ori;
	ray.direction = normalize(target);
	ray.mask = -1;

	float3 sta = idea(ray, spheres, &c);

	float4 f = (float4)(sta,1);

#ifdef INTEROP
	write_imagef(output, (int2)(x,y), f);
#else
	int index = (x + w * y) * 4;
	//output[index] = f;
	output[index] = sta.x;
	output[index+1] = sta.y;
	output[index+2] = sta.z;
#endif
}
