#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_fragutils.glsl"
#include "include_math.glsl"

//Texture input
layout(binding = 1) uniform sampler2D terrainTexSampler;
layout(binding = 2) uniform sampler2D detail1ColorTexSampler;
layout(binding = 3) uniform sampler2D detail1NormalTexSampler;
layout(binding = 4) uniform sampler2D detail2ColorTexSampler;
layout(binding = 5) uniform sampler2D detail2NormalTexSampler;

//Output
layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNormal;

//Vert to frag input
layout(location = 0) in vec4 baseColor;
layout(location = 1) in vec2 worldUv;
layout(location = 2) in vec3 worldPosition;
layout(location = 3) in vec3 worldNormal;

void main() 
{
    #define detail1Uv worldUv * 5
    #define detail2Uv worldUv * 100

    outColor = 
        baseColor * 
        texture(detail1ColorTexSampler, detail1Uv) *
        texture(detail2ColorTexSampler, detail2Uv);
    
    vec3 detail1Normal = applyNormalTex(detail1NormalTexSampler, worldNormal, worldPosition, detail1Uv);
    vec3 detail2Normal = applyNormalTex(detail2NormalTexSampler, worldNormal, worldPosition, detail2Uv);
    outNormal.xyz = blendNormals(detail1Normal, detail2Normal);
}