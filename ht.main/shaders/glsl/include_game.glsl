#define worldSize vec2(256, 256)
#define heightmapScale 40
#define sunDirection vec3(-0.89, -0.44, 0.0)
#define sunColor vec4(1.0, 0.97, 0.67, 1.0)
#define ambientColor vec4(0.4, 0.35, 0.3, 1.0)

vec2 getWorldUv(vec3 worldPosition)
{
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

vec3 windBend(vec3 worldVertexPos, const vec3 instWorldPositon)
{
    #define distOffset 0.1
    #define frequencies vec4(3.8, 1, 1.7, 0.9)
    #define forces vec4(0.04, 0.04, 0.1, 0.1)

    vec3 localPos = worldVertexPos - instWorldPositon;
    
    const vec4 time = sceneData.time + (vec4(instWorldPositon.xz, instWorldPositon.xz) * distOffset);
    const vec4 windVec = sin(time * frequencies) * forces;
    const float dist = length(localPos);
 
    localPos.xz += (windVec.xz + windVec.yw) * (localPos.y * localPos.y * localPos.y);
    return instWorldPositon + normalize(localPos) * dist;
}