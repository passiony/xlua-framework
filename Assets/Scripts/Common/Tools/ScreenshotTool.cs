using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.IO;

/// <summary>
/// 截图工具，移动端
/// 1.保存当前手机屏幕画面，到persistent目录
/// 2.刷新相册，需要额外调用方法
/// 3.pc下比较简单，可以直接Application.ScreenShot()
/// </summary>
public class ScreenshotTool
{
    //截屏保存，回调路径
    public static void CaptureImage(string filePath, Action<string> callback)
    {
        CoroutineTool.Instance.StartCoroutine(CoCapture(filePath, callback));
    }

    //截屏协程
    private static IEnumerator CoCapture(string filePath, Action<string> callback)
    {
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);
        yield return new WaitForEndOfFrame();
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, true);
        tex.Apply();
        yield return tex;
        byte[] bytes = tex.EncodeToJPG();

        GameUtility.SafeWriteAllBytes(filePath, bytes);
        yield return new WaitForEndOfFrame();

        if (callback != null)
        {
            callback(filePath);
        }
    }

    //Android下刷新图片，显示到相册中
    public static void RefreshPhoto(string path)
    {
        using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject playerActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            using (AndroidJavaObject Conn = new AndroidJavaObject("android.media.MediaScannerConnection", playerActivity, null))
            {
                Conn.CallStatic("scanFile", playerActivity, new string[1] { path }, null, null);
            }
        }
    }
}