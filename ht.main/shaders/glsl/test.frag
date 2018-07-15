#version 450
#extension GL_ARB_separate_shader_objects : enable

//Input
layout(location = 0) in vec2 fragUv;

layout(binding = 2) uniform sampler2D texSampler;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    outColor = texture(texSampler, fragUv);
}