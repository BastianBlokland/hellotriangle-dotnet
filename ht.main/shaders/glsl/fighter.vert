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
layout(binding = 2) uniform sampler2D exhaustTexSampler;

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

void main()
{
    //Settings
    float exhaustMult = clamp(instanceAge, 0, 1);
    float exhaustScale = 2;
    vec4 exhaustColor = vec4(1, 1, 5, 1);
    
    //Logic
    vec4 exhaustSample = texture(exhaustTexSampler, vec2(instanceAge, vertUv2.y));
    float heightMap = exhaustSample.r;
    float colorMultiplier = exhaustSample.g;

    //Calculate position (heightmap pushes vertices in the negative z axis for getting a exhaust effect)
    vec3 modelPos = vertPosition - vec3(0, 0, heightMap * exhaustScale * exhaustMult);
    vec4 worldPos = instanceModelMatrix * vec4(modelPos, 1);

    //Output
    colorUv = vertUv1;
    additiveColor = exhaustColor * colorMultiplier * exhaustMult;
    gl_Position = sceneData.viewProjectionMatrix * worldPos;
}