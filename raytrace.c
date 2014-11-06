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

float findInter(float3, float3, float);
float findInter(float3 toSphere, float3 rayDir, float r)
{
    float b = -dot(toSphere, rayDir);
    float det = b*b - (toSphere.x*toSphere.x + toSphere.y*toSphere.y + toSphere.z*toSphere.z) + r*r;

    if(det < 0) return -1;

    return b - sqrt(det);
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

#define MAX_STEPS 1

float4 trace(float4*, int*, int, int, float3, float3);
float4 trace(float4* spheres, int* c, int skip, int steps, float3 source, float3 target)
{
    //const float4 lights[1] = { {0,0,0,0} };
    //const float3 lightsCol[1] = { {1,0,0} };
	float4 col;
	col.x = 0; col.y = 0; col.z = 0; col.w = -1;

	for(int i = 0; i < *c; i++)
	{
		if(i == skip) continue;

		float4* sp = &spheres[i];

		float3 sphere_pos = sp->xyz;
		float sphere_radius = sp->w;

		float3 toSp = (source - sphere_pos);
		float3 targ = normalize(target - source);
		float f = findInter(toSp, targ, sphere_radius);
		if(f >= 0) 
		{
			float3 incident = targ * f + source;
			float3 normal = normalize(incident - sphere_pos);

			if(steps < MAX_STEPS) { 
				float3 refl = reflect(targ, normal);
				float4 reflCol = trace(spheres, c, i, steps+1, incident, incident + refl);

				col.x = normal.x * 0.6 + reflCol.x * 0.4;
				col.y = normal.y * 0.6 + reflCol.y * 0.4;
				col.z = normal.z * 0.6 + reflCol.z * 0.4;
			}
			else
			{
				col.x = normal.x;
             	col.y = normal.y;
             	col.z = normal.z;
			}
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
	int x = get_global_id(0);
	int y = get_global_id(1);

	int index = (x + w * y) * 3;

	float3 target = { x, y, 1.0 }; 

    int c = 4;

    const float4 spheres[4] = { { 5, -1, 5, 1 }, { 15, 0, 5, 3 }, { 5, 1, 15, 2}, { 25, 0, 5, 1 }};

	struct Matrix4x4 mat[1] = { matrix[0] };

	applyMat(mat, &target);

	float4 col = trace(spheres, &c, -1, 0, (origin.xyz), target);

	output[index]   = col.x;
	output[index+1] = col.y;
	output[index+2] = col.z;
}
