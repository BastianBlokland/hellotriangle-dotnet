#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_GOOGLE_include_directive : enable
#include "include_sceneinput.glsl"
#include "include_math.glsl"

//Uniforms
layout(binding = 0) uniform CameraData cameraData;
layout(binding = 1) uniform SceneData sceneData;

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};
layout(location = 0) out vec2 outUv;
layout(location = 1) out vec3 outWorldViewDirection;

void main()
{
    //Fullscreen triangle, more info: https://www.saschawillems.de/?page_id=2122
    outUv = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
	gl_Position = vec4(outUv * 2.0 - 1.0, 1.0, 1.0);

    //Calculate the world-space view direction
    //NOTE: This is NOT normalized yet
    vec3 viewSpaceVertPos = (inverse(cameraData.projectionMatrix) * gl_Position).xyz;
    outWorldViewDirection = mat3(cameraData.cameraMatrix) * viewSpaceVertPos;
}