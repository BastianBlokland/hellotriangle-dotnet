#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_fragutils.glsl"
#include "include_sceneinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Uniforms
layout(binding = 0) uniform SceneData sceneData;
layout(binding = 1) uniform CameraData cameraData;
layout(binding = 2) uniform ShadowData shadowData;
layout(binding = 3) uniform sampler2D sceneColorSampler;
layout(binding = 4) uniform sampler2D sceneNormalSampler;
layout(binding = 5) uniform sampler2D sceneAttributeSampler;
layout(binding = 6) uniform sampler2D sceneDepthSampler;
layout(binding = 7) uniform sampler2D sceneBloomSampler;
layout(binding = 8) uniform sampler2D sceneShadowSampler;
layout(binding = 9) uniform samplerCube reflectionSampler;

//Input
layout(location = 0) in vec2 inUv;

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
    vec3 clipPos = vec3(inUv * 2.0 - 1.0, 1.0);
    vec3 viewDir = normalize((cameraData.inverseViewProjectionMatrix * vec4(clipPos, 0.0)).xyz);
    vec3 camPos = cameraData.cameraMatrix[3].xyz;

    //Sample data from the scene
    vec3 color = texture(sceneColorSampler, inUv).rgb;
    vec3 normal = texture(sceneNormalSampler, inUv).xyz;
    vec4 attributes = texture(sceneAttributeSampler, inUv);
    float depth = texture(sceneDepthSampler, inUv).r;
    float specIntensity = attributes.x * specMultiplier;
    float emmisiveness = attributes.y;
    float shadowReceive = attributes.z;
    float reflectivity = attributes.a;
    float linearDepth = linearizeDepth(depth, cameraData.nearClipDistance, cameraData.farClipDistance);
    vec3 worldPos = worldPosFromDepth(depth, inUv, cameraData.inverseViewProjectionMatrix);
    vec3 sunDirection = mat3(shadowData.cameraMatrix) * vec3(0.0, 0.0, 1.0);
    
    //Calculate shadow
    #define MIN_SHADOW_DIST 70.0
    #define MAX_SHADOW_DIST 85.0
    float depthBlend = smoothstep(MAX_SHADOW_DIST, MIN_SHADOW_DIST, linearDepth);
    float shadow = getShadow(worldPos) * depthBlend * shadowReceive;

    //Calculate reflection color
    vec3 toSurfaceDir = normalize(worldPos - camPos);
    vec3 reflectNormal = reflect(toSurfaceDir, normal);
    vec3 reflectColor = texture(reflectionSampler, reflectNormal).rgb;
    color = mix(color, (color * 0.3) + reflectColor * 1.5, reflectivity);

    //Calculate lighting
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

    //Apply fog
    float fogHeightBlend = smoothstep(1000.0, 3.0, worldPos.y * worldPos.y);
    float fogDistanceBlend = clamp(linearDepth / 100, 0.01, 0.7);
    outColor = mix(outColor, vec4(sunColor, 1), fogHeightBlend * fogDistanceBlend);

    //Apply bloom
    outColor.rgb += texture(sceneBloomSampler, inUv).rgb;
}