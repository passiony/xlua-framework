using System.IO;
using AssetBundles;
using Puerts;
using UnityEngine;

public class CustomLoader : ILoader
{
    private const string jsAssetbundleAssetName = "Js";
    private const string tsAssetbundleAssetName = "Ts";
    private const string jsScriptsFolder = "JsScripts";

    private string root = "";

    public CustomLoader()
    {
        root = Path.Combine(Application.dataPath, jsScriptsFolder);
    }
    
    private string PathToUse(string filepath)
    {
        return filepath.EndsWith(".cjs") ? 
            filepath.Substring(0, filepath.Length - 4) : 
            filepath;
    }
    
    public bool FileExists(string filepath)
    {
//#if PUERTS_GENERAL
            return File.Exists(Path.Combine(root, filepath));
//#else
//        string pathToUse = this.PathToUse(filepath);
//        return UnityEngine.Resources.Load(pathToUse) != null;
//#endif
    }

    public string ReadFile(string filepath, out string debugpath)
    {
//#if PUERTS_GENERAL
//            debugpath = Path.Combine(root, filepath);
//            return File.ReadAllText(debugpath);
//#else
//        string pathToUse = this.PathToUse(filepath);
//        UnityEngine.TextAsset file = (UnityEngine.TextAsset)UnityEngine.Resources.Load(pathToUse);
//        debugpath = System.IO.Path.Combine(root, filepath);
//#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
//        debugpath = debugpath.Replace("/", "\\");
//#endif
//        return file == null ? null : file.text;
//#endif
        debugpath = string.Empty;
//        filepath = filepath.Replace(".", "/") + ".lua";
#if UNITY_EDITOR
        if (AssetBundleConfig.IsEditorMode)
        {
            debugpath = Path.Combine(Application.dataPath, jsScriptsFolder);
            debugpath = Path.Combine(debugpath, filepath);
            //Logger.Log("Load lua script : " + scriptPath);
            return GameUtility.SafeReadAllText(debugpath);
        }
#endif

        debugpath = string.Format("{0}/{1}.bytes", jsAssetbundleAssetName, filepath);
        string assetbundleName = null;
        string assetName = null;
        bool status = AssetBundleManager.Instance.MapAssetPath(debugpath, out assetbundleName, out assetName);
        if (!status)
        {
            Logger.LogError("MapAssetPath failed : " + debugpath);
            return null;
        }
        var asset = AssetBundleManager.Instance.GetAssetCache(assetName) as TextAsset;
        if (asset != null)
        {
            //Logger.Log("Load lua script : " + scriptPath);
            return asset.text;
        }
        Logger.LogError("Load lua script failed : " + debugpath + ", You should preload lua assetbundle first!!!");
        return null;
    }
}
