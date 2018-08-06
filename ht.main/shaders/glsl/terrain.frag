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

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    //Output
    outColor = vec4(1, 1, 1, 1);
}