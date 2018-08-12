#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Texture input
layout(binding = 1) uniform sampler2D sceneColorSampler;
layout(binding = 2) uniform sampler2D sceneNormalSampler;
layout(binding = 3) uniform sampler2D sceneDepthSampler;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    //Calculate what location of the screen (0 to 1 'texture space)
    vec2 location = gl_FragCoord.xy / vec2(sceneData.surfaceSizeX, sceneData.surfaceSizeY);
    
    //Sample data from the scene
    vec4 color = texture(sceneColorSampler, location);
    vec4 normalAndEmmisiveness = texture(sceneNormalSampler, location);
    vec3 normal = normalAndEmmisiveness.xyz;
    float emmisiveness = normalAndEmmisiveness.a;

    //Calculae lighting
    vec4 litResult = color * ambientColor; //Start with the ambient

    //Add the sun intensity to the result
    float sunIntensity = max(dot(sunDirection, normal) * -1.0, 0.0);
    litResult += sunColor * color * sunIntensity;

    //Emmisiveness decides how much of the raw unlit color we use
    outColor = mix(litResult, color, emmisiveness);
}