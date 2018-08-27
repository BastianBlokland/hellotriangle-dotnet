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
layout(location = 0) out vec2 outUv;

void main()
{
    //Fullscreen triangle, more info: https://www.saschawillems.de/?page_id=2122
    outUv = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
	gl_Position = vec4(outUv * 2.0 - 1.0, 1.0, 1.0);
}