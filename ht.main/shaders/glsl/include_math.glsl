float linearizeDepth(float depthSample, float near, float far)
{
    return 2.0 * near * far / (far + near - depthSample * (far - near));
}

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

//Simple method for blending normals, more experimentation can be done to improve this
//Allot of info in this article: http://blog.selfshadow.com/publications/blending-in-detail/
vec3 blendNormals(vec3 normal1, vec3 normal2)
{
    vec3 result = vec3(normal1.xy + normal2.xy, normal1.z * normal2.z);
    return normalize(result);
}

//Convert a color to a luminance ('brightness') value, uses constansts to weight the different
//channels to compensate for human eye sensitivity
//https://en.wikipedia.org/wiki/Luma_(video)
float luma(vec3 color)
{
    return dot(vec3(0.2126, 0.7152, 0.0722), color);
}