using UnityEditor;
using UnityEngine;
using System.IO;
using GameChannel;
using AssetBundles;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// added by wsh @ 2018.01.03
/// 说明：打包工具
/// TODO：
/// 1、安卓打包可以不用区分渠道，没有IOS那样的机器审核难以通过的问题
/// </summary>

public class PackageTool : EditorWindow
{
    static private BuildTarget buildTarget = BuildTarget.Android;
    static private ChannelType channelType = ChannelType.Test;
    static private string appVersion = "1.0.0";
    static private string resVersion = "1.0.0";
    static private LocalServerType localServerType = LocalServerType.CurrentMachine;
    static private string localServerIP = "127.0.0.1";
    static private bool androidBuildABForPerChannel;
    static private bool iosBuildABForPerChannel;
    static private bool buildABSForPerChannel;

    private static EditorWindow instance;
    private static EditorWindow Instance
    {
        get
        {
            if (instance == null) 
                instance = GetWindow(typeof(PackageTool));
            return instance;
        }
    }
    
    [MenuItem("Tools/Package", false, 0)]
    static void Init() {
        EditorWindow.GetWindow(typeof(PackageTool));
    }

    void OnEnable()
    {
        buildTarget = EditorUserBuildSettings.activeBuildTarget;
        channelType = PackageUtils.GetCurSelectedChannel();

        ReadLocalVersionFile(buildTarget, channelType);

        localServerType = PackageUtils.GetLocalServerType();
        localServerIP = PackageUtils.GetLocalServerIP();

        androidBuildABForPerChannel = PackageUtils.GetAndroidBuildABForPerChannelSetting();
        iosBuildABForPerChannel = PackageUtils.GetIOSBuildABForPerChannelSetting();
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target : ", buildTarget);
        GUILayout.Space(5);
        bool buildTargetSupport = false;
        if (buildTarget != BuildTarget.Android && buildTarget != BuildTarget.iOS)
        {
            GUILayout.Label("Error : Only android or iOS build target supported!!!");
        }
        else
        {
            buildTargetSupport = true;
            channelType = (ChannelType)EditorGUILayout.EnumPopup("Build Channel : ", channelType);
        }
        GUILayout.EndVertical();

        if (buildTargetSupport)
        {
            if (GUI.changed)
            {
                PackageUtils.SaveCurSelectedChannel(channelType);
            }

            DrawAssetBundlesConfigGUI();
            DrawConfigGUI();
            DrawLocalServerGUI();
            DrawAssetBundlesGUI();
            DrawXLuaGUI();
            DrawBuildPlayerGUI();
        }
    }

    #region AB相关配置
    void DrawAssetBundlesConfigGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[AssetBundles Config]-------------");
        GUILayout.Space(3);

        // 是否为每个channel打一个AB包
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build For Per Channel : ", GUILayout.Width(150));
        if (buildTarget == BuildTarget.Android)
        {
            buildABSForPerChannel = EditorGUILayout.Toggle(androidBuildABForPerChannel, GUILayout.Width(50));
            if (buildABSForPerChannel != androidBuildABForPerChannel)
            {
                androidBuildABForPerChannel = buildABSForPerChannel;
                PackageUtils.SaveAndroidBuildABForPerChannelSetting(buildABSForPerChannel);
            }
        }
        else
        {
            buildABSForPerChannel = EditorGUILayout.Toggle(iosBuildABForPerChannel, GUILayout.Width(50));
            if (buildABSForPerChannel != iosBuildABForPerChannel)
            {
                iosBuildABForPerChannel = buildABSForPerChannel;
                PackageUtils.SaveIOSBuildABForPerChannelSetting(buildABSForPerChannel);
            }
        }
        if (GUILayout.Button("Run All Checkers", GUILayout.Width(200)))
        {
            bool checkChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);
            PackageUtils.CheckAndRunAllCheckers(checkChannel, true);
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region 资源配置GUI
    void DrawConfigGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Config]-------------");

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("app_version", GUILayout.Width(80));
        string curBundleVersion = GUILayout.TextField(appVersion, GUILayout.Width(100));
        if (curBundleVersion != appVersion)
        {
            appVersion = curBundleVersion;
            PlayerSettings.bundleVersion = curBundleVersion;
            SaveLocalVersionFile(buildTarget, channelType);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("res_version", GUILayout.Width(80));
        string curResVersion = GUILayout.TextField(resVersion, GUILayout.Width(100));
        if (curResVersion != resVersion)
        {
            resVersion = curResVersion;
            SaveLocalVersionFile(buildTarget, channelType);
        }
        GUILayout.EndHorizontal();


        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
            if (GUILayout.Button("Load Version From Channel", GUILayout.Width(200)))
            {
                ReadLocalVersionFile(buildTarget, channelType);
            }
            if (GUILayout.Button("Validate All Channels ResVersion", GUILayout.Width(200)))
            {
                ValidateAllLocalVersions();
            }
            if (GUILayout.Button("Save Version To All Channels", GUILayout.Width(200)))
            {
                SaveAllVersionFile();
            }
        }
        else
        {
            if (GUILayout.Button("Load Version From Channel", GUILayout.Width(200)))
            {
                ReadLocalVersionFile(buildTarget, channelType);
            }
            if (GUILayout.Button("Save Version To Channel", GUILayout.Width(200)))
            {
                SaveLocalVersionFile(buildTarget, channelType);
            }
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region 本地服务器配置GUI
    void DrawLocalServerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[AssetBundles Local Server]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        var curSelected = (LocalServerType)EditorGUILayout.EnumPopup("Local Server Type : ", localServerType, GUILayout.Width(300));
        bool typeChanged = curSelected != localServerType;
        if (typeChanged)
        {
            PackageUtils.SaveLocalServerType(curSelected);

            localServerType = curSelected;
            localServerIP = PackageUtils.GetLocalServerIP();
        }
        if (localServerType == LocalServerType.CurrentMachine)
        {
            GUILayout.Label(localServerIP);
        }
        else
        {
            localServerIP = GUILayout.TextField(localServerIP, GUILayout.Width(100));
            if (GUILayout.Button("Save", GUILayout.Width(200)))
            {
                PackageUtils.SaveLocalServerIP(localServerIP);
            }
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region AB相关操作GUI
    void DrawAssetBundlesGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build AssetBundles]-------------");
        GUILayout.Space(3);
        
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        if (buildABSForPerChannel)
        {
            if (GUILayout.Button("Current Channel Only", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAssetBundlesForCurrentChannel;
            }
            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAssetBundlesForAllChannels;
            }
            if (GUILayout.Button("Open Current Output", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsOpenOutput();
            }
            if (GUILayout.Button("Copy To StreamingAsset", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsCopyAssetbundles();
            }
        }
        else
        {
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAssetBundlesForCurrentChannel;
            }
            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsOpenOutput();
            }
            if (GUILayout.Button("Copy To StreamingAsset", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsCopyAssetbundles();
            }
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region xlua相关GUI
    void DrawXLuaGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Gen XLua Code]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Code", GUILayout.Width(200)))
        {
            GenXLuaCode(buildTarget);
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region 打包相关GUI
    void DrawBuildAndroidPlayerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build Android Player]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
//            if (GUILayout.Button("Copy SDK Resources", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += CopyAndroidSDKResources;
//            }
            if (GUILayout.Button("Current Channel Only", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAndroidPlayerForCurrentChannel;
            }
            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAndroidPlayerForAllChannels;
            }
            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = PackageUtils.GetChannelOutputPath(buildTarget, channelType.ToString());
                EditorUtils.ExplorerFolder(folder);
            }
        }
        else
        {
//            if (GUILayout.Button("Copy SDK Resource", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += CopyAndroidSDKResources;
//            }
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAndroidPlayerForCurrentChannel;
            }
            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = PackageUtils.GetChannelOutputPath(buildTarget, channelType.ToString());
                EditorUtils.ExplorerFolder(folder);
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawBuildIOSPlayerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build IOS Player]-------------");
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
            if (GUILayout.Button("Current Channel Only", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildIOSPlayerForCurrentChannel;
            }
            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildIOSPlayerForAllChannels;
            }
            if (GUILayout.Button("Open Current Output", GUILayout.Width(200)))
            {
                var folder = Path.Combine(System.Environment.CurrentDirectory, BuildPlayer.XCodeOutputPath);
                EditorUtils.ExplorerFolder(folder);
            }
        }
        else
        {
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildIOSPlayerForCurrentChannel;
            }
            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = Path.Combine(System.Environment.CurrentDirectory, BuildPlayer.XCodeOutputPath);
                EditorUtils.ExplorerFolder(folder);
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawBuildPlayerGUI()
    {
        if (buildTarget == BuildTarget.Android)
        {
            DrawBuildAndroidPlayerGUI();
        }
        else if (buildTarget == BuildTarget.iOS)
        {
            DrawBuildIOSPlayerGUI();
        }
    }
    #endregion

    #region 资源配置操作
    // 此处存储的VersionConfig和VersionFile重复，暂时不用
    //public static void ReadVersionConfig()
    //{
    //    // 从数据库加载资源版本号
    //    AssetBundleResVersionConfig config = AssetDatabase.LoadAssetAtPath(AssetBundleResVersionConfig.RES_PATH, typeof(AssetBundleResVersionConfig)) as AssetBundleResVersionConfig;
    //    if (config == null)
    //    {
    //        config = CreateInstance<AssetBundleResVersionConfig>();
    //        AssetDatabase.CreateAsset(config, AssetBundleResVersionConfig.RES_PATH);
    //        AssetDatabase.Refresh();
    //    }
    //    appVersion = config.appVersion;
    //    resVersion = config.resVersion;
    //}
    //public static void SaveVersionConfig()
    //{
    //    // 保存资源版本号到数据库
    //    AssetBundleResVersionConfig config = AssetDatabase.LoadAssetAtPath(AssetBundleResVersionConfig.RES_PATH, typeof(AssetBundleResVersionConfig)) as AssetBundleResVersionConfig;
    //    if (config == null)
    //    {
    //        config = CreateInstance<AssetBundleResVersionConfig>();
    //        AssetDatabase.CreateAsset(config, AssetBundleResVersionConfig.RES_PATH);
    //        AssetDatabase.Refresh();
    //    }
    //    config.appVersion = appVersion;
    //    config.resVersion = resVersion;
    //    EditorUtility.SetDirty(config);
    //    AssetDatabase.SaveAssets();
    //    AssetDatabase.Refresh();
    //}

    public static bool ReadLocalVersionFile(BuildTarget target, ChannelType channel)
    {
        // 从资源版本号文件（当前渠道AB输出目录中）加载资源版本号
        string rootPath = PackageUtils.GetAssetBundleOutputPath(target, channel.ToString());
        
        string app_path = rootPath + "/" + BuildUtils.AppVersionFileName;
        app_path = GameUtility.FormatToUnityPath(app_path);
        appVersion = "0.0.0";
        resVersion = "0.0.0";
        channelType = ChannelType.Test;

        string content = GameUtility.SafeReadAllText(app_path);
        if (content == null)
        {
            return false;
        }

        Debug.Log("Load local version :" + content);
        var arr = content.Split('|');
        if (arr.Length >= 3)
        {
            appVersion = arr[0];
            resVersion = arr[1];
            channelType = (ChannelType)Enum.Parse(typeof(ChannelType), arr[2]);
        }
        
        return true;
    }

    public static void SaveLocalVersionFile(BuildTarget target, ChannelType channel)
    {
        string rootPath = PackageUtils.GetAssetBundleOutputPath(target, channel.ToString());
        
        string path = rootPath + "/" + BuildUtils.AppVersionFileName;
        path = GameUtility.FormatToUnityPath(path);

        string content = appVersion + "|" + resVersion + "|" + channel;
        Debug.Log("save app_version.bytes->" + content);
        // 保存所有版本号信息到资源版本号文件（当前渠道AB输出目录中）
        GameUtility.SafeWriteAllText(path, content);
    }

    #region Server Control Versions
    //资源版本一般都是存储在服务器端，此处逻辑不同公司方式不同，暂不给出
    public static void LoadServerResVersion(bool silence = false)
    {
        
    }

    public static void SaveServerVersionFile(bool silence = false)
    {

    }

    public static void IncreaseServerVersionFile(bool silence = false)
    {

    }

    #endregion
    
    public static void ValidateAllLocalVersions()
    {
        // 校验所有渠道AB输出目录下资源版本号信息
        Dictionary<ChannelType,string[]> versionMap = new Dictionary<ChannelType,string[]>();
        
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            string[] versions = PackageUtils.ReadAppAndResVersionFile(buildTarget,current);
            if (versions == null)
            {
                continue;
            }
            
            versionMap.Add(current, versions);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("{0} Channels : \n\n", versionMap.Count);

        foreach (var current in versionMap)
        {
            var channel = current.Key.ToString();
            var appV = current.Value[0];
            var resV = current.Value[1];

            sb.AppendFormat("Channel : {0}\n\n", channel);
            sb.AppendFormat("AppVersion : {0}   ResVersion : {1}\n", appV, resV);
            sb.AppendLine("-----------------------------------------------\n");
        }
        
        EditorUtility.DisplayDialog("Result", sb.ToString(), "Confirm");
    }


    void SaveAllVersionFile()
    {
        // 保存当前版本号信息到所有渠道AB输出目录
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            SaveLocalVersionFile(buildTarget, current);
        }
        EditorUtility.DisplayDialog("Success", "Save all version files to all channels done!", "Confirm");
    }
    #endregion

    #region AB相关操作
    public static void IncreaseResSubVersion()
    {
        // 每一次构建资源，子版本号自增，注意：前两个字段这里不做托管，自行编辑设置
        resVersion = PackageUtils.IncreaseResSubVersion(resVersion);
        Instance.Repaint();
        Debug.Log("IncreaseResSubVersion:" + resVersion);
        SaveLocalVersionFile(buildTarget, channelType);
        SaveServerVersionFile(true);
    }

    public static void BuildAssetBundlesForCurrentChannel()
    {
        IncreaseResSubVersion();

        var start = DateTime.Now;
        BuildPlayer.BuildAssetBundles(buildTarget, channelType.ToString());

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build AssetBundles for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s", 
            buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void BuildAssetBundlesForAllChannels()
    {
        IncreaseResSubVersion();

        var start = DateTime.Now;
        BuildPlayer.BuildAssetBundlesForAllChannels(buildTarget);
        
        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build AssetBundles for : \n\nplatform : {0} \nchannel : all \n\ndone! use {1}s", buildTargetName, (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void GenXLuaCode(BuildTarget buildTarget)
    {
//        PackageUtils.CheckAndAddSymbolIfNeeded(buildTarget, "HOTFIX_ENABLE");
//        CSObjectWrapEditor.Generator.ClearAll();
//        CSObjectWrapEditor.Generator.GenAll();
    }

    public static bool CheckSymbolsToCancelBuild()
    {
        var buildTargetGroup = buildTarget == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        var replace = symbols.Replace("HOTFIX_ENABLE", "");
        replace = symbols.Replace(";", "").Trim();
        if (!string.IsNullOrEmpty(replace))
        {
            int checkClear = EditorUtility.DisplayDialogComplex("Build Symbol Warning",
                string.Format("Now symbols : \n\n{0}\n\nClear all symbols except \"HOTFIX_ENABLE\" ?", symbols),
                "Yes", "No", "Cancel");
            if (checkClear == 0)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, "HOTFIX_ENABLE");
            }
            return checkClear == 2;
        }
        return false;
    }
    #endregion

    #region 打包相关操作
    public static void IncreaseAppSubVersion()
    {
        // 每一次构建安装包，子版本号自增，注意：前两个字段这里不做托管，自行到PlayerSetting中设置
        appVersion = PackageUtils.IncreaseResSubVersion(appVersion);
        resVersion = appVersion.Substring(2) + ".1";
        
        Instance.Repaint();
        Debug.Log("IncreaseAppSubVersion:" + appVersion);
        SaveLocalVersionFile(buildTarget, channelType);
        SaveServerVersionFile(true);
    }

    public static void CopyAndroidSDKResources()
    {
        PackageUtils.CopyAndroidSDKResources(channelType.ToString());
    }

    public static void BuildAndroidPlayerForCurrentChannel()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        IncreaseAppSubVersion();

        var start = DateTime.Now;
        BuildPlayer.BuildAndroid(channelType.ToString(), channelType == ChannelType.Test);

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build player for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s", buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void BuildAndroidPlayerForAllChannels()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        IncreaseAppSubVersion();

        var start = DateTime.Now;
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            BuildPlayer.BuildAndroid(current.ToString(), current == ChannelType.Test);
        }

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build player for : \n\nplatform : {0} \nchannel : all \n\ndone! use {2}s", buildTargetName, (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void BuildIOSPlayerForCurrentChannel()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        IncreaseAppSubVersion();

        var start = DateTime.Now;
        BuildPlayer.BuildXCode(channelType.ToString(), channelType == ChannelType.Test);

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build player for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s", buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void BuildIOSPlayerForAllChannels()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        IncreaseAppSubVersion();

        var start = DateTime.Now;
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            BuildPlayer.BuildXCode(current.ToString(), channelType == ChannelType.Test);
        }

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format("Build player for : \n\nplatform : {0} \nchannel : all \n\ndone! use {2}s", buildTargetName, (DateTime.Now - start).TotalSeconds), "Confirm");
    }
    #endregion
}
