#define CameraData CameraData           \
{                                       \
    mat4 inverseViewMatrix;             \
	mat4 viewMatrix;                    \
    mat4 projectionMatrix;              \
    mat4 viewProjectionMatrix;          \
    mat4 inverseViewProjectionMatrix;   \
    float nearClipDistance;             \
    float farClipDistance;              \
}

#define ShadowData ShadowData           \
{                                       \
    mat4 inverseViewMatrix;             \
	mat4 viewMatrix;                    \
    mat4 projectionMatrix;              \
    mat4 viewProjectionMatrix;          \
    mat4 inverseViewProjectionMatrix;   \
    float nearClipDistance;             \
    float farClipDistance;              \
}

#define SceneData SceneData             \
{                                       \
    int frame;                          \
    float time;                         \
    float deltaTime;                    \
}