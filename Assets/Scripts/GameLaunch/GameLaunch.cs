using UnityEngine;
using System.Collections;
using AssetBundles;
using GameChannel;
using System;

public class GameLaunch : MonoBehaviour
{
    const string launchPrefabPath = "UI/Prefabs/UILoading/UILoading.prefab";
    const string noticeTipPrefabPath = "UI/Prefabs/Common/UINoticeTip.prefab";
    const string LaunchLayerPath = "UIRoot/TopLayer";
    
    GameObject launchPrefab;
    GameObject noticeTipPrefab;
    AssetbundleUpdater updater;
    string channelName;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    IEnumerator Start ()
    {
        // 启动资源管理模块
        var start = DateTime.Now;
        yield return AssetBundleManager.Instance.Initialize();
        Logger.Log(string.Format("AssetBundleManager Initialize use {0}ms", (DateTime.Now - start).Milliseconds));

        yield return null;

        // 初始化UI界面
        start = DateTime.Now;
        yield return InitLaunchPrefab();
        Logger.Log(string.Format("Load launchPrefab use {0}ms", (DateTime.Now - start).Milliseconds));

        // 初始化App版本
        start = DateTime.Now;
        yield return InitAppVersion();
        Logger.Log(string.Format("Init AppVersion use {0}ms", (DateTime.Now - start).Milliseconds));

        // 初始化渠道
        start = DateTime.Now;
        yield return InitChannel();
        Logger.Log(string.Format("Init Channel use {0}ms", (DateTime.Now - start).Milliseconds));

        // 初始化SDK
        start = DateTime.Now;
        yield return InitSDK();
        Logger.Log(string.Format("Init SDK use {0}ms", (DateTime.Now - start).Milliseconds));

        yield return null;

        start = DateTime.Now;
        yield return InitNoticeTipPrefab();
        Logger.Log(string.Format("Load noticeTipPrefab use {0}ms", (DateTime.Now - start).Milliseconds));

        // 开始更新
        start = DateTime.Now;
        yield return updater.StartCheckUpdate();
        Logger.Log(string.Format("CheckUpdate use {0}ms", (DateTime.Now - start).Milliseconds));
        Destroy(updater);
        yield return null;
        
        // 启动xlua框架
        start = DateTime.Now;
        PuertsManager.Instance.Startup();
        yield return StartGame();
        Logger.Log(string.Format("PuertsManager StartUp use {0}ms", (DateTime.Now - start).Milliseconds));
	}

     IEnumerator InitAppVersion()
    {
        var streamingAppVersion = "0.0.0";
        var streamingResVersion = "0.0.0";
        var streamingChannel = "Test";
        
#if UNITY_EDITOR
        if (AssetBundleConfig.IsEditorMode)
        {
            ChannelManager.Instance.appVersion = streamingAppVersion;
            ChannelManager.Instance.resVersion = streamingResVersion;
            channelName = streamingChannel;
            yield break;
        }
#endif
        //streaming
        var appVersionRequest = AssetBundleManager.Instance.RequestAssetFileAsync(BuildUtils.AppVersionFileName);
        yield return appVersionRequest;
        var streamingTxt = appVersionRequest.text;
        appVersionRequest.Dispose();

        if (!string.IsNullOrEmpty(streamingTxt))
        {
            var array = streamingTxt.Split('|');
            streamingAppVersion = array[0];
            streamingResVersion = array[1];
            streamingChannel = array[2];
        }

        //persistent
        var persistentAppVersion = streamingAppVersion;
        var persistentResVersion = streamingResVersion;
        var persistentChannel = streamingChannel;
        
        var appVersionPath = AssetBundleUtility.GetPersistentDataPath(BuildUtils.AppVersionFileName);
        var persistentTxt= GameUtility.SafeReadAllText(appVersionPath);
        if (string.IsNullOrEmpty(persistentTxt))
        {
            GameUtility.SafeWriteAllText(appVersionPath,
                streamingAppVersion + "|" + streamingResVersion + "|" + streamingChannel);
        }
        else
        {
            var array = persistentTxt.Split('|');
            persistentAppVersion = array[0];
            persistentResVersion = array[1];
            persistentChannel = array[2];
        }
        
        //init
        ChannelManager.Instance.appVersion = persistentAppVersion;
        ChannelManager.Instance.resVersion = persistentResVersion;
        channelName = persistentChannel;

        Logger.Log(string.Format("persistentResVersion = {0}, persistentResVersion = {1}, persistentChannel = {2}",
            persistentAppVersion, persistentResVersion, persistentChannel));
        Logger.Log(string.Format("streamingAppVersion = {0}, streamingAppVersion = {1}, streamingChannel = {2}",
            streamingAppVersion, streamingResVersion, streamingChannel));
        
        // 如果persistent目录版本比streamingAssets目录app版本低，说明是大版本覆盖安装，清理过时的缓存
        if (BuildUtils.CheckIsNewVersion(persistentAppVersion, streamingAppVersion))
        {
            Debug.Log("大版本覆盖安装，清理过时的缓存");
            
            ChannelManager.Instance.appVersion = streamingAppVersion;
            ChannelManager.Instance.resVersion = streamingResVersion;
            
            var path = AssetBundleUtility.GetPersistentDataPath();
            GameUtility.SafeDeleteDir(path);
            GameUtility.SafeWriteAllText(appVersionPath,streamingAppVersion + "|" + streamingResVersion + "|" + streamingChannel);
            
            // 重启资源管理器
            yield return AssetBundleManager.Instance.Cleanup();
            yield return AssetBundleManager.Instance.Initialize();
        }
    }

    IEnumerator InitChannel()
    {
        ChannelManager.Instance.Init(channelName);
        Logger.Log(string.Format("channelName = {0}", channelName));
        yield break;
    }

    GameObject InstantiateGameObject(GameObject prefab)
    {
        var start = DateTime.Now;
        var luanchLayer = transform.Find(LaunchLayerPath);
        GameObject go = GameObject.Instantiate(prefab, luanchLayer);
        go.name = prefab.name;
        
        Logger.Log(string.Format("Instantiate use {0}ms", (DateTime.Now - start).Milliseconds));
        return go;
    }
    
    IEnumerator InitNoticeTipPrefab()
    {
        var loader = AssetBundleManager.Instance.LoadAssetAsync(noticeTipPrefabPath, typeof(GameObject));
        yield return loader;
        noticeTipPrefab = loader.asset as GameObject;
        loader.Dispose();
        if (noticeTipPrefab == null)
        {
            Logger.LogError("LoadAssetAsync noticeTipPrefab err : " + noticeTipPrefabPath);
            yield break;
        }
        var go = InstantiateGameObject(noticeTipPrefab);
        UINoticeTip.Instance.UIGameObject = go;
        yield break;
    }

    IEnumerator InitLaunchPrefab()
    {
        var loader = AssetBundleManager.Instance.LoadAssetAsync(launchPrefabPath,typeof(GameObject));
        yield return loader;
        launchPrefab= loader.asset as GameObject;
        loader.Dispose();
        if (launchPrefab == null)
        {
            Logger.LogError("LoadAssetAsync launchPrefab err : " + launchPrefabPath);
            yield break;
        }
        var go = InstantiateGameObject(launchPrefab);
        UILauncher.Instance.UIGameObject = go;
        updater = go.AddComponent<AssetbundleUpdater>();
    }
    
    IEnumerator InitSDK()
    {
        bool SDKInitComplete = false;
        ChannelManager.Instance.InitSDK(() =>
        {
            SDKInitComplete = true;
        });
//        yield return new WaitUntil(()=> {
//            return SDKInitComplete;
//        });
        yield break;
    }
    
    IEnumerator StartGame()
    {
        string luaAssetbundleName = PuertsManager.Instance.AssetbundleName;
        AssetBundleManager.Instance.SetAssetBundleResident(luaAssetbundleName, true);
        var abloader = AssetBundleManager.Instance.LoadAssetBundleAsync(luaAssetbundleName,typeof(TextAsset));
        yield return abloader;
        abloader.Dispose();

        PuertsManager.Instance.OnInit();
        PuertsManager.Instance.StartGame();
    }
}
