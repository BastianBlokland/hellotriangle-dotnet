#define worldSize vec2(256, 256)
#define heightmapScale 40
#define ambientColor vec3(0.45, 0.39, 0.34)
#define sunColor vec3(0.8, 0.7, 0.5)
#define sunSpecPower 16
#define specMultiplier 1.5

vec2 getWorldUv(vec3 worldPosition)
{
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

vec3 windBend(vec3 worldVertexPos, const vec3 instWorldPositon, float sceneTime)
{
    #define distOffset 0.1
    #define frequencies vec4(3.8, 1, 1.7, 0.9)
    #define forces vec4(0.04, 0.04, 0.1, 0.1)

    vec3 localPos = worldVertexPos - instWorldPositon;
    
    const vec4 time = sceneTime + (vec4(instWorldPositon.xz, instWorldPositon.xz) * distOffset);
    const vec4 windVec = sin(time * frequencies) * forces;
    const float dist = length(localPos);
 
    localPos.xz += (windVec.xz + windVec.yw) * (localPos.y * localPos.y * localPos.y);
    return instWorldPositon + normalize(localPos) * dist;
}