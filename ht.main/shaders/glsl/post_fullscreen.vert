#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    //Fullscreen triangle, more info: https://www.saschawillems.de/?page_id=2122
    gl_Position = vec4((gl_VertexIndex << 1 & 2) * 2.0 - 1, (gl_VertexIndex & 2) * 2.0 - 1, 1, 1);
}