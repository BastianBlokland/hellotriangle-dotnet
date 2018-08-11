#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_math.glsl"
#include "include_game.glsl"

//Texture input
layout(binding = 1) uniform sampler2D colorTexSampler;
layout(binding = 2) uniform sampler2D normalTexSampler;
layout(binding = 3) uniform sampler2D terrainTexSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 colorUv;
layout(location = 1) out vec4 colorTint;
layout(location = 2) out vec3 worldPosition;
layout(location = 3) out vec3 worldNormal;

vec4 getTint()
{
    const vec4 tints[] = vec4[]
    (
        vec4(1.0, 1.0, 1.0, 0.98),
        vec4(1.0, 0.9, 0.9, 0.92),
        vec4(1.0, 0.8, 1.0, 0.97),
        vec4(1.0, 1.0, 0.8, 0.91),
        vec4(0.9, 0.9, 0.9, 0.96),
        vec4(0.9, 1.0, 0.9, 0.94),
        vec4(1.0, 1.0, 0.8, 0.92)
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
    vec4 bendWorldPosition = vec4(windBend(vertWorldPosition.xyz, instanceWorldPositon), 1.0);
    vec3 bendOffset = bendWorldPosition.xyz - vertWorldPosition.xyz;

    //Offset by the terrain height
    bendWorldPosition.y += texture(terrainTexSampler, getWorldUv(instanceWorldPositon)).a * heightmapScale;

    colorUv = vertUv1;
    colorTint = getTint();
    worldNormal = mat3(instanceModelMatrix) * (vertNormal + bendOffset);
    worldPosition = bendWorldPosition.xyz;
    gl_Position = sceneData.viewProjectionMatrix * bendWorldPosition;
}