#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform ShadowData shadowData;
layout(binding = 2) uniform SceneData sceneData;
layout(binding = 3) uniform sampler2D sceneColorSampler;
layout(binding = 4) uniform sampler2D sceneNormalSampler;
layout(binding = 5) uniform sampler2D sceneDepthSampler;
layout(binding = 6) uniform sampler2D sceneShadowSampler;

//Input
layout(location = 0) in vec2 inUv;
layout(location = 1) in vec3 inWorldViewDirection;

//Output
layout(location = 0) out vec4 outColor;

float getShadow(vec3 worldPos)
{
    #define blurSize 0.5

    vec2 texelSize = 1.0 / textureSize(sceneShadowSampler, 0);
    vec4 clipPos = shadowData.viewProjectionMatrix * vec4(worldPos, 1.0);
    vec2 baseShadowCoord = clipPos.xy * 0.5 + 0.5; //To texture space

    //Take multiple samples with a offset to apply blurring for softer shadows
    float shadowSum = 0.0;
    for (int y = -1; y <= 1; y += 1)
    for (int x = -1; x <= 1; x += 1)
    {
        vec2 shadowCoord = baseShadowCoord + vec2(x * blurSize, y * blurSize) * texelSize;
        shadowSum += float(texture(sceneShadowSampler, shadowCoord).r  < clipPos.z);
    }
    return shadowSum / 9;
}

void main()
{
    vec3 viewDir = normalize(inWorldViewDirection);

    //Sample data from the scene
    vec4 colorAndSpec = texture(sceneColorSampler, inUv);
    vec3 color = colorAndSpec.rgb;
    float specIntensity = colorAndSpec.a * specMultiplier;
    vec4 normalAndEmmisiveness = texture(sceneNormalSampler, inUv);
    vec3 normal = normalAndEmmisiveness.xyz;
    float depth = texture(sceneDepthSampler, inUv).r;
    float linearDepth = linearizeDepth(depth, cameraData.nearClipDistance, cameraData.farClipDistance);
    vec3 worldPos = worldPosFromDepth(depth, inUv, cameraData.inverseViewProjectionMatrix);
    float emmisiveness = normalAndEmmisiveness.a;
    vec3 sunDirection = mat3(shadowData.cameraMatrix) * vec3(0.0, 0.0, 1.0);
    
    //Calculate shadow
    #define MIN_SHADOW_DIST 70.0
    #define MAX_SHADOW_DIST 85.0
    float depthBlend = smoothstep(MAX_SHADOW_DIST, MIN_SHADOW_DIST, linearDepth);
    float shadow = getShadow(worldPos) * depthBlend;

    //Calculae lighting
    vec3 litResult = color * ambientColor; //Start with the ambient

    //Add the sun intensity to the result
    float sunIntensity = max(dot(normal, sunDirection), 0.0) * (1.0 - shadow);
    litResult += sunColor * color * sunIntensity;

    //Add the sun specular to the result
    vec3 halfDir = normalize(sunDirection - viewDir);
    float specAngle = max(dot(normal, halfDir), 0.0);
    float specular = pow(specAngle, sunSpecPower) * specIntensity;
    litResult += sunColor * specular * sunIntensity;

    //Emmisiveness decides how much of the raw unlit color we use
    outColor = vec4(mix(litResult, color, emmisiveness), 1.0);
}