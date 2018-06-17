# hellotriangle-dotnet
Work in progress dotnet vulkan renderer

# structure
Supported platforms are Windows and MacOS at the moment. All projects target netstandard / netcoreapp so 
pure code is portable to loads of platforms. For platform specific code (for example window management) there
are thin wrappers around the os functions. In my opinion full dotnet is not worth it just for window-management,
using pure netcore code allows use of the dotnet cli for building and also allows creating of windows executables
from mac-side and vise versa. 

General project structure is that platform-specific code lives in 'launcher' projects, for example: platforms/ht.win32 or platforms/ht.macos

Platform specific code:
- Win32 window management: Bindings to the user32 dll (should be compatible with loads of windows versions)
- MacOS window management: Thin obj-c wrapper around Cocoa (Source available here: [macwindow-dotnet](https://github.com/BastianBlokland/macwindow-dotnet))

Note: MacOS runs through the MoltenVK wrapper to run vulkan on metal devices.

# setup
- Install dotnet-core 2.1 sdk (https://www.microsoft.com/net/download)
- Install the vulkan sdk (https://vulkan.lunarg.com/sdk/home)
    Mac-note: On macos this will automatically include molten-vk now
- Make sure the vulkan library (win: vulkan-1.dll, macos: libMoltenVK.dylib) is linked to a os lib folder.
    Win-note: On windows side the installer should do this automatically.
    Mac-note: On macos create a link from [sdkroot]/MoltenVK/macOSlibMoltenVK.dylib to /usr/lib/libMoltenVK.dylib



# credits
C# vulkan bindings [VulkanCore](https://github.com/discosultan/VulkanCore) made by [discosultan](https://github.com/discosultan)
