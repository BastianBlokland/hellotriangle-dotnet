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
    #define blurSize 2.0

    vec2 texelSize = 1.0 / textureSize(sceneShadowSampler, 0);
    vec4 clipPos = shadowData.viewProjectionMatrix * vec4(worldPos, 1.0);
    vec2 baseShadowCoord = clipPos.xy * 0.5 + 0.5; //To texture space

    if (clipPos.z < -1.0 || clipPos.z > 1.0) //Outside depth range
       return 0.0;

    //Take multiple samples with a offset to apply blurring for softer shadows
    float shadowSum = 0.0;
    int count = 0;
    for (int y = -1; y <= 1; y += 1)
    for (int x = -1; x <= 1; x += 1)
    {
        vec2 shadowCoord = baseShadowCoord + vec2(x * blurSize, y * blurSize) * texelSize;
        shadowSum += float(texture(sceneShadowSampler, shadowCoord).r  < clipPos.z);
        count++;
    }
    return shadowSum / count;
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
    vec3 worldPos = worldPosFromDepth(sceneDepthSampler, inUv, cameraData.inverseViewProjectionMatrix);
    float emmisiveness = normalAndEmmisiveness.a;
    vec3 sunDirection = mat3(shadowData.inverseViewMatrix) * vec3(0.0, 0.0, 1.0);

    //Calculate shadow
    float shadow = getShadow(worldPos);

    //Calculae lighting
    vec3 litResult = color * ambientColor; //Start with the ambient

    //Add the sun intensity to the result
    float sunIntensity = max(dot(normal, sunDirection), 0.0) * (1.0 - shadow); 
    litResult += sunColor * color * sunIntensity;

    //Add the sun specular to the result
    if (sunIntensity > 0.0)
    {
        vec3 halfDir = normalize(sunDirection - viewDir);
        float specAngle = max(dot(normal, halfDir), 0.0);
        float specular = pow(specAngle, sunSpecPower) * specIntensity;
        litResult += sunColor * specular;
    }

    //Emmisiveness decides how much of the raw unlit color we use
    outColor = vec4(mix(litResult, color, emmisiveness), 1.0);
}