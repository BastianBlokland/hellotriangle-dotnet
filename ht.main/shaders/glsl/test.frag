#version 450
#extension GL_ARB_separate_shader_objects : enable

//Input
layout(location = 0) in vec4 fragColor;

//Output
layout(location = 0) out vec4 outColor;

void main() 
{
    outColor = fragColor;
}