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
layout(binding = 1) uniform sampler2D colorTexSampler;
layout(binding = 2) uniform sampler2D terrainTexSampler;

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
layout(location = 0) out vec2 colorUv;

vec3 GetPosition(const mat4 matrix)
{
    return matrix[3].xyz;
}

void main()
{
    vec2 worldSize = vec2(256, 256);
    float heightmapScale = 40;

    //Get the 'worldUv' for this instance
    vec3 instanceWorldPositon = GetPosition(instanceModelMatrix);
    vec2 worldUv = (instanceWorldPositon.xz + worldSize * 0.5) / worldSize;
    
    //Offset the y coordinate by the terrain heightmap 
    vec4 worldPosition = instanceModelMatrix * vec4(vertPosition, 1.0);
    worldPosition.y += texture(terrainTexSampler, worldUv).a * heightmapScale;

    //Output
    colorUv = vertUv1;
    gl_Position =  sceneData.viewProjectionMatrix * worldPosition;
}