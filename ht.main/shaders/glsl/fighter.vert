#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_math.glsl"

//Texture input
layout(binding = 1) uniform sampler2D colorTexSampler;
layout(binding = 2) uniform sampler2D normalTexSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 colorUv;
layout(location = 1) out float exhaustIntensity;
layout(location = 2) out float exhaustMask;
layout(location = 3) out vec3 worldPosition;
layout(location = 4) out vec3 worldNormal;

void main()
{
    //Wobble effect
    const vec2 wobbleFrequencies = vec2(1.2, 1.5);
    const vec2 wobbleAngles = 
        sin((sceneData.time + gl_InstanceIndex) * wobbleFrequencies) * vec2(0.06, 0.08);
    mat3 wobbleMatrix = xRotMatrix(wobbleAngles.x) * zRotMatrix(wobbleAngles.y);
    
    //Apply wobble matrix
    vec3 adjustedPos = wobbleMatrix * vertPosition;
    vec3 adjustedNorm = wobbleMatrix * vertNormal;

    //Exhaust effect
    //red channel contains mask for exhaust vertices
    //green channel of vert color contains exhaust intensity
    const float minExhaustScale = 1.5;
    const float maxExhaustScale = 3.5;
    const float frequency = 20.0;
    exhaustIntensity = vertColor.g *
        abs(sin((adjustedPos.x + adjustedPos.y + sceneData.time + (gl_InstanceIndex * 0.5312)) * frequency));
    adjustedPos.z -= vertColor.g * mix(minExhaustScale, maxExhaustScale, exhaustIntensity);
    exhaustMask = vertColor.r;

    colorUv = vertUv1;
    worldPosition = (instanceModelMatrix * vec4(adjustedPos, 1.0)).xyz;
    worldNormal = mat3(instanceModelMatrix) * adjustedNorm;
    gl_Position = sceneData.viewProjectionMatrix * vec4(worldPosition, 1.0);
}