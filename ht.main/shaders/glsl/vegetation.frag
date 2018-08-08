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
layout(binding = 1) uniform sampler2D colorTexSampler;
layout(binding = 2) uniform sampler2D terrainTexSampler;

//Vert to frag input
layout(location = 0) in vec2 colorUv;
layout(location = 1) in vec4 colorTint;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    //Output
    outColor = texture(colorTexSampler, colorUv) * colorTint;
    if (outColor.a < 0.01)
    {
        discard;
    }
}