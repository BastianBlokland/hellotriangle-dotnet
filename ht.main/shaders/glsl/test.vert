#version 450
#extension GL_ARB_separate_shader_objects : enable

//Input
layout(location = 0) in vec3 vertPosition;
layout(location = 1) in vec4 vertColor;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec4 fragColor;

void main()
{
    gl_Position = vec4(vertPosition, 1.0);
    fragColor = vertColor;
}