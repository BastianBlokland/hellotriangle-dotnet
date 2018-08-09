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
layout(binding = 1) uniform sampler2D terrainTexSampler;
layout(binding = 2) uniform sampler2D detail1TexSampler;
layout(binding = 3) uniform sampler2D detail2TexSampler;

//Output
layout(location = 0) out vec4 outColor;

//Vert to frag input
layout(location = 0) in vec4 baseColor;
layout(location = 1) in vec2 worldUv;

void main() 
{
    const float detail1TexScale = 5;
    const float detail2TexScale = 100;

    outColor = 
        baseColor * 
        texture(detail1TexSampler, worldUv * detail1TexScale) *
        texture(detail2TexSampler, worldUv * detail2TexScale);
}