#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_vertexinput.glsl"
#include "include_instanceinput.glsl"
#include "include_game.glsl"

//Texture input
layout(binding = 1) uniform sampler2D terrainTexSampler;
layout(binding = 2) uniform sampler2D detail1TexSampler;
layout(binding = 3) uniform sampler2D detail2TexSampler;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 baseColor;
layout(location = 1) out vec2 worldUv;

void main()
{
    vec4 vertWorldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);
    
    //Calculate the world uv based on the world pos
    worldUv = getWorldUv(vertWorldPosition.xyz);
    const vec4 terrainSample = texture(terrainTexSampler, worldUv);

    //Offset by the terrain height
    vertWorldPosition.y += terrainSample.a * heightmapScale;

    gl_Position = sceneData.viewProjectionMatrix * vertWorldPosition;
    baseColor = vec4(terrainSample.rgb, 1.0);
}