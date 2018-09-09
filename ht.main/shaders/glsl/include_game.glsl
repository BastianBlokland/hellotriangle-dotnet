#define worldSize vec2(256, 256)
#define heightmapScale 35.0
#define minAmbientColor vec3(0.1, 0.06, 0.05)
#define maxAmbientColor vec3(0.6, 0.5, 0.4)
#define sunColor vec3(0.8, 0.7, 0.5)
#define sunSpecPower 16
#define specMultiplier 1.8

vec2 getWorldUv(vec3 worldPosition)
{
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

vec3 windBend(vec3 worldVertexPos, const vec3 instWorldPositon, float sceneTime)
{
    #define distOffset 0.13
    #define frequencies vec4(4.8, 1.4, 1.9, 1.3)
    #define forces vec4(0.04, 0.04, 0.12, 0.12)

    vec3 localPos = worldVertexPos - instWorldPositon;
    
    const vec4 time = sceneTime + (vec4(instWorldPositon.xz, instWorldPositon.xz) * distOffset);
    const vec4 windVec = sin(time * frequencies) * forces;
    const float dist = length(localPos);
 
    localPos.xz += (windVec.xz + windVec.yw) * (localPos.y * localPos.y * localPos.y);
    return instWorldPositon + normalize(localPos) * dist;
}