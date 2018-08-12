#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_math.glsl"

//Textures
layout(binding = 1) uniform sampler2D colorSampler;
layout(binding = 2) uniform sampler2D normalSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 outUv;
layout(location = 1) out float outExhaustIntensity;
layout(location = 2) out float outExhaustMask;
layout(location = 3) out vec3 outWorldPosition;
layout(location = 4) out vec3 outWorldNormal;

void main()
{
    //Wobble effect
    #define wobbleFrequencies vec2(1.2, 1.5)
    #define wobbleStrenghts vec2(0.06, 0.08)
    const vec2 wobbleAngles = 
        sin((sceneData.time + gl_InstanceIndex) * wobbleFrequencies) * wobbleStrenghts;
    mat3 wobbleMatrix = xRotMatrix(wobbleAngles.x) * zRotMatrix(wobbleAngles.y);
    
    //Apply wobble matrix
    vec3 adjustedPos = wobbleMatrix * vertPosition;
    vec3 adjustedNorm = wobbleMatrix * vertNormal;

    //Exhaust effect
    //red channel contains mask for exhaust vertices
    //green channel of vert color contains exhaust intensity
    #define minExhaustScale 1.5
    #define maxExhaustScale 3.5
    #define frequency 20.0
    outExhaustIntensity = vertColor.g *
        abs(sin((adjustedPos.x + adjustedPos.y + sceneData.time + (gl_InstanceIndex * 0.5312)) * frequency));
    adjustedPos.z -= vertColor.g * mix(minExhaustScale, maxExhaustScale, outExhaustIntensity);
    outExhaustMask = vertColor.r;

    outUv = vertUv1;
    outWorldPosition = (instanceModelMatrix * vec4(adjustedPos, 1.0)).xyz;
    outWorldNormal = mat3(instanceModelMatrix) * adjustedNorm;
    gl_Position = sceneData.viewProjectionMatrix * vec4(outWorldPosition, 1.0);
}