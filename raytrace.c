struct Matrix4x4
{
	float a; float e; float i; float m;
	float b; float f; float j; float n; 
	float c; float g; float k; float o; 
	float d; float h; float l; float p; 
};

void applyMat(struct Matrix4x4*, float4*);
void applyMat(struct Matrix4x4* matrix, float4* vector)
{
	float w = 1.0 / (vector->x * matrix->m + vector->y * matrix->n + vector->z * matrix->o + matrix->p);
	float x = (vector->x * matrix->a + vector->y * matrix->b + vector->z * matrix->c + matrix->d) * w;
	float y = (vector->x * matrix->e + vector->y * matrix->f + vector->z * matrix->g + matrix->h) * w;
	float z = (vector->x * matrix->i + vector->y * matrix->j + vector->z * matrix->k + matrix->l) * w;

	vector->x = x;
	vector->y = y;
	vector->z = z;
}

struct discriminant
{
    float A;
    float B;
    float C;
    float D;
};

struct intersection
{
    int length;
    float D;
    float t1;
    float t2;
    float4* points;
};

struct discriminant getDiscriminant(float4*, float4*, float4*);
struct discriminant getDiscriminant(float4* sphere, float4* origin, float4* target)
{
    float cx = sphere->x;
    float cy = sphere->y;
    float cz = sphere->z;

    float px = origin->x;
    float py = origin->y;
    float pz = origin->z;

    float vx = target->x - px;
    float vy = target->y - py;
    float vz = target->z - pz;

    struct discriminant discr;
    discr.A = vx * vx + vy * vy + vz * vz;
    discr.B = 2.0 * (px * vx + 
    				 py * vy + 
    				 pz * vz - 
    				 vx * cx - 
    				 vy * cy - 
    				 vz * cz);
    discr.C = 	px * px - 2.0 * px * cx + cx * cx + 
    			py * py - 2.0 * py * cy + cy * cy +
               	pz * pz - 2.0 * pz * cz + cz * cz - 
               	sphere->w * sphere->w;
    discr.D = discr.B * discr.B - 4.0 * discr.A * discr.C;
    return discr;
}

struct intersection getIntersections(float4*, float4*, float4*);
struct intersection getIntersections(float4* sphere, float4* origin, float4* target)
{
    struct discriminant discr = getDiscriminant(sphere, origin, target);
    struct intersection inter;
	inter.length = 0;
	inter.D = discr.D;

	if (discr.D < 0) 
	{
		return inter;
	}
		
	float DSqrt = sqrt(discr.D);
	float A2 = 2.0*discr.A;
    float t1 = (-discr.B - DSqrt) / A2;
    if (t1 < 0) 
	{
		return inter;
	}
		
    if(discr.D == 0)
	{
		float ox = origin->x;
	    float oy = origin->y;
	    float oz = origin->z;

		float tx = target->x;
		float ty = target->y;
		float tz = target->z;
		float m1 = 1.0 - t1;

		float4 p1;
		p1.x = ox * m1 + t1 * tx;
		p1.y = oy * m1 + t1 * ty;
		p1.z = oz * m1 + t1 * tz;
		float4 sol1[1] = {
			p1
		};
		inter.length = 1;
		inter.points = sol1;
		inter.t1 = t1;
		return inter;
	}

    float t2 = (-discr.B + DSqrt) / A2;
    if (t2 < 0) 
	{
		return inter;
	}

	float ox = origin->x;
    float oy = origin->y;
    float oz = origin->z;

	float tx = target->x;
	float ty = target->y;
	float tz = target->z;

	float a1 = fabs((float)t1 - 0.5);
	float a2 = fabs((float)t2 - 0.5);


	float m1 = 1.0 - t1;
    float m2 = 1.0 - t2;

	float4 p1;
	p1.x = ox * m1 + t1 * tx;
	p1.y = oy * m1 + t1 * ty;
	p1.z = oz * m1 + t1 * tz;

	float4 p2;
	p2.x = ox * m2 + t2 * tx;
	p2.y = oy * m2 + t2 * ty;
	p2.z = oz * m2 + t2 * tz;
    if (a1 < a2)
    {
		float4 sols[2] = {
			p1, p2
		};
		inter.length = 2;
		inter.points = sols;
		inter.t1 = t1;
		inter.t2 = t2;
    }
	else
	{
		float4 sols[2] = {
			p2, p1
		};
		inter.length = 2;
		inter.points = sols;
		inter.t1 = t1;
		inter.t2 = t2;
	}
    return inter;
}

float3 calcLight(float4*, int, int, float4*, float3*, float4*, float3*);
float3 calcLight(float4* sph, int len, int ignore, float4* p, float3* n, float4* l_p, float3* l_c)
{
    float3 lightRet;
    lightRet.x = 0; lightRet.y = 0; lightRet.z = 0;

    int intersections = 0;
    for(int i = 0; i < len; i++)
    {
    	if(i == ignore) continue;

    	float4* sp = &sph[i];
    	struct intersection shadow = getIntersections(sp, p, l_p);
		if(shadow.length > 0) intersections++;
    }
    /*if(intersections==0)
	{
		float3 l_d = p->xyz - l_p->xyz;
		float3 normal = *n;
		float dp = dot(normal, -fast_normalize(l_d));
		lightRet.x = l_c->x * dp;
		lightRet.y = l_c->y * dp;
		lightRet.z = l_c->z * dp;
	}*/
	if(intersections==0)
	{
		lightRet.x = 1;
	}
	else
	{
		lightRet.y = 1;
	}
	return lightRet;
}

/*
// px,py,pz=(ray origin position - sphere position),
function check_ray(px,py,pz,dx,dy,dz,r){
 
    //calculate the determinant
    var det,b;
    b = -dot(px,py,pz,dx,dy,dz);
    det = b*b - dot(px,py,pz,px,py,pz) + r*r;
 
    if (det<0) //if it's less than 0, there's no intersection, return -1
        return -1;
 
    //calculate the two values for t
    det= sqrt(det);
    t1= b - det;
    t2= b + det;  //not really necessary!
 
    //always return t1, as it'll always be the shortest distance
    return t1;
}*/

float findInter(float3*, float3*, float);
float findInter(float3* toSphere, float3* rayDir, float r)
{
    float3 p = *toSphere;
    float3 d = *rayDir;
    float b = -dot(p, d);
    float det = b*b - dot(p, p) + r*r;

    if(det < 0) return -1;
    det = sqrt(det);

    return b - det;
}

float4* trace(float4*, float4*);
float4* trace(float4* source, float4* target)
{
    int c = 4;
    float4 spheres[4] = { { 5, -1, 5, 1 }, { 15, 0, 5, 3 }, { 5, 1, 15, 2}, { 25, 0, 5, 1 }};
    float4 lights[1] = { {0,0,0,0} };
    float3 lightsCol[1] = { {1,0,0} };
	float4 col[1];
	col->x = 0; col->y = 0; col->z = 0; col->w = -1;

	float3 eye = source->xyz;
	for(int i = 0; i < c; i++)
	{
		float4* sp = &spheres[i];
		float3 toSp = eye - sp->xyz;
		float3 targ = normalize(target->xyz - source->xyz);
		float f = findInter(&toSp, &targ, sp->w);
		if(f >= 0) 
		{
			col->x = 1;
		}
		/*
		struct intersection inter = getIntersections(sp, source, target);
		if(inter.length > 0)
		{
			col->x = 1;/ *
			float3 sphCent = sp->xyz;
			float3 inc = inter.points->xyz;

			float3 normal = fast_normalize(sphCent - inc);
			float d = fast_distance(sphCent, inc);

			//float dp = dot(normal, -fast_normalize(inc)) * shadowFac;
			float3 colc;
			colc.x = 0; colc.y = 0; colc.z = 0;
			for(int j = 0; j < 1; j++)
			{
				float3 retcol = calcLight(spheres, c, i, inter.points, &normal, &lights[i], &lightsCol[i]);
				colc.x = colc.x + retcol.x;
				colc.y = colc.y + retcol.y;
				colc.z = colc.z + retcol.z;
			}

			if(col->w < 0)
				col->w = d;

			if(col->w <= d)
			{
				col->x = colc.x;
				col->y = colc.y;
				col->z = colc.z;
				col->w = d;
			}* /
		}*/
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

	float4 target = { get_global_id(0), get_global_id(1), 1.0, 0.0 }; 
	//target.x = get_global_id(0); target.y = get_global_id(1);//(float)x / width * 2.0 - 1.0; source.y = (float)y / height * 2.0 - 1.0;
	//target.z = 1.0;

	struct Matrix4x4 mat[1] = { matrix[0] };

	applyMat(mat, &target);
	float* col = trace(&origin, &target);

	output[index]   = (col[0]);
	output[index+1] = (col[1]);
	output[index+2] = (col[2]);
}
