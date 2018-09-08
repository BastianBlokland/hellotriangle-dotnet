struct SceneData
{
    int frame;
    float time;
    float deltaTime;
};

struct CameraData
{
    mat4 cameraMatrix;
    mat4 viewMatrix;
    mat4 projectionMatrix;
    mat4 viewProjectionMatrix;
    mat4 inverseViewProjectionMatrix;
    float nearClipDistance;
    float farClipDistance;
};