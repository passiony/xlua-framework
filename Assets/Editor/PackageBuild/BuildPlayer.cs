using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using GameChannel;
using AssetBundles;
using System;
using System.Linq;
using System.Text;

/// <summary>
/// added by wsh @ 2018.01.03
/// 功能： 打包相关配置和通用函数
/// 
/// 注意：
/// 1）如果为每个渠道分别打AB包，则将渠道名打入各个AB包<---为了解决iOS各个渠道包的提审问题
/// 
/// </summary>

public class BuildPlayer : Editor
{
    public const string StudioOutputPath = "vStudio";
    public const string XCodeOutputPath = "vXCode";

    //    public static void WriteChannelNameFile(BuildTarget buildTarget, string channelName)
    //    {
    //        var outputPath = PackageUtils.GetAssetBundleOutputPath(buildTarget, channelName);
    //        GameUtility.SafeWriteAllText(Path.Combine(outputPath, BuildUtils.ChannelNameFileName), channelName);
    //    }

    //把所有bundle的详情：filename|md5|size 写入文件versions.txt
    public static void WriteBundlesVersionFile(AssetBundleManifest manifest)
    {
        var outputPath = PackageUtils.GetCurBuildSettingAssetBundleOutputPath();
        var allAssetbundles = manifest.GetAllAssetBundles();

        StringBuilder sb = new StringBuilder();
        if (allAssetbundles != null && allAssetbundles.Length > 0)
        {
            foreach (var assetbundle in allAssetbundles)
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(outputPath, assetbundle));

                Hash128 hash = manifest.GetAssetBundleHash(assetbundle);
                int size = (int)(fileInfo.Length / 1024) + 1;
                sb.AppendFormat("{0}|{1}|{2}\n", GameUtility.FormatToUnityPath(assetbundle), hash, size);
            }
        }
        string content = sb.ToString().Trim();
        GameUtility.SafeWriteAllText(Path.Combine(outputPath, BuildUtils.VersionsFileName), content);
    }
    
    private static void InnerBuildAssetBundles(BuildTarget buildTarget, string channelName, bool writeConfig)
    {
        BuildAssetBundleOptions buildOption = BuildAssetBundleOptions.IgnoreTypeTreeChanges | BuildAssetBundleOptions.DeterministicAssetBundle;
        string outputPath = PackageUtils.GetAssetBundleOutputPath(buildTarget, channelName);
//        var old_manifest = GetCurrentManifest();
        
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, buildOption, buildTarget);
        if (manifest != null && writeConfig)
        {
            AssetsPathMappingEditor.BuildPathMapping(manifest);
            VariantMappingEditor.BuildVariantMapping(manifest);
            manifest = BuildPipeline.BuildAssetBundles(outputPath, buildOption, buildTarget);
        }

        WriteBundlesVersionFile(manifest);
        ClearUnuseFiles(outputPath, manifest);
        AssetDatabase.Refresh();

        //写入修改文件，刷新cdn
//        var new_manifest = GetCurrentManifest();
//        var updates = old_manifest.CompareTo(new_manifest);
//        SaveCDNFlushFile(updates);
    }

    private static void ClearUnuseFiles(string outputPath, AssetBundleManifest manifest)
    {
        var items = manifest.GetAllAssetBundles();
        var buildVersions = items.ToDictionary(item => item, item => manifest.GetAssetBundleHash(item).ToString());
        
        //clear no use files
        string[] ignoredFiles = { "AssetBundles","AssetBundleServerUrl", "versions.bytes", "assetsmap_bytes", "app_version.bytes" };
        var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
        var deletes = (from t in files
            let file = t.Replace('\\', '/').Replace(outputPath.Replace('\\', '/') + '/', "")
            where !file.EndsWith(".manifest", StringComparison.Ordinal) && !Array.Exists(ignoredFiles, s => s.Equals(file))
            where !buildVersions.ContainsKey(file)
            select t).ToList();

        foreach (string delete in deletes)
        {
            if (!File.Exists(delete))
            {
                continue;
            }

            File.Delete(delete);
            File.Delete(delete + ".manifest");
        }
        deletes.Clear();
    }
    
    public static Manifest GetCurrentManifest()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        string channelName = PackageUtils.GetCurSelectedChannel().ToString();
        
        string path = PackageUtils.GetAssetbundleManifestPath(buildTarget, channelName);
        Manifest manifest=new Manifest();

        if (File.Exists(path))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            manifest.LoadFromAssetbundle(assetBundle);
            assetBundle.Unload(false);
        }
              
        return manifest;
    }
    
    public static void BuildAssetBundles(BuildTarget buildTarget, string channelName)
    {
        bool buildForPerChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);

        PuertsMenu.CopyLuaFilesToAssetsPackage();
        PackageUtils.CheckAndRunAllCheckers(buildForPerChannel, false);

        DateTime start = DateTime.Now;
        if (buildForPerChannel)
        {
            start = DateTime.Now;
            CheckAssetBundles.SwitchChannel(channelName);
            Debug.Log("Finished CheckAssetBundles.SwitchChannel! use " + (DateTime.Now - start).TotalSeconds + "s");
        }

        start = DateTime.Now;
        InnerBuildAssetBundles(buildTarget, channelName, true);
        Debug.Log("Finished InnerBuildAssetBundles! use " + (DateTime.Now - start).TotalSeconds + "s");

        var targetName = PackageUtils.GetPlatformName(buildTarget);
        Debug.Log(string.Format("Build assetbundles for platform : {0} and channel : {1} done!", targetName, channelName));
    }

    public static void BuildAssetBundlesForAllChannels(BuildTarget buildTarget)
    {
        bool buildForPerChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);
        var targetName = PackageUtils.GetPlatformName(buildTarget);
        Debug.Assert(buildForPerChannel == true);

        PuertsMenu.CopyLuaFilesToAssetsPackage();
        PackageUtils.CheckAndRunAllCheckers(buildForPerChannel, false);

        int index = 0;
        double switchChannel = 0;
        double buildAssetbundles = 0;
        var start = DateTime.Now;
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            var channelName = current.ToString();

            start = DateTime.Now;
            CheckAssetBundles.SwitchChannel(channelName);
            switchChannel = (DateTime.Now - start).TotalSeconds;

            start = DateTime.Now;
            InnerBuildAssetBundles(buildTarget, channelName, index == 0);
            buildAssetbundles = (DateTime.Now - start).TotalSeconds;

            index++;
            Debug.Log(string.Format("{0}.Build assetbundles for platform : {1} and channel : {2} done! use time : switchChannel = {3}s , build assetbundls = {4} s", index, targetName, channelName, switchChannel, buildAssetbundles));
        }

        Debug.Log(string.Format("Build assetbundles for platform : {0} for all {1} channels done!", targetName, index));
    }

    public static void BuildAssetBundlesForCurSetting()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        string channelName = PackageUtils.GetCurSelectedChannel().ToString();
        BuildAssetBundles(buildTarget, channelName);
    }

    private static void SetPlayerSetting(BaseChannel channel)
    {
        if (channel != null)
        {
#if UNITY_5_6_OR_NEWER
            PlayerSettings.applicationIdentifier = channel.GetBundleID();
#else
            PlayerSettings.bundleIdentifier = channel.GetBundleID();
#endif
            PlayerSettings.productName = channel.GetProductName();
            PlayerSettings.companyName = channel.GetCompanyName();
        }
    }

    public static void BuildAndroid(string channelName, bool isTest)
    {
        BuildTarget buildTarget = BuildTarget.Android;
        PackageUtils.CopyAssetBundlesToStreamingAssets(buildTarget, channelName);
        if (!isTest)
        {
            LaunchAssetBundleServer.ClearAssetBundleServerURL();
        }
        else
        {
            LaunchAssetBundleServer.WriteAssetBundleServerURL();
        }
        
        BaseChannel channel = ChannelManager.Instance.CreateChannel(channelName);
        SetPlayerSetting(channel);

        string savePath = PackageUtils.GetChannelOutputPath(buildTarget, channelName);
        string appName = channel.GetProductName() + ".apk";
        if (channel.IsGooglePlay())
        {
            savePath = Path.Combine(savePath, "GooglePlay");
            GameUtility.SafeDeleteDir(savePath);
            BuildPipeline.BuildPlayer(GetBuildScenes(), savePath, buildTarget, BuildOptions.AcceptExternalModificationsToPlayer);
        }
        else
        {
            savePath = Path.Combine(savePath, appName);
            BuildPipeline.BuildPlayer(GetBuildScenes(), savePath, buildTarget, BuildOptions.None);
        }
        string outputPath = Path.Combine(Application.persistentDataPath, AssetBundleConfig.AssetBundlesFolderName);
        GameUtility.SafeDeleteDir(outputPath);
        Debug.Log(string.Format("Build android player for : {0} done! output ：{1}", channelName, savePath));
    }
    
    public static void BuildXCode(string channelName, bool isTest)
    {
        BuildTarget buildTarget = BuildTarget.iOS;
        PackageUtils.CopyAssetBundlesToStreamingAssets(buildTarget, channelName);
        if (!isTest)
        {
            LaunchAssetBundleServer.ClearAssetBundleServerURL();
        }
        else
        {
            LaunchAssetBundleServer.WriteAssetBundleServerURL();
        }

        string buildFolder = Path.Combine(System.Environment.CurrentDirectory, XCodeOutputPath);
        buildFolder = Path.Combine(buildFolder, channelName);
        GameUtility.CheckDirAndCreateWhenNeeded(buildFolder);

        string iconPath = "Assets/Editor/icon/ios/{0}/{1}.png";
        string[] iconSizes = new string[] { "180", "167","152", "144", "120", "114", "76", "72", "57" };
        List<Texture2D> iconList = new List<Texture2D>();
        for (int i = 0; i < iconSizes.Length; i++)
        {
            Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(string.Format(iconPath, channelName, iconSizes[i]), typeof(Texture2D));
            iconList.Add(texture);
        }
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, iconList.ToArray());

        BaseChannel channel = ChannelManager.Instance.CreateChannel(channelName);
        SetPlayerSetting(channel);

        PackageUtils.CheckAndAddSymbolIfNeeded(buildTarget, channelName);
        BuildPipeline.BuildPlayer(GetBuildScenes(), buildFolder, buildTarget, BuildOptions.None);

        string outputPath = Path.Combine(Application.persistentDataPath, AssetBundleConfig.AssetBundlesFolderName);
        GameUtility.SafeDeleteDir(outputPath);
    }
	
	static string[] GetBuildScenes()
	{
		List<string> names = new List<string>();
		foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e != null && e.enabled)
            {
                names.Add(e.path);
            }
        }
        return names.ToArray();
    }
}
