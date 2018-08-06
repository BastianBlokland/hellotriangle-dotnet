#version 450
#extension GL_ARB_separate_shader_objects : enable

//Scene input
layout(binding = 0) uniform SceneData 
{
    mat4 cameraMatrix;
	mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 viewProjectionMatrix;
    int frame;
    float time;
    float deltaTime;
} sceneData;

//Texture input
layout(binding = 1) uniform samplerCube skyboxTexture;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec3 viewDirection;

mat3 yRotMatrix(float angle)
{
    float c = sin(angle);
    float s = cos(angle);
    return mat3(c,      0.0,    s,
                0.0,    1.0,    0.0,
                -s,     0.0,    c);
}

void main()
{
    //Can be used to tweak the rotatin of the skybox
    float offsetAngle = 2.1;

    //Fullscreen triangle, more info: https://www.saschawillems.de/?page_id=2122
    gl_Position = vec4((gl_VertexIndex << 1 & 2) * 2.0 - 1, (gl_VertexIndex & 2) * 2.0 - 1, 1, 1);

    //Create world space viewDirection by transforming the clip-space vertices back into view-space
    //by using a inverse of the projection-matrix and then transforming them from view-space into
    //world space by using the cameraMatrix
    viewDirection = yRotMatrix(offsetAngle) *
                    mat3(sceneData.cameraMatrix) *
                    (inverse(sceneData.projectionMatrix) * gl_Position).xyz;
}