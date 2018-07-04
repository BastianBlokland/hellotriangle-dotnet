#version 450
#extension GL_ARB_separate_shader_objects : enable

//Input
layout(push_constant) uniform SceneData 
{
	mat4 viewProjectionMatrix;
} sceneData;
layout(binding = 0) uniform ObjectData
{
    mat4 modelMatrix;
} objData;

layout(location = 0) in vec3 vertPosition;
layout(location = 1) in vec4 vertColor;
layout(location = 2) in vec3 vertNormal;
layout(location = 3) in vec2 vertUv1;
layout(location = 4) in vec2 vertUv2;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 fragUv;

void main()
{
    gl_Position = sceneData.viewProjectionMatrix * objData.modelMatrix * vec4(vertPosition, 1.0);
    fragUv = vertUv1;
}