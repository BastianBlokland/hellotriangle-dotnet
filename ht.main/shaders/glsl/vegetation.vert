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
layout(location = 1) out vec4 colorTint;

vec3 getPosition(const mat4 matrix) { return matrix[3].xyz; }

vec3 windBend(vec3 worldVertexPos, const vec3 instWorldPositon)
{
    const float distOffset = 0.1;
    const vec4 frequencies = vec4(3.8, 1, 1.7, 0.9);
    const vec4 forces = vec4(0.04, 0.04, 0.1, 0.1);

    vec3 localPos = worldVertexPos - instWorldPositon;
    
    const vec4 time = sceneData.time + (vec4(instWorldPositon.xz, instWorldPositon.xz) * distOffset);
    const vec4 windVec = sin(time * frequencies) * forces;
    const float dist = length(localPos);
 
    localPos.xz += (windVec.xz + windVec.yw) * (localPos.y * localPos.y * localPos.y);
    return instWorldPositon + normalize(localPos) * dist;
}

vec2 getWorldUv(vec3 worldPosition)
{
    const vec2 worldSize = vec2(256, 256);
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

float getTerrainHeight(vec2 worldUv)
{
    const float heightmapScale = 40;
    return texture(terrainTexSampler, worldUv).a * heightmapScale;
}

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
    vertWorldPosition.xyz = windBend(vertWorldPosition.xyz, instanceWorldPositon);

    //Offset by the terrain height
    vertWorldPosition.y += getTerrainHeight(getWorldUv(instanceWorldPositon));

    colorUv = vertUv1;
    colorTint = getTint();
    gl_Position = sceneData.viewProjectionMatrix * vertWorldPosition;
}