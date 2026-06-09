using System;
using System.Runtime.InteropServices;

public static class MacNativeFileBrowser
{
    // Return IntPtr instead of string to prevent Mono from calling free() on the
    // native pointer. The plugin returns pointers into NSString UTF8String buffers
    // (autoreleased, not malloc'd), so freeing them causes SIGABRT.
    [DllImport("MacNativeFileBrowser")]
    private static extern IntPtr _OpenFilePanel(string title, string directory, string extension, bool multiselect);

    [DllImport("MacNativeFileBrowser")]
    private static extern IntPtr _OpenFolderPanel(string title, string directory);

    public static string OpenFilePanel(string title, string directory, string extension, bool multiselect)
    {
        return Marshal.PtrToStringAnsi(_OpenFilePanel(title, directory, extension, multiselect));
    }

    public static string OpenFolderPanel(string title, string directory)
    {
        return Marshal.PtrToStringAnsi(_OpenFolderPanel(title, directory));
    }
}