struct SceneData
{
    int frame;
    float time;
    float deltaTime;
    //float padding[1];
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
    //float padding[2];
};