using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using GameChannel;
using System;

/// <summary>
/// added by wsh @ 2017.12.29
/// 功能：Assetbundle更新器
/// </summary>

public class AssetbundleUpdater : MonoBehaviour
{
    static int MAX_DOWNLOAD_NUM = 5;
    static int UPDATE_SIZE_LIMIT = 5 * 1024 * 1024;
    static string APK_FILE_PATH = "/xluaframework_{0}_{1}.apk";

    string resVersionPath = null;
    string noticeVersionPath = null;
    string clientAppVersion = null;
    string serverAppVersion = null;
    string clientResVersion = null;
    string serverResVersion = null;

    bool needDownloadGame = false;
    bool needUpdateGame = false;

    double timeStamp = 0;
    bool isDownloading = false;
    bool hasError = false;

    Versions versions = new Versions();

    Manifest localManifest = null;
    Manifest hostManifest = null;
    List<string> needDownloadList = new List<string>();
    List<UnityWebAssetRequester> downloadingRequest = new List<UnityWebAssetRequester>();

    int downloadSize = 0;
    int totalDownloadCount = 0;
    int finishedDownloadCount = 0;

    // Hotfix测试---用于测试热更模块的热修复
    public void TestHotfix()
    {
        Logger.Log("********** AssetbundleUpdater : Call TestHotfix in cs...");
    }
    
    void Start()
    {
        DateTime startDate = new DateTime(1970, 1, 1);
        timeStamp = (DateTime.Now - startDate).TotalMilliseconds;
        UILauncher.Instance.SetSatus("正在检测资源更新...");
    }

    #region 主流程

    public IEnumerator StartCheckUpdate()
    {
        yield return StartCoroutine(CheckUpdateOrDownloadGame());
#if UNITY_EDITOR || CLIENT_DEBUG
        TestHotfix();
#endif
    }

    IEnumerator CheckUpdateOrDownloadGame()
    {
        // 初始化本地版本信息
        InitLocalVersion();

#if UNITY_EDITOR
        if (AssetBundleConfig.IsEditorMode)
        {
            yield break;
        }
#endif

        // 获取服务器地址，并检测大版本更新、资源更新
        bool isInternalVersion = ChannelManager.Instance.IsInternalVersion();

        yield return GetUrlListAndCheckUpdate(isInternalVersion);

        // 执行大版本更新、资源更新
        if (needDownloadGame)
        {
            UINoticeTip.Instance.ShowOneButtonTip("游戏下载", "需要下载新的游戏版本！", "确定", null);
            yield return UINoticeTip.Instance.WaitForResponse();
            yield return DownloadGame();
        }
        else if (needUpdateGame)
        {
            yield return CheckGameUpdate(isInternalVersion);
        }
        else
        {
            yield return UpdateFinish(false);
        }

        yield break;
    }

    #endregion

    #region 初始化工作

    void InitLocalVersion()
    {
        clientAppVersion = ChannelManager.Instance.appVersion;
        clientResVersion = ChannelManager.Instance.resVersion;

        serverAppVersion = clientAppVersion;
        serverResVersion = clientResVersion;
    }

    #endregion

    #region 服务器地址获取以及检测版本更新

    IEnumerator GetUrlListAndCheckUpdate(bool isInternalVersion)
    {
        if (isInternalVersion)
        {
            // 内部版本使用本地服务器更新
            yield return InternalGetUrlList();
        }
        else
        {
            // 外部版本一律使用外网服务器更新
            yield return OutnetGetUrlList();
        }

        // 检测大版本更新
#if UNITY_EDITOR
        // 编辑器下总是不进行大版本更新
        needDownloadGame = false;
#else
        if (isInternalVersion)
        {
#if UNITY_ANDROID
            if (ChannelManager.Instance.IsGooglePlay())
            {
                // TODO：这里还要探索下怎么下载
                needDownloadGame = false;
                Debug.LogError("No support for local server download game for GooglePlay now !!!");
            }
            else
            {
                // 对比版本号更新
                needDownloadGame = BuildUtils.CheckIsNewVersion(clientAppVersion, serverAppVersion);
            }
#elif UNITY_IPHONE
            // TODO：iOS下的本地下载要进一步探索，这里先不管
            needDownloadGame = false;
            Debug.LogError("No support for local server download game for iOS now !!!");
#endif
        }
        else
        {
            // 外部版本对比版本号更新
            needDownloadGame = BuildUtils.CheckIsNewVersion(clientAppVersion, serverAppVersion);
        }
#endif

        // 检测资源更新
        if (isInternalVersion)
        {
            // 内部版本总是检测资源更新，避免开发过程中需要频繁升级资源版本号
            needUpdateGame = true;
        }
        else
        {
            // 外部版本对比版本号更新
            needUpdateGame = BuildUtils.CheckIsNewVersion(clientResVersion, serverResVersion);
        }

#if UNITY_CLIENT || LOGGER_ON
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendFormat("SERVER_LIST_URL = {0}\n", URLSetting.SERVER_LIST_URL);
        sb.AppendFormat("LOGIN_URL = {0}\n", URLSetting.LOGIN_URL);
        sb.AppendFormat("REPORT_ERROR_URL = {0}\n", URLSetting.REPORT_ERROR_URL);
        sb.AppendFormat("NOTIFY_URL = {0}\n", URLSetting.NOTICE_URL);
        sb.AppendFormat("APP_DOWNLOAD_URL = {0}\n", URLSetting.APP_DOWNLOAD_URL);
        sb.AppendFormat("RES_DOWNLOAD_URL = {0}\n", URLSetting.RES_DOWNLOAD_URL);
        sb.AppendFormat("noticeVersion = {0}\n", ChannelManager.Instance.noticeVersion);
        sb.AppendFormat("serverAppVersion = {0}\n", serverAppVersion);
        sb.AppendFormat("serverResVersion = {0}\n", serverResVersion);
        Logger.Log(sb.ToString());
#endif

        yield break;
    }

    IEnumerator DownloadInternalLocalAppVersion()
    {
        var request = AssetBundleManager.Instance.DownloadAssetFileAsync(BuildUtils.AppVersionFileName);
        yield return request;
        if (request.error != null)
        {
            UINoticeTip.Instance.ShowOneButtonTip("网络错误", "检测更新失败，请确认网络已经连接！", "重试", null);
            yield return UINoticeTip.Instance.WaitForResponse();
            Logger.LogError("Download :  " + request.assetbundleName + "\n from url : " + request.url + "\n err : " +
                            request.error);
            request.Dispose();

            // 内部版本本地服务器有问题直接跳过，不要卡住游戏
            yield break;
        }

        serverAppVersion = request.text.Trim().Replace("\r", "");
        request.Dispose();

        yield break;
    }

    IEnumerator DownloadInternalServerResVersion()
    {
        var request = AssetBundleManager.Instance.DownloadAssetFileAsync(BuildUtils.AppVersionFileName);
        yield return request;
        if (request.error != null)
        {
            UINoticeTip.Instance.ShowOneButtonTip("网络错误", "检测更新失败，请确认网络已经连接！", "重试", null);
            yield return UINoticeTip.Instance.WaitForResponse();
            Logger.LogError("Download :  " + request.assetbundleName + "\n from url : " + request.url + "\n err : " + request.error);
            request.Dispose();

            // 内部版本本地服务器有问题直接跳过，不要卡住游戏
            yield break;
        }

        var versionTxt = request.text.Trim().Replace("\r", "");
        var array= versionTxt.Split('|');
        serverAppVersion = array[0];
        serverResVersion = array[1];
        request.Dispose();

        yield break;
    }

    IEnumerator InternalGetUrlList()
    {
        // 内网服务器地址设置
        var localSerUrlRequest =
            AssetBundleManager.Instance.RequestAssetFileAsync(AssetBundleConfig.AssetBundleServerUrlFileName);
        yield return localSerUrlRequest;
#if UNITY_ANDROID
        // TODO：GooglePlay下载还有待探索
        if (!ChannelManager.Instance.IsGooglePlay())
        {
            string apkName = ChannelManager.Instance.GetProductName() + ".apk";
            URLSetting.APP_DOWNLOAD_URL = localSerUrlRequest.text + apkName;
        }
#elif UNITY_IPHONE
        // TODO：ios下载还有待探索
#endif
        URLSetting.RES_DOWNLOAD_URL = localSerUrlRequest.text + BuildUtils.ManifestBundleName + "/";
        localSerUrlRequest.Dispose();

        // 从本地服务器拉一下App版本号
        yield return DownloadInternalLocalAppVersion();

        // 从本地服务器拉一下资源版本号
        yield return DownloadInternalServerResVersion();

        yield break;
    }

    IEnumerator OutnetGetUrlList()
    {
        var args = string.Format("package={0}&app_version={1}&res_version={2}&notice_version={3}",
            ChannelManager.Instance.channelName, ChannelManager.Instance.appVersion, clientResVersion,
            ChannelManager.Instance.noticeVersion);

        bool GetUrlListComplete = false;
        WWW www = null;
        SimpleHttp.HttpPost(URLSetting.START_UP_URL, null, DataUtils.StringToBytes(args), (WWW wwwInfo) =>
        {
            www = wwwInfo;
            GetUrlListComplete = true;
        });
        yield return new WaitUntil(() => { return GetUrlListComplete; });

        if (www == null || !string.IsNullOrEmpty(www.error) || www.bytes == null || www.bytes.Length == 0)
        {
            Logger.LogError("Get url list for args {0} with err : {1}", args,
                www == null ? "www null" : (!string.IsNullOrEmpty(www.error) ? www.error : "bytes length 0"));
            yield return OutnetGetUrlList();
        }

        var urlList = (Dictionary<string, object>) MiniJSON.Json.Deserialize(DataUtils.BytesToString(www.bytes));
        if (urlList == null)
        {
            Logger.LogError("Get url list for args {0} with err : {1}", args, "Deserialize url list null!");
            yield return OutnetGetUrlList();
        }

        if (urlList.ContainsKey("serverlist"))
        {
            URLSetting.SERVER_LIST_URL = urlList["serverlist"].ToString();
        }

        if (urlList.ContainsKey("verifying"))
        {
            URLSetting.LOGIN_URL = urlList["verifying"].ToString();
        }

        if (urlList.ContainsKey("logserver"))
        {
            URLSetting.REPORT_ERROR_URL = urlList["logserver"].ToString();
        }

        if (urlList.ContainsKey("app_version") && !string.IsNullOrEmpty(urlList["app_version"].ToString()))
        {
            serverAppVersion = urlList["app_version"].ToString();
        }

        if (urlList.ContainsKey("res_version") && !string.IsNullOrEmpty(urlList["res_version"].ToString()))
        {
            serverResVersion = urlList["res_version"].ToString();
        }

        if (urlList.ContainsKey("notice_version") && !string.IsNullOrEmpty(urlList["notice_version"].ToString()))
        {
            ChannelManager.Instance.noticeVersion = urlList["notice_version"].ToString();
            GameUtility.SafeWriteAllText(noticeVersionPath, ChannelManager.Instance.noticeVersion);
        }

        if (urlList.ContainsKey("notice_url") && !string.IsNullOrEmpty(urlList["notice_url"].ToString()))
        {
            URLSetting.NOTICE_URL = urlList["notice_url"].ToString();
        }

        if (urlList.ContainsKey("app") && !string.IsNullOrEmpty(urlList["app"].ToString()))
        {
            URLSetting.APP_DOWNLOAD_URL = urlList["app"].ToString();
        }
        else if (urlList.ContainsKey("res") && !string.IsNullOrEmpty(urlList["res"].ToString()))
        {
            URLSetting.RES_DOWNLOAD_URL = urlList["res"].ToString();
        }

        yield break;
    }

    #endregion

    #region 游戏下载

    IEnumerator DownloadGame()
    {
#if UNITY_ANDROID
        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            UINoticeTip.Instance.ShowOneButtonTip("游戏下载", "当前为非Wifi网络环境，下载需要消耗手机流量，继续下载？", "确定", null);
            yield return UINoticeTip.Instance.WaitForResponse();
        }

        DownloadGameForAndroid();
#elif UNITY_IPHONE
        ChannelManager.instance.StartDownloadGame(URLSetting.APP_DOWNLOAD_URL);
#endif
        yield break;
    }

#if UNITY_ANDROID
    void DownloadGameForAndroid()
    {
        UILauncher.Instance.SetSatus("正在检测资源更新...");
        UILauncher.Instance.SetValue(0);
        
        string saveName = string.Format(APK_FILE_PATH, ChannelManager.Instance.channelName, serverAppVersion);
        Logger.Log(string.Format("Download game : {0}", saveName));
        ChannelManager.Instance.StartDownloadGame(URLSetting.APP_DOWNLOAD_URL, DownloadGameSuccess, DownloadGameFail,
            (int progress) =>
            {
                UILauncher.Instance.SetValue(progress);
            }, saveName);
    }

    void DownloadGameSuccess()
    {
        UINoticeTip.Instance.ShowOneButtonTip("下载完毕", "游戏下载完毕，确认安装？", "安装",
            () => { ChannelManager.Instance.InstallGame(DownloadGameSuccess, DownloadGameFail); });
    }

    void DownloadGameFail()
    {
        UINoticeTip.Instance.ShowOneButtonTip("下载失败", "游戏下载失败！", "重试", () => { DownloadGameForAndroid(); });
    }
#endif

    private bool ShowUpdatePrompt(int downloadSize)
    {
        if (UPDATE_SIZE_LIMIT <= 0 &&
            Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            // wifi不提示更新了
            return false;
        }

        if (downloadSize < UPDATE_SIZE_LIMIT)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region 资源更新

    IEnumerator CheckGameUpdate(bool isInternal)
    {
        // 检测资源更新
        Logger.Log("Resource download url : " + URLSetting.RES_DOWNLOAD_URL);
        var start = DateTime.Now;
        yield return CheckIfNeededUpdate(isInternal);
        Logger.Log(string.Format("CheckIfNeededUpdate use {0}ms", (DateTime.Now - start).Milliseconds));
        
        if (needDownloadList.Count <= 0)
        {
            Logger.Log("No resources to update...");
            yield return UpdateFinish(false);
            yield break;
        }

        Logger.Log("GetDownloadAssetBundlesSize : {0}", KBSizeToString(downloadSize));
        if (ShowUpdatePrompt(downloadSize) || isInternal)
        {
            UINoticeTip.Instance.ShowOneButtonTip("更新提示", string.Format("本次更新需要消耗{0}流量！", KBSizeToString(downloadSize)), "确定", null);
            yield return UINoticeTip.Instance.WaitForResponse();
        }
        
        UILauncher.Instance.SetSatus("download assets...");
        UILauncher.Instance.SetValue(0);
        
        totalDownloadCount = needDownloadList.Count;
        finishedDownloadCount = 0;
        Logger.Log(totalDownloadCount + " resources to update...");

        start = DateTime.Now;
        yield return StartUpdate();
        Logger.Log(string.Format("Update use {0}ms", (DateTime.Now - start).Milliseconds));

        UILauncher.Instance.SetValue(1);
        start = DateTime.Now;
        yield return UpdateFinish(true);
        Logger.Log(string.Format("UpdateFinish use {0}ms", (DateTime.Now - start).Milliseconds));

        yield break;
    }

    IEnumerator CheckIfNeededUpdate(bool isInternal)
    {
        yield return versions.LoadLocalVersions();
        
        yield return versions.LoadServerVersion();
        
        downloadSize = versions.CompareVersions(ref needDownloadList);
        yield break;
    }

    IEnumerator StartUpdate()
    {
        downloadingRequest.Clear();
        isDownloading = true;
        hasError = false;
        yield return new WaitUntil(() => { return isDownloading == false; });
        if (needDownloadList.Count > 0)
        {
            UINoticeTip.Instance.ShowOneButtonTip("网络错误", "游戏更新失败，请确认网络已经连接！", "重试", null);
            yield return UINoticeTip.Instance.WaitForResponse();
            yield return StartUpdate();
        }

        yield break;
    }

    IEnumerator UpdateFinish(bool hasUpdate)
    {
        var start = DateTime.Now;
        UILauncher.Instance.SetSatus("正在准备资源...");
        if (hasUpdate)
        {
            // 存储版本号
            var appVersionPath = AssetBundleUtility.GetPersistentDataPath(BuildUtils.AppVersionFileName);
            GameUtility.SafeWriteAllText(appVersionPath,
                clientAppVersion + "|" + serverResVersion + "|" + ChannelManager.Instance.channelName);
            clientResVersion = serverResVersion;

            // 拷贝临时文件
            versions.SaveAllToTemp();
            CopyTemp2Data();

            // 设置版本号
            Logger.appVersion = clientAppVersion;
            Logger.resVersion = clientResVersion;
            ChannelManager.Instance.resVersion = serverResVersion;

            // 重启资源管理器
            yield return AssetBundleManager.Instance.Cleanup();
            yield return AssetBundleManager.Instance.Initialize();
        }

        Logger.Log(string.Format("UpdateFinish use {0}ms", (DateTime.Now - start).Milliseconds));
    }

    void CopyTemp2Data()
    {
        var tempPath = AssetBundleUtility.GetPersistentTempPath();
        var files = GameUtility.GetAllFilesInFolder(tempPath);
        if (files.Length > 0)
        {
            foreach (string fromfile in files)
            {
                string tofile = fromfile.Replace(AssetBundleConfig.TempFolderName,
                    AssetBundleConfig.AssetBundlesFolderName);
                GameUtility.SafeCopyFile(fromfile, tofile);
                GameUtility.SafeDeleteFile(fromfile);
            }

            GameUtility.SafeDeleteDir(tempPath);
        }
    }

    void Update()
    {
        if (!isDownloading)
        {
            return;
        }

        for (int i = downloadingRequest.Count - 1; i >= 0; i--)
        {
            var request = downloadingRequest[i];
            if (request.isDone)
            {
                if (!string.IsNullOrEmpty(request.error))
                {
                    Logger.LogError("Error when downloading file : " + request.assetbundleName + "\n from url : " +
                                    request.url + "\n err : " + request.error);
                    hasError = true;
                    needDownloadList.Add(request.assetbundleName);
                }
                else
                {
                    // TODO：是否需要显示下载流量进度？
                    Logger.Log("Finish downloading file : " + request.assetbundleName + "\n from url : " + request.url);
                    downloadingRequest.RemoveAt(i);
                    finishedDownloadCount++;
                    var filePath = AssetBundleUtility.GetPersistentDataPath(request.assetbundleName);
                    GameUtility.SafeWriteAllBytes(filePath, request.bytes);
                }

                request.Dispose();
            }
        }

        if (!hasError)
        {
            while (downloadingRequest.Count < MAX_DOWNLOAD_NUM && needDownloadList.Count > 0)
            {
                var fileName = needDownloadList[needDownloadList.Count - 1];
                needDownloadList.RemoveAt(needDownloadList.Count - 1);
                var request = AssetBundleManager.Instance.DownloadAssetBundleAsync(fileName);
                downloadingRequest.Add(request);
            }
        }

        if (downloadingRequest.Count == 0)
        {
            isDownloading = false;
        }

        float progressSlice = 1.0f / totalDownloadCount;
        float progressValue = finishedDownloadCount * progressSlice;
        for (int i = 0; i < downloadingRequest.Count; i++)
        {
            progressValue += (progressSlice * downloadingRequest[i].progress);
        }

        UILauncher.Instance.SetValue(progressValue);
    }

    private string KBSizeToString(int kbSize)
    {
        string sizeStr = string.Empty;
        if (kbSize >= 1024)
        {
            sizeStr = (kbSize / 1024.0f).ToString("0.0") + "M";
        }
        else
        {
            sizeStr = kbSize + "K";
        }

        return sizeStr;
    }

    #endregion
}
