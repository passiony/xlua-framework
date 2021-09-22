using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手机获取摄像头拍摄的工具类
/// 1.该类为单利类，直接调用StartWebCamera即可使用
/// 2.需要传递一个RawImage参数，作为摄像机画面输出
/// </summary>
public class WebCamTool : MonoSingleton<WebCamTool>
{
    string DeviceName;
    bool isFront;

    public WebCamTexture webCamera;
    public AspectRatioFitter fit;
    public RawImage background;
    public float Scale = 0.5f;//根据不同设备定义

    public void StartWebCamera(RawImage raw, bool front = false)
    {
        background = raw;
        isFront = front;

        fit = background.GetComponent<AspectRatioFitter>();
        if(fit==null)
        {
            fit = background.gameObject.AddComponent<AspectRatioFitter>();
        }
        StartCoroutine(CoLoadWebCamera());
    }

    public void StopWebCamera()
    {
        if (webCamera != null)
            webCamera.Stop();
        webCamera = null;
    }

    public void Destroy()
    {
        StopWebCamera();
        Destroy(gameObject);
    }

    void Update()
    {
        if (webCamera == null)
            return;

        float ratio = (float)webCamera.width / (float)webCamera.height;
        fit.aspectRatio = ratio; // Set the aspect ratio  

        float scaleY = webCamera.videoVerticallyMirrored ? -1f : 1f; // Find if the camera is mirrored or not  
        background.rectTransform.localScale = new Vector3(Scale, scaleY * Scale, Scale); // Swap the mirrored camera  

        int orientY = isFront ? 180 : 0;//front and back switch
        int orientZ = -webCamera.videoRotationAngle;

        background.rectTransform.localEulerAngles = new Vector3(0, orientY, orientZ);
    }

    /// <summary>    
    /// 初始化摄像头  
    /// </summary>    
    IEnumerator CoLoadWebCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if(devices.Length>0)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    //开启前置后置摄像头
                    if (devices[i].isFrontFacing == isFront)
                    {
                        DeviceName = devices[i].name;
                        webCamera = new WebCamTexture(DeviceName, Screen.width, Screen.height, 60);
                        background.texture = webCamera;
                        webCamera.Play();
                        break;
                    }
                }
            }
        }
    }

}
