#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 3;

//PushData
layout(push_constant) uniform PushData
{
    int swapchainIndex;
} pushdata;

//Uniforms
layout(binding = 0) uniform SceneDataBlock { SceneData sceneData[swapchainCount]; };
layout(binding = 1) uniform CameraDataBlock { CameraData cameraData[swapchainCount]; };
layout(binding = 2) uniform ShadowDataBlock { CameraData shadowData[swapchainCount]; };
layout(binding = 3) uniform sampler2D sceneColorSampler;
layout(binding = 4) uniform sampler2D sceneNormalSampler;
layout(binding = 5) uniform sampler2D sceneAttributeSampler;
layout(binding = 6) uniform sampler2D sceneDepthSampler;
layout(binding = 7) uniform sampler2D sceneShadowSampler;
layout(binding = 8) uniform sampler2D sceneBloomSampler;
layout(binding = 9) uniform sampler2D sceneAOSampler;
layout(binding = 10) uniform samplerCube reflectionSampler;

//Input
layout(location = 0) in vec2 inUv;

//Output
layout(location = 0) out vec4 outColor;

float getShadow(vec3 worldPos)
{
    #define blurSize 0.5

    mat4 viewProjectionMatrix = shadowData[pushdata.swapchainIndex].viewProjectionMatrix;
    vec2 texelSize = 1.0 / textureSize(sceneShadowSampler, 0);
    vec4 clipPos = viewProjectionMatrix * vec4(worldPos, 1.0);
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
    float nearClipDistance = cameraData[pushdata.swapchainIndex].nearClipDistance;
    float farClipDistance = cameraData[pushdata.swapchainIndex].farClipDistance;
    mat4 sceneCamMatrix = cameraData[pushdata.swapchainIndex].cameraMatrix;
    mat4 invViewProjectionMatrix = cameraData[pushdata.swapchainIndex].inverseViewProjectionMatrix;
    mat4 shadowCamMatrix = shadowData[pushdata.swapchainIndex].cameraMatrix;

    vec3 clipPos = vec3(inUv * 2.0 - 1.0, 1.0);
    vec3 viewDir = normalize((invViewProjectionMatrix * vec4(clipPos, 0.0)).xyz);
    vec3 camPos = sceneCamMatrix[3].xyz;

    //Sample data from the scene
    vec3 color = texture(sceneColorSampler, inUv).rgb;
    vec3 worldNormal = texture(sceneNormalSampler, inUv).xyz;
    vec4 attributes = texture(sceneAttributeSampler, inUv);
    float depth = texture(sceneDepthSampler, inUv).r;
    float specIntensity = attributes.x * specMultiplier;
    float emmisiveness = attributes.y;
    float shadowReceive = attributes.z;
    float reflectivity = attributes.a;
    float linearDepth = linearizeDepth(depth, nearClipDistance, farClipDistance);
    vec3 worldPos = worldPosFromDepth(depth, inUv, invViewProjectionMatrix);
    vec3 sunDirection = mat3(shadowCamMatrix) * vec3(0.0, 0.0, 1.0);
    float ambientOcclusion = texture(sceneAOSampler, inUv).r;
    
    //Calculate shadow
    #define MIN_SHADOW_DIST 70.0
    #define MAX_SHADOW_DIST 85.0
    float depthBlend = smoothstep(MAX_SHADOW_DIST, MIN_SHADOW_DIST, linearDepth);
    //In the distance use ambientOcclusion to fake some sort of shadows
    float shadow = mix(1.0 - ambientOcclusion, getShadow(worldPos), depthBlend) * shadowReceive;

    //Calculate reflection color
    vec3 toSurfaceDir = normalize(worldPos - camPos);
    vec3 reflectNormal = reflect(toSurfaceDir, worldNormal);
    vec3 reflectColor = texture(reflectionSampler, reflectNormal).rgb;
    color = mix(color, (color * 0.3) + reflectColor * 1.5, reflectivity);

    //Calculate lighting
    vec3 litResult = color * mix(minAmbientColor, maxAmbientColor, ambientOcclusion); //Start with the ambient

    //Add the sun intensity to the result
    float sunIntensity = max(dot(worldNormal, sunDirection), 0.0) * (1.0 - shadow);
    litResult += sunColor * color * sunIntensity;

    //Add the sun specular to the result
    vec3 halfDir = normalize(sunDirection - viewDir);
    float specAngle = max(dot(worldNormal, halfDir), 0.0);
    float specular = pow(specAngle, sunSpecPower) * specIntensity;
    litResult += sunColor * specular * sunIntensity;

    //Emmisiveness decides how much of the raw unlit color we use
    outColor = vec4(mix(litResult, color, emmisiveness), 1.0);

    //Apply fog
    float fogHeightBlend = smoothstep(1500.0, 0.0, worldPos.y * worldPos.y);
    float fogDistanceBlend = clamp(linearDepth / 125.0, 0.0, 0.6);
    outColor = mix(outColor, vec4(sunColor, 1), fogHeightBlend * fogDistanceBlend);

    //Apply bloom
    outColor.rgb += texture(sceneBloomSampler, inUv).rgb;
}