using System.Runtime.InteropServices;

public static class MacNativeFileBrowser
{
    [DllImport("MacNativeFileBrowser")]
    private static extern string _OpenFilePanel(string title, string directory, string extension, bool multiselect);

    [DllImport("MacNativeFileBrowser")]
    private static extern string _OpenFolderPanel(string title, string directory);

    public static string OpenFilePanel(string title, string directory, string extension, bool multiselect)
    {
        return _OpenFilePanel(title, directory, extension, multiselect);
    }

    public static string OpenFolderPanel(string title, string directory)
    {
        return _OpenFolderPanel(title, directory);
    }
}