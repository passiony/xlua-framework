using UnityEngine;
using UnityEditor;
using System.IO;
using AssetBundles;

[InitializeOnLoad]
public static class PuertsMenu
{
    [MenuItem("Puerts/Copy TS Files To AssetsPackage", false, 51)]
    public static void CopyLuaFilesToAssetsPackage()
    {
        string destination = Path.Combine(Application.dataPath, AssetBundleConfig.AssetsFolderName);
        destination = Path.Combine(destination, PuertsManager.tsAssetbundleAssetName);
        string source = Path.Combine(Application.dataPath, PuertsManager.tsScriptsFolder);
        GameUtility.SafeDeleteDir(destination);

        FileUtil.CopyFileOrDirectoryFollowSymlinks(source, destination);

        var notLuaFiles = GameUtility.GetSpecifyFilesInFolder(destination, new string[] { ".ts", ".pb" }, true);
        if (notLuaFiles != null && notLuaFiles.Length > 0)
        {
            for (int i = 0; i < notLuaFiles.Length; i++)
            {
                GameUtility.SafeDeleteFile(notLuaFiles[i]);
            }
        }

        var luaFiles = GameUtility.GetSpecifyFilesInFolder(destination, new string[] { ".ts" ,".pb" }, false);
        if (luaFiles != null && luaFiles.Length > 0)
        {
            for (int i = 0; i < luaFiles.Length; i++)
            {
                GameUtility.SafeRenameFile(luaFiles[i], luaFiles[i] + ".bytes");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Copy ts files over");
    }
}
