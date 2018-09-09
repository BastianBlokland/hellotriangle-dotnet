struct SceneData
{
    int frame;
    float time;
    float deltaTime;
    
    //Pad to 16 bytes alignment
    float padding;
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
    
    //Pad to 16 bytes alignment
    vec2 padding;
};