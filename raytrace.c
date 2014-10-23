struct Matrix4x4
{
	float a; float e; float i; float m;
	float b; float f; float j; float n; 
	float c; float g; float k; float o; 
	float d; float h; float l; float p; 
};

struct Vector3 
{
	float x; float y; float z;
};

struct Vector3 multiplyMat(struct Matrix4x4, struct Vector3);
struct Vector3 multiplyMat(struct Matrix4x4 matrix, struct Vector3 vector)
{

	struct Vector3 vec;
	float w = 1.0 / (vector.x * matrix.m + vector.y * matrix.n + vector.z * matrix.o + matrix.p);
	vec.x = (vector.x * matrix.a + vector.y * matrix.b + vector.z * matrix.c + matrix.d) * w;
	vec.y = (vector.x * matrix.e + vector.y * matrix.f + vector.z * matrix.g + matrix.h) * w;
	vec.z = (vector.x * matrix.i + vector.y * matrix.j + vector.z * matrix.k + matrix.l) * w;

	return 
	vec;
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
    float* points;

    float D;
    float t1;
    float t2;
};

struct discriminant getDiscriminant(float, float*, float*, float*);
struct discriminant getDiscriminant(float radius, float* sphere, float* origin, float* target)
{
    float cx = sphere[0];
    float cy = sphere[1];
    float cz = sphere[2];

    float px = origin[0];
    float py = origin[1];
    float pz = origin[2];

    float vx = target[0] - px;
    float vy = target[1] - py;
    float vz = target[2] - pz;

    struct discriminant discr;
    discr.A = vx * vx + vy * vy + vz * vz;
    float B = 2.0 * (px * vx + 
    				 py * vy + 
    				 pz * vz - 
    				 vx * cx - 
    				 vy * cy - 
    				 vz * cz);
    float C = 	px * px - 2.0 * px * cx + cx * cx + 
    			py * py - 2.0 * py * cy + cy * cy +
               	pz * pz - 2.0 * pz * cz + cz * cz - 
               	radius * radius;

	discr.B = B;
    discr.C = C;
    discr.D = B * B - 4.0 * discr.A * C;
    return discr;
}

struct intersection getIntersections(float, float*, float*, float*);
struct intersection getIntersections(float radius, float *sphere, float *origin, float *target)
{
    struct discriminant discr = getDiscriminant(radius, sphere, origin, target);
    struct intersection inter;
	inter.length = 0;
	inter.D = discr.D;

	float ox = origin[0];
    float oy = origin[1];
    float oz = origin[2];

	float tx = target[0];
	float ty = target[1];
	float tz = target[2];

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
		float m1 = 1.0 - t1;
		float sol1[3] = {
			ox * m1 + t1 * tx, 
			oy * m1 + t1 * ty, 
			oz * m1 + t1 * tz
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

	float a1 = fabs((float)t1 - 0.5);
	float a2 = fabs((float)t2 - 0.5);
    if (a1 < a2)
    {
		float m1 = 1.0 - t1;
    	float m2 = 1.0 - t2;
		float sols[6] = {
			ox * m1 + t1 * tx, 
			oy * m1 + t1 * ty, 
			oz * m1 + t1 * tz,
			ox * m2 + t2 * tx, 
			oy * m2 + t2 * ty, 
			oz * m2 + t2 * tz
		};
		inter.length = 2;
		inter.points = sols;
		inter.t1 = t1;
		inter.t2 = t2;
    }
	else
	{
		float m1 = 1.0 - t1;
    	float m2 = 1.0 - t2;
		float sols[6] = {
			ox * m2 + t2 * tx, 
			oy * m2 + t2 * ty, 
			oz * m2 + t2 * tz,
			ox * m1 + t1 * tx, 
			oy * m1 + t1 * ty, 
			oz * m1 + t1 * tz
		};
		inter.length = 2;
		inter.points = sols;
		inter.t1 = t1;
		inter.t2 = t2;
	}
    return inter;
}

float* trace(float*, float*, int);
float* trace(float* source, float* target, int steps)
{
	float svec[3] = { 15,0,0 };
	float svec2[3] = { 15,0,5 };
	float col[3] = { 0,0,0 };

	struct intersection inter = getIntersections(2.0, svec, source, target);

	if(inter.length > 0)
	{
		col[0] += 1;
		col[1] += 1;
		col[2] += 1;
	}

	struct intersection inter2 = getIntersections(2.0, svec2, source, target);
	if(inter2.length > 0)
	{
		col[0] += 1;
		col[1] += 1;
		col[2] += 1;
	}
	float* colret = col;
	return colret;
}

kernel void MatrixMultiply(
	const int w,
	const int h,
	const float4 origin,
	global const struct Matrix4x4* matrix,
	global write_only uchar* output
)
{
	//int width = (int)data[0];
	//int height = (int)data[1];
	//int index = get_global_id(0);

	int x = get_global_id(0); //index % width; //i = y * width + x
	int y = get_global_id(1); //index / width;

	int index = x + w * y;

	struct Vector3 source; source.x = (float)x; source.y = (float)y;//(float)x / width * 2.0 - 1.0; source.y = (float)y / height * 2.0 - 1.0;
	source.z = 1.0;

	/*struct Vector4 target; target.x = (float)_x; target.y = (float)_y;
	target.z = 1.0;
	target.w = 1.0;*/

	struct Matrix4x4 mat2 = matrix[0];
	struct Vector3 sourceMult = multiplyMat(mat2, source);
	//struct Vector4 targetMult = multiplyMat(mat2, target);

	float targetfl[4] = { sourceMult.x, sourceMult.y, sourceMult.z, 1 };
	float* sourcefl = &origin;
	float* col = trace(sourcefl, targetfl, 100);

	//float4 out;
	output[index*3]   = (uchar)(col[0]*255.0);
	output[index*3+1] = (uchar)(col[1]*255.0);
	output[index*3+2] = (uchar)(col[2]*255.0);

	//output[index] = out;
}
