const vec2 worldSize = vec2(256, 256);
const float heightmapScale = 40;

vec2 getWorldUv(vec3 worldPosition)
{
    return (worldPosition.xz + worldSize * 0.5) / worldSize;
}

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