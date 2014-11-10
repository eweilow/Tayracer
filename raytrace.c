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

#define MAX_STEPS 8192

float3 grid(float3);
float3 grid(float3 pos)
{
	float f = (fmod(floor(pos.x) + floor(pos.z), 2) < 1) ? 0.3 : 1;
	float3 checker = {f,f,f};
	return checker;
}

float4 trace(float4*, float4*, int*, int, int, float3, float3);
float4 trace(float4* spheres, float4* cols, int* c, int skip, int steps, float3 source, float3 target)
{
    //const float4 lights[1] = { {0,0,0,0} };
    //const float3 lightsCol[1] = { {1,0,0} };
	float4 col;
	col.x = 0; col.y = 0; col.z = 0; col.w = -1;

	float3 targ = normalize(target - source);
	for(int i = 0; i < *c; i++)
	{
		if(i == skip) continue;

		float4* sp = &spheres[i];
		float4* spcol = &cols[i];

		float3 sphere_pos = sp->xyz;
		float sphere_radius = sp->w;

		float3 toSp = (source - sphere_pos);
		float f = findInter(toSp, targ, sphere_radius);
		if(f >= 0) 
		{
			float3 incident = targ * f + source;
			float3 normal = normalize(incident - sphere_pos);

			if(col.w < 0) col.w = f;
			else if(f >= col.w) continue;
			else col.w = f;

			if(steps < MAX_STEPS) { 
				float3 refl = reflect(targ, normal);
				float4 reflCol = trace(spheres, cols, c, i, steps+1, incident, incident + refl);

				float3 spCol = spcol->xyz;

				float fac = spcol->w;
				float oneFac = 1.0 - fac;

				col.x = spCol.x * oneFac + reflCol.x * fac;
				col.y = spCol.y * oneFac + reflCol.y * fac;
				col.z = spCol.z * oneFac + reflCol.z * fac;
			}
			else
			{
				float3 g = grid(incident);
				col.x = g.x;
             	col.y = g.y;
             	col.z = g.z;
			}
		}
	}

	if(skip == -2) return col;
	//float findInterPlane(float3 pNormal, float3 pPos, float3 rayPos, float3 rayDir)
	/* plane */
	float3 pnormal = { 0, 1, 0 };
	float3 ppos = { 0, -1, 0 };

	float fPlane = findInterPlane(pnormal, ppos, source, targ);
	if(fPlane >= 0) 
	{
		float3 incident = targ * fPlane + source;
		float3 normal = pnormal;

		if(col.w < 0) col.w = fPlane;
		else if(fPlane >= col.w) return col;
		else col.w = fPlane;

		float3 tex = grid(incident);
		const float planeFac = 0.5;
		const float onePlaneFac = 1.0 - planeFac;
		if(steps < MAX_STEPS) { 
			float3 refl = reflect(targ, normal);
			float4 reflCol = trace(spheres, cols, c, -2, steps+1, incident, incident + refl);

			col.x = tex.x * onePlaneFac + reflCol.x * planeFac;
			col.y = tex.y * onePlaneFac + reflCol.y * planeFac;
			col.z = tex.z * onePlaneFac + reflCol.z * planeFac;
		}
		else
		{
			col.x = tex.x;
         	col.y = tex.y;
         	col.z = tex.z;
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
    const float4 spherecolors[4] = { { 1, 0.2, 0.1, 0.5 }, { 0.1, 1, 0.1, 0.7 },{ 1.0, 0.1, 0.1, 0.95 },{ 0.1, 0.2, 1.0, 0.5 }};

	struct Matrix4x4 mat[1] = { matrix[0] };

	applyMat(mat, &target);

	/* p, p, p, copy, copy, copy, copy */
	float4 col = trace(spheres, spherecolors, &c, -1, 0, (origin.xyz), target);

	output[index]   = col.x;
	output[index+1] = col.y;
	output[index+2] = col.z;
}
