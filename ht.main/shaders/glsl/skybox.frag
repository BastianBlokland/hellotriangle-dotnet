#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"

//Texture input
layout(binding = 1) uniform samplerCube skyboxTexture;

//Vert to frag input
layout(location = 0) in vec3 skyboxDirection;
layout(location = 1) in vec3 worldNormal;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

void main() 
{
    outColor = texture(skyboxTexture, skyboxDirection);
    outNormal.xyz = normalize(worldNormal);
}