#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_math.glsl"

//Specialization
layout(constant_id = 0) const int swapchainCount = 3;
layout(constant_id = 1) const bool isShadowPass = false;

//PushData
layout(push_constant) uniform PushData
{
    int swapchainIndex;
} pushdata;

//Uniforms
layout(binding = 0) uniform SceneDataBlock { SceneData sceneData[swapchainCount]; };
layout(binding = 1) uniform CameraDataBlock { CameraData cameraData[swapchainCount]; };
layout(binding = 2) uniform sampler2D colorSampler;
layout(binding = 3) uniform sampler2D normalSampler;

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
    const float time = sceneData[pushdata.swapchainIndex].time;

    //Wobble effect
    #define wobbleFrequencies vec2(1.2, 1.5)
    #define wobbleStrenghts vec2(0.06, 0.08)
    const vec2 wobbleAngles = sin((time + gl_InstanceIndex) * wobbleFrequencies) * wobbleStrenghts;
    mat3 wobbleMatrix = xRotMatrix(wobbleAngles.x) * zRotMatrix(wobbleAngles.y);
    
    //Apply wobble matrix
    vec3 adjustedPos = wobbleMatrix * vertPosition;
    vec3 adjustedNorm = wobbleMatrix * vertNormal;

    //Exhaust effect (offset the vertices of the exausted based on mask in vertext colors)
    //red channel contains mask, green channel contains intensity
    //Note: Don't apply effect during the shadow pass as flames are not supposed to cast shadows :)
    if (!isShadowPass)
    {
        #define minExhaustScale 1.5
        #define maxExhaustScale 3.5
        #define frequency 20.0
        outExhaustIntensity = vertColor.g *
            abs(sin((adjustedPos.x + adjustedPos.y + time + (gl_InstanceIndex * 0.5312)) * frequency));
        adjustedPos.z -= vertColor.g * mix(minExhaustScale, maxExhaustScale, outExhaustIntensity);
        outExhaustMask = vertColor.r;
    }
    else
    {
        outExhaustIntensity = 0.0;
        outExhaustMask = 0.0;
    }

    outUv = vertUv1;
    outWorldPosition = (instanceModelMatrix * vec4(adjustedPos, 1.0)).xyz;
    outWorldNormal = mat3(instanceModelMatrix) * adjustedNorm;
    gl_Position = 
        cameraData[pushdata.swapchainIndex].viewProjectionMatrix *
        vec4(outWorldPosition, 1.0);
}