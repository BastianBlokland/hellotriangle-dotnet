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

//Vertex input
layout(location = 0) in vec3 vertPosition;

//Instance input
layout(location = 5) in mat4 instanceModelMatrix; //Uses location 5, 6, 7, 8 becuase 4 x vec4

//Output
out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    gl_Position = sceneData.viewProjectionMatrix * instanceModelMatrix * vec4(vertPosition, 1.0);
}