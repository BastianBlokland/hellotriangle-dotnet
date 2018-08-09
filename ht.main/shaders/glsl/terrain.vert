#version 450
#extension GL_ARB_separate_shader_objects : enable

//Scene input
layout(binding = 0) uniform SceneData 
{
    mat4 cameraMatrix;
	mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 viewProjectionMatrix;
    int frame;
    float time;
    float deltaTime;
} sceneData;

//Texture input
layout(binding = 1) uniform sampler2D terrainTexSampler;
layout(binding = 2) uniform sampler2D detail1TexSampler;
layout(binding = 3) uniform sampler2D detail2TexSampler;

//Vertex input
layout(location = 0) in vec3 vertPosition;
layout(location = 1) in vec4 vertColor;
layout(location = 2) in vec3 vertNormal;
layout(location = 3) in vec2 vertUv1;
layout(location = 4) in vec2 vertUv2;

//Instance input
layout(location = 5) in mat4 instanceModelMatrix; //Uses location 5, 6, 7, 8 becuase 4 x vec4
layout(location = 9) in float instanceAge;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 baseColor;
layout(location = 1) out vec2 worldUv;

vec2 getWorldUv(vec3 worldPosition)
{
    const vec2 worldSize = vec2(256, 256);
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

void main()
{
    vec4 vertWorldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);
    
    //Calculate the world uv based on the world pos
    worldUv = getWorldUv(vertWorldPosition.xyz);
    const vec4 terrainSample = texture(terrainTexSampler, worldUv);

    //Offset by the terrain height
    const float heightmapScale = 40;
    vertWorldPosition.y += terrainSample.a * heightmapScale;

    gl_Position = sceneData.viewProjectionMatrix * vertWorldPosition;
    baseColor = vec4(terrainSample.rgb, 1.0);
}