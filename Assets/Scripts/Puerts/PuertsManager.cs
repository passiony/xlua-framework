using System;
using AssetBundles;
using Puerts;

public class PuertsManager : MonoSingleton<PuertsManager>
{
    public const string tsAssetbundleAssetName = "Ts";
    public const string tsScriptsFolder = "TsScripts";
    const string commonMainScriptName = "Common.Main";
    const string gameMainScriptName = "GameMain.ts";
    JsEnv jsEnv = null;
    public Action onLevelWasLoaded;
    public Action onApplicationQuit;
    
    public string AssetbundleName
    {
        get;
        protected set;
    }
    public bool HasGameStart
    {
        get;
        protected set;
    }
    public JsEnv GetJsEnv()
    {
        return jsEnv;
    }
    
    // Start is called before the first frame update
    public void Start()
    {
        base.Init();
        string path = AssetBundleUtility.PackagePathToAssetsPath(tsAssetbundleAssetName);
        AssetbundleName = AssetBundleUtility.AssetBundlePathToAssetBundleName(path);
        InitJsEnv();
    }

    // Update is called once per frame
    void InitJsEnv()
    {
        jsEnv = new JsEnv(new CustomLoader());
        HasGameStart = false;
    }
    
    public void OnInit()
    {
        if (jsEnv != null)
        {
//            LoadScript(commonMainScriptName);
//            luaUpdater = gameObject.GetComponent<LuaUpdater>();
//            if (luaUpdater == null)
//            {
//                luaUpdater = gameObject.AddComponent<LuaUpdater>();
//            }
//            luaUpdater.OnInit(luaEnv);
        }
    }
    
    public void StartGame()
    {
        if (jsEnv != null)
        {
            LoadScript(gameMainScriptName);
            HasGameStart = true;
        }
    }
    
    public void Restart()
    {
        Dispose();
        InitJsEnv();
        OnInit();
    }
    
    void LoadScript(string scriptName)
    {
        SafeDoString(string.Format("require('{0}')", scriptName));
    }
        
    public void SafeDoString(string scriptContent)
    {
        if (jsEnv != null)
        {
            try
            {
                jsEnv.Eval(scriptContent);
            }
            catch (System.Exception ex)
            {
                string msg = string.Format("ts exception : {0}\n {1}", ex.Message, ex.StackTrace);
                Logger.LogError(msg, null);
            }
        }
    }
    
    private void OnLevelWasLoaded()
    {
        if (jsEnv != null && HasGameStart)
        {
            onLevelWasLoaded?.Invoke();
        }
    }

    private void OnApplicationQuit()
    {
        if (jsEnv != null && HasGameStart)
        {
            onApplicationQuit?.Invoke();
        }
    }

    private void Update()
    {
        jsEnv?.Tick();
    }

    public override void Dispose()
    {
//        if (luaUpdater != null)
//        {
//            luaUpdater.OnDispose();
//        }
        if (jsEnv != null)
        {
            try
            {
                jsEnv.Dispose();
                jsEnv = null;
            }
            catch (Exception ex)
            {
                string msg = string.Format("ts exception : {0}\n {1}", ex.Message, ex.StackTrace);
                Logger.LogError(msg, null);
            }
        }
    }
}
