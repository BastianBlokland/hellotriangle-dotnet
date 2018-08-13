layout(binding = 0) uniform CameraData 
{
    mat4 cameraMatrix;
	mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 viewProjectionMatrix;
    float nearClipDistance;
    float farClipDistance;
} cameraData;

layout(binding = 1) uniform SceneData 
{
    int frame;
    float time;
    float deltaTime;
} sceneData;

float LinearizeDepth(float depth)
{
    float near = cameraData.nearClipDistance;
    float far = cameraData.farClipDistance;
    float z = depth * 2.0 - 1.0; // back to NDC
    return (2.0 * near * far) / (far + near - z * (far - near));
}