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

//Two triangles allready in clip-space to form a fullscreen quad
//Position at z = 1 meaning the far clip-plane
//TODO: Can be optimised by using triangleStrip rendering or doing some smart bit-shifting to extract
//these from the gl_VertexIndex
const vec4 positions[6] = vec4[]
(
    //Triangle one
    vec4(-1, -1, 1, 1),
    vec4(1, -1, 1, 1),
    vec4(1, 1, 1, 1),

    //Triangle two
    vec4(-1, -1, 1, 1),
    vec4(1, 1, 1, 1),
    vec4(-1, 1, 1, 1)
);

void main()
{
    gl_Position = positions[gl_VertexIndex];
    
    //Create world space viewDirection by transforming the clip-space vertices back into view-space
    //by using a inverse of the projection-matrix and then transforming them from view-space into
    //world space by using the cameraMatrix
    viewDirection = mat3(sceneData.cameraMatrix) * (inverse(sceneData.projectionMatrix) * gl_Position).xyz;
}