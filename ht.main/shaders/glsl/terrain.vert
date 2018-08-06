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

//Instance input
layout(location = 5) in mat4 instanceModelMatrix; //Uses location 5, 6, 7, 8 becuase 4 x vec4

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 baseColor;
layout(location = 1) out vec2 worldUv;

void main()
{
    vec2 worldSize = vec2(256, 256);
    float heightmapScale = 40;

    vec4 worldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);
    worldUv = (worldPosition.xz + worldSize * 0.5) / worldSize;
    vec4 terrainTexSample = texture(terrainTexSampler, worldUv);
    
    //Offset the y by the heightmap in the alpha channel
    worldPosition.y += terrainTexSample.a * heightmapScale;

    gl_Position = sceneData.viewProjectionMatrix * worldPosition;
    baseColor = vec4(terrainTexSample.rgb, 1.0);
}