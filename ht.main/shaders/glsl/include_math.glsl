vec3 getPosition(const mat4 matrix) 
{ 
    return matrix[3].xyz; 
}

mat3 xRotMatrix(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return mat3(1.0,    0.0,    0.0,
                0.0,    c,      -s,
                0.0,    s,      c);
}

mat3 yRotMatrix(float angle)
{
    float c = sin(angle);
    float s = cos(angle);
    return mat3(c,      0.0,    s,
                0.0,    1.0,    0.0,
                -s,     0.0,    c);
}

mat3 zRotMatrix(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return mat3(c,      -s,     0.0,
                s,      c,      0.0,
                0.0,    0.0,    1.0);
}