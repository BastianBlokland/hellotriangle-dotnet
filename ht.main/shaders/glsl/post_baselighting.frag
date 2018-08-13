#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Texture input
layout(binding = 2) uniform sampler2D sceneColorSampler;
layout(binding = 3) uniform sampler2D sceneNormalSampler;
layout(binding = 4) uniform sampler2D sceneDepthSampler;
layout(binding = 5) uniform sampler2D sceneShadowSampler;

//Input
layout(location = 0) in vec2 inUv;
layout(location = 1) in vec3 inWorldViewDirection;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    vec3 viewDir = normalize(inWorldViewDirection);

    //Sample data from the scene
    vec4 colorAndSpec = texture(sceneColorSampler, inUv);
    vec3 color = colorAndSpec.rgb;
    float specIntensity = colorAndSpec.a;
    vec4 normalAndEmmisiveness = texture(sceneNormalSampler, inUv);
    vec3 normal = normalAndEmmisiveness.xyz;
    float emmisiveness = normalAndEmmisiveness.a;

    //Calculae lighting
    vec3 litResult = color * ambientColor; //Start with the ambient

    //Add the sun intensity to the result
    float sunIntensity = max(dot(sunDirection, normal) * -1.0, 0.0); //* -1 because its lit when normals are opposite
    litResult += sunColor * color * sunIntensity;

    //Add the sun specular to the result
    if (sunIntensity > 0.0)
    {
        vec3 halfDir = normalize(sunDirection + viewDir);
        float specAngle = max(dot(halfDir, normal) * -1.0, 0.0);
        float specular = pow(specAngle, sunSpecPower) * specIntensity;
        litResult += sunColor * specular;
    }

    //Emmisiveness decides how much of the raw unlit color we use
    outColor = vec4(mix(litResult, color, emmisiveness), 1.0);

    // float rawDepth = texture(sceneShadowSampler, inUv).x;
    // float linearDepth = LinearizeDepth(rawDepth);
    // float normDepth = linearDepth / cameraData.farClipDistance;
    // outColor = vec4(normDepth, normDepth, normDepth, 1.0) * 2.0;
}