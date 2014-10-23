typedef float m_t;
m_t* mulmat4(m_t*, m_t*);
m_t* mulmat4(m_t* matrix, m_t* vec)
{
    m_t a = matrix[0]; m_t e = matrix[4]; m_t i = matrix[8];  m_t m = matrix[12];
    m_t b = matrix[1]; m_t f = matrix[5]; m_t j = matrix[9];  m_t n = matrix[13];
    m_t c = matrix[2]; m_t g = matrix[6]; m_t k = matrix[10]; m_t o = matrix[14];
    m_t d = matrix[3]; m_t h = matrix[7]; m_t l = matrix[11]; m_t p = matrix[15];

    m_t x = vec[0];
    m_t y = vec[1];
    m_t z = vec[2];
    m_t w = vec[3];

    m_t val[4] = {	x*a + y*b + z*c + w*d,
    				x*e + y*f + z*g + w*h,
    				x*i + y*j + z*k + w*l,
    				x*m + y*n + z*o + w*p };
    m_t *retval = val;
    return retval;
}

m_t* mulmat3(m_t*, m_t*);
m_t* mulmat3(m_t* matrix, m_t* vec)
{
    m_t v[4] = { vec[0], vec[1], vec[2], 1 };
    m_t* m = mulmat4(matrix, v);
    m_t mres[3] = {m[0], m[1], m[2]};
    m_t *retval = mres;
    return retval;
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
    float A = vx * vx + vy * vy + vz * vz;
    float B = 2.0 * (px * vx + py * vy + pz * vz - vx * cx - vy * cy - vz * cz);
    float C = px * px - 2.0 * px * cx + cx * cx + py * py - 2.0 * py * cy + cy * cy +
               pz * pz - 2.0 * pz * cz + cz * cz - radius * radius;

    discr.A = A;
	discr.B = B;
    discr.C = C;
    discr.D = B * B - 4.0 * A * C;
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

	float sol1[3] = {
		ox * (1.0 - t1) + t1 * tx, 
		oy * (1.0 - t1) + t1 * ty, 
		oz * (1.0 - t1) + t1 * tz
	};
    
    if(discr.D == 0)
	{
		inter.length = 1;
		inter.points = sol1;
		inter.t1 = t1;
		return inter;
	}

    float t2 = (-discr.B + DSqrt) / A2;
    if (t1 < 0) 
	{
		return inter;
	}

	float sol2[3] = {
		ox * (1.0 - t2) + t2 * tx, 
		oy * (1.0 - t2) + t2 * ty, 
		oz * (1.0 - t2) + t2 * tz
	};

	float a1 = fabs((float)t1 - 0.5);
	float a2 = fabs((float)t2 - 0.5);
    if (a1 < a2)
    {
		float sols[6] = {
			sol1[0], sol1[1], sol1[2],
			sol2[0], sol2[1], sol2[2]
		};
		inter.length = 2;
		inter.points = sols;
		inter.t1 = t1;
		inter.t2 = t2;
        return inter;
    }
	float sols[6] = {
		sol2[0], sol2[1], sol2[2],
		sol1[0], sol1[1], sol1[2]
	};
	inter.length = 2;
	inter.points = sols;
	inter.t1 = t1;
	inter.t2 = t2;
    return inter;
}

float* trace(float*, float*, int);
float* trace(float* source, float* target, int steps)
{

	float svec[3] = { 15,0,0 };
	struct intersection inter = getIntersections(2.0, svec, source, target);

	float col[3] = { 0,0,0 };
	if(inter.length > 0)
	{
		col[0] = 1;
		col[1] = 1;
		col[2] = 1;
	}
	float* colret = col;
	return colret;
}

float* domymult(float*, float);
float* domymult(float* vec, float fac)
{
	float v[4] = {vec[2] * fac, vec[3] * fac, vec[0] * fac, vec[1] * fac};
	float* vres = v;
	return vres;
}

struct Matrix 
{
	float a; float b; float c; float d;
	float e; float f; float g; float h; 
	float i; float j; float k; float l; 
	float m; float n; float o; float p; 
};

struct Matrix createMatrix(float*);
struct Matrix createMatrix(float* data)
{

	struct Matrix matrix;
	matrix.a = data[0 ];
	matrix.b = data[1 ];
	matrix.c = data[2 ];
	matrix.d = data[3 ];
	matrix.e = data[4 ];
	matrix.f = data[5 ];
	matrix.g = data[6 ];
	matrix.h = data[7 ];
	matrix.i = data[8 ];
	matrix.j = data[9 ];
	matrix.k = data[10];
	matrix.l = data[11];
	matrix.m = data[12];
	matrix.n = data[13];
	matrix.o = data[14];
	matrix.p = data[15];
	return matrix;
}

float* matrixToPointer(struct Matrix*);
float* matrixToPointer(struct Matrix* matrix)
{
	float data[16] = { 
	 	matrix->a,
	 	matrix->b,
	 	matrix->c,
	 	matrix->d,
	 	matrix->e,
	 	matrix->f,
	 	matrix->g,
	 	matrix->h,
	 	matrix->i,
	 	matrix->j,
	 	matrix->k,
	 	matrix->l,
	 	matrix->m,
	 	matrix->n,
	 	matrix->o,
	 	matrix->p
	};
	return data;
}

kernel void MatrixMultiply(
	global read_only struct Matrix* matrix,
	global read_only struct Vector3* vector,
	global write_only float* output
)
{
	float data[16] = { 
	 	matrix->a,
	 	matrix->b,
	 	matrix->c,
	 	matrix->d,
	 	matrix->e,
	 	matrix->f,
	 	matrix->g,
	 	matrix->h,
	 	matrix->i,
	 	matrix->j,
	 	matrix->k,
	 	matrix->l,
	 	matrix->m,
	 	matrix->n,
	 	matrix->o,
	 	matrix->p
	};

	for(int i = 0; i < 16; i++)
	{
		float f = data[i];
		output[i] = f + 1;
	}
		
}


kernel void VectorAdd(
	global read_only	float* a,
	global read_only	float* b,
	global write_only	float* color,
	global 			 	float* matrix,
	global read_only 	float* data)
{
	int index = get_global_id(0);

	int readindex = index*3;

	float svec[3] = { 15,0,0 };
	float source[4] = { a[readindex], a[readindex+1], a[readindex+2], 1 };
	float target[4] = { b[readindex], b[readindex+1], b[readindex+2], 1 };

	float m[16] = {matrix[0], matrix[1], matrix[2], matrix[3], 
					  matrix[4], matrix[5], matrix[6], matrix[7], 
					  matrix[8], matrix[9], matrix[10], matrix[11], 
					  matrix[12], matrix[13], matrix[14], matrix[15]};
	float* multiplied = mulmat4(m, source);
	float* multiplied2 = mulmat4(m, target);
	float* multiplied3 = domymult(multiplied, data[0]);
	float* multiplied4 = domymult(multiplied2, data[0]);

	matrix[0] = multiplied[0];
	matrix[1] = multiplied[1];
	matrix[2] = multiplied[2];
	matrix[3] = multiplied[3];

	matrix[4] = multiplied2[0];
	matrix[5] = multiplied2[1];
	matrix[6] = multiplied2[2];
	matrix[7] = multiplied2[3];

	matrix[8] = multiplied3[0];
	matrix[9] = multiplied3[1];
	matrix[10] = multiplied3[2];
	matrix[11] = multiplied3[3];

	matrix[12] = multiplied4[0];
	matrix[13] = multiplied4[1];
	matrix[14] = multiplied4[2];
	matrix[15] = multiplied4[3];

	float* col = trace(source, target, 100);
	color[readindex] = col[0];
	color[readindex+1] = col[1]; //multiplied[1];
	color[readindex+2] = col[2]; //multiplied[2];
}