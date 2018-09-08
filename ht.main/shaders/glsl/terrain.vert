#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_game.glsl"

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
layout(binding = 2) uniform sampler2D terrainSampler;
layout(binding = 3) uniform sampler2D detail1ColorSampler;
layout(binding = 4) uniform sampler2D detail1NormalSampler;
layout(binding = 5) uniform sampler2D detail2ColorSampler;
layout(binding = 6) uniform sampler2D detail2NormalSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 outBaseColor;
layout(location = 1) out vec2 outWorldUv;
layout(location = 2) out vec3 outWorldPosition;
layout(location = 3) out vec3 outWorldNormal;

void main()
{
    vec4 vertWorldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);
    
    //Calculate the world uv based on the world pos
    outWorldUv = getWorldUv(vertWorldPosition.xyz);

    //Main sample at this position
    const vec4 terrainSample = texture(terrainSampler, outWorldUv);

    //Calculate the world normal by taking samples around this location and normalizing the 
    //difference. Note: it uses '2' in z because the samples are '2' units away
    float leftHeight = textureOffset(terrainSampler, outWorldUv, ivec2(-1, 0)).a * heightmapScale;
    float rightHeight = textureOffset(terrainSampler, outWorldUv, ivec2(1, 0)).a * heightmapScale;
    float downHeight = textureOffset(terrainSampler, outWorldUv, ivec2(0, -1)).a * heightmapScale;
    float upHeight = textureOffset(terrainSampler, outWorldUv, ivec2(0, 1)).a * heightmapScale;
    outWorldNormal = normalize(vec3(leftHeight - rightHeight, 2.0, downHeight - upHeight));

    //Offset by the terrain height
    vertWorldPosition.y += terrainSample.a * heightmapScale;

    outBaseColor = vec4(terrainSample.rgb, 1.0);
    outWorldPosition = vertWorldPosition.xyz;
    gl_Position = cameraData[pushdata.swapchainIndex].viewProjectionMatrix * vertWorldPosition;
}