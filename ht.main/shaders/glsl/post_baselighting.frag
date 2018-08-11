#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"

//Texture input
layout(binding = 1) uniform sampler2D sceneColor;
layout(binding = 2) uniform sampler2D sceneDepth;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    vec2 location = gl_FragCoord.xy / vec2(sceneData.surfaceSizeX, sceneData.surfaceSizeY);
    
    float rawDepth = texture(sceneDepth, location).x;
    float linearDepth = LinearizeDepth(rawDepth);
    float normDepth = linearDepth / sceneData.farClipDistance;

    outColor = vec4(normDepth, normDepth, normDepth, 1.0) * 2.0;
    //outColor = texture(sceneColor, location);
}