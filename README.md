# hellotriangle-dotnet
Work in progress dotnet vulkan renderer

# structure
Supported platforms are Windows and MacOS at the moment. Using dotnet-core so base code is portable to loads of platforms.
For platform specific code (for example window management) there are thin wrappers around the os functions.

General project structure is that platform-specific code lives in 'launcher' projects, for example: platforms/ht.win32 or platforms/ht.macos

Platform specific code:
- Win32 window management: Bindings to the user32 dll (should be compatible with loads of windows versions)
- MacOS window management: Thin obj-c wrapper around Cocoa (Source available here: [macwindow-dotnet](https://github.com/BastianBlokland/macwindow-dotnet))

Note: MacOS runs through the MoltenVK wrapper to run vulkan on metal devices.

# environment setup
- Install dotnet-core 2.1 sdk (https://www.microsoft.com/net/download)
- Install the vulkan sdk (https://vulkan.lunarg.com/sdk/home)
    Mac-note: you need to set some environment variables: (the getting started guide in the sdk contains more info)
        -Add 'vulkansdk/macOS/bin' to the PATH environment variable (used for things like the 'glslangValidator')
        -Set 'vulkansdk/macOS/lib' to the variable: 'DYLD_LIBRARY_PATH' (used by the vulkan-loader)
        -Set 'vulkansdk/macOS/etc/vulkan/explicit_layer.d' to the variable: 'VK_LAYER_PATH' (used by the vulkan-loader)
        -Set 'vulkansdk/macOS/etc/vulkan/icd.d/MoltenVK_icd.json' to the variable: 'VK_ICD_FILENAMES' (used by the vulkan-loader)
- Windows: Set script execution policy, because for shader compilation it uses a small powershell script
    run 'set-executionpolicy remotesigned' in a shell with administrator rights, more info can be found here: http://go.microsoft.com/fwlink/?LinkID=135170

# ide setup
Project is setup for VSCode usage on both Windows and MacOS, this readme assumes VSCode use but any ide should work as it only requires dotnet cli 
for building and some simple bash / powershell scripts for asset building.
- Install vscode (https://code.visualstudio.com/)
- Some extensions that will make your life better:
    - 'C#' (https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
    - 'Shader languages support for VS Code' (https://marketplace.visualstudio.com/items?itemName=slevesque.shader)
    - 'VS code SPIR-V (glsl/hlsl) shaders tools' (https://marketplace.visualstudio.com/items?itemName=elviras9t.vscode-shadercode)
    - Very optional but i can't go without: 'Code Spell Checker' (https://marketplace.visualstudio.com/items?itemName=streetsidesoftware.code-spell-checker)
        Note: Workspace settings actually already contains a white-listed words list for this project

# building
- Debugger: There are 'win32' and 'macos' entries in the launch.json that automatically pick the right launcher
- Running: Tasks (menu -> Tasks -> Run Task...) have been setup for running in both Debug and Release mode for both win32 and macos
- Publishing: There is a 'publish-win32' and a 'publish-macos' task that package up the app into a root/build directory.
    -Note: The published app already contains the netcore runtime so user don't have to have the runtime installed
    -Note: The published app does not contain the vulkan library so people will have to install the vulkan runtime

Note: When building it automatically compiles the shaders from glsl to spr-v and includes the spr-v in the build. If you want to
run it manually there is a task 'build-shaders' which calls tools/compile-shaders.ps1 on windows and compile-shaders.sh on macos

# credits
- C# vulkan bindings [VulkanCore](https://github.com/discosultan/VulkanCore) made by [discosultan](https://github.com/discosultan)

Sample assets:
- Skybox textures (Heiko Irrgang, https://93i.de/p/free-skybox-texture-set/)
- OpenGameArt (https://opengameart.org/)
- TurboSquid https://www.turbosquid.com/
