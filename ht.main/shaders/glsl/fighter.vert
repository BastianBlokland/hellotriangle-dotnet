
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
layout(location = 1) out vec4 additiveColor;

mat3 xRotMatrix(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return mat3(1.0,    0.0,    0.0,
                0.0,    c,      -s,
                0.0,    s,      c);
}

mat3 zRotMatrix(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return mat3(c,      -s,     0.0,
                s,      c,      0.0,
                0.0,    0.0,    1.0);
}

void main()
{
    vec4 vertPos = vec4(vertPosition, 1.0);

    //Wobble effect
    const vec2 wobbleFrequencies = vec2(1.2, 1.5);
    const vec2 wobbleAngles = 
        sin((sceneData.time + gl_InstanceIndex) * wobbleFrequencies) * vec2(0.06, 0.08);
    vertPos.xyz = xRotMatrix(wobbleAngles.x) * zRotMatrix(wobbleAngles.y) * vertPos.xyz;

    //Exhaust effect
    //red channel contains mask for exhaust vertices
    //green channel of vert color contains exhaust intensity
    const float minExhaustScale = 1.5;
    const float maxExhaustScale = 3.5;
    const float frequency = 20.0;
    const float exhaustIntensity = 
        abs(sin((vertPos.x + vertPos.y + sceneData.time + (gl_InstanceIndex * 0.5312)) * frequency));
    
    const vec4 hotExhaustColor = vec4(0.9, 0.9, 2.0, 1.0);
    const vec4 coldExhaustColor = vec4(0.2, 0.2, 2.0, 1.0);
    vertPos.z -= vertColor.g * mix(minExhaustScale, maxExhaustScale, exhaustIntensity);
    additiveColor = mix(
        hotExhaustColor,
        coldExhaustColor,
        vertColor.g * exhaustIntensity) * vertColor.r;

    colorUv = vertUv1;
    gl_Position = sceneData.viewProjectionMatrix * instanceModelMatrix * vertPos;
}