#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 1;
layout(constant_id = 1) const bool isShadowPass = false;

//Uniforms
layout(binding = 0) uniform SceneData sceneData;
layout(binding = 1) uniform CameraData cameraData;
layout(binding = 2) uniform sampler2D colorSampler;
layout(binding = 3) uniform sampler2D normalSampler;
layout(binding = 4) uniform sampler2D terrainSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 outUv;
layout(location = 1) out vec3 outColorTint;
layout(location = 2) out vec3 outWorldPosition;
layout(location = 3) out vec3 outWorldNormal;

vec3 getTint()
{
    const vec3 tints[] = vec3[]
    (
        vec3(1.0, 1.0, 1.0),
        vec3(1.0, 0.9, 0.9),
        vec3(1.0, 0.8, 1.0),
        vec3(1.0, 1.0, 0.8),
        vec3(0.9, 0.9, 0.9),
        vec3(0.9, 1.0, 0.9),
        vec3(1.0, 1.0, 0.8)
    );
    return tints[gl_InstanceIndex % tints.length()];
}

void main()
{
    //Get the instances position from the instance matrix
    const vec3 instanceWorldPositon = getPosition(instanceModelMatrix);

    //Calculate world space position
    vec4 vertWorldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);

    //Apply bending to simulate wind
    vec4 bendWorldPosition = vec4(windBend(vertWorldPosition.xyz, instanceWorldPositon, sceneData.time), 1.0);
    vec3 bendOffset = bendWorldPosition.xyz - vertWorldPosition.xyz;

    //Offset by the terrain height
    bendWorldPosition.y += texture(terrainSampler, getWorldUv(instanceWorldPositon)).a * heightmapScale;

    outUv = vertUv1;
    outColorTint = getTint();
    outWorldNormal = mat3(instanceModelMatrix) * (vertNormal + bendOffset);
    outWorldPosition = bendWorldPosition.xyz;
    gl_Position = cameraData.viewProjectionMatrix * bendWorldPosition;
}