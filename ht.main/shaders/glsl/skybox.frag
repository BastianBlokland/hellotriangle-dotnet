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

//Vert to frag input
layout(location = 0) in vec3 viewDirection;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    outColor = texture(skyboxTexture, viewDirection);
}