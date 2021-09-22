using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RenderTexture的使用工具类
/// 1.用于动态创建RenderTexture，用于3d人物等生成2d图片，显示到UI的RawImage上
/// 2.也可以只创建RenderTexture，但是不输出rawiamge,可以自行保存像素
/// 3.调用静态方法CreateRender即可
/// 4.页面关闭时，别忘记调用Destroy方法，销毁相机
/// </summary>
public class RenderCameraTool
{
    public Camera renderCamera;
    public GameObject target;
    public RenderTexture renderTexture;

    public static readonly Vector3 TARGET_POS = new Vector3(0, 0.55f, 7);
    public static readonly Vector3 TARGET_ROT = new Vector3(0, 180, 0);

    private RenderCameraTool()
    {

    }

    /// <summary>
    /// 为游戏对象go，生成一个渲染相机，输出画面到RenderTexture
    /// </summary>
    void AddRenderTex(Camera camera, int width,int height,int depth,int format)
    {
        renderCamera = camera;

        // 创建RenderTexture并绑定到摄像机上
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(width, height, depth, (RenderTextureFormat)format);
        }
        renderCamera.targetTexture = renderTexture;
    }

    /// <summary>
    /// 设置摄像机到人物节点下，并赋值相对位置和相对旋转
    /// </summary>
    public void SetTarget(GameObject go,Vector3? pos=null, Vector3? rot=null)
    {
        if (pos == null)
            pos = RenderCameraTool.TARGET_POS;
        if (rot == null)
            rot = RenderCameraTool.TARGET_ROT;

        target = go;
        renderCamera.transform.SetParent(target.transform, false);
        renderCamera.transform.localPosition = (Vector3)pos;
        renderCamera.transform.localEulerAngles = (Vector3)rot;
    }

    public byte[] GetBytes()
    {
        return GetBytes(this.renderTexture);
    }

    /// <summary>
    /// 隐藏相机
    /// </summary>
    /// <param name="visible"></param>
    public void SetVisible(bool visible)
    {
        renderCamera.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 删除相机
    /// </summary>
    public void Destroy()
    {
        GameObject.Destroy(renderTexture);
        GameObject.Destroy(renderCamera.gameObject);
    }


    #region Static Function

    public static RenderCameraTool CreateRender(Camera camera, RawImage raw, int width, int height,int depth,int format)
    {
        RenderCameraTool rt = new RenderCameraTool();
        rt.AddRenderTex(camera,width, height,depth, format);
        raw.texture = rt.renderTexture;

        return rt;
    }

    public static RenderCameraTool CreateRender(Camera camera,int width, int height, int depth, int format)
    {
        RenderCameraTool rt = new RenderCameraTool();
        rt.AddRenderTex(camera,width, height, depth,format);

        return rt;
    }

    /// <summary>
    /// 创建拍摄用的摄像机
    /// </summary>
    public static Camera CreateCamera(int clearFlags, int layerMask)
    {
        GameObject go = new GameObject("ModelCamera");
        Camera _camera = go.AddComponent<Camera>();
        _camera.clearFlags = (CameraClearFlags)clearFlags;//depth做透明背景，color做
        _camera.cullingMask = layerMask;
        _camera.backgroundColor = Color.clear;
        _camera.fieldOfView = 10;

        return _camera;
    }

    /// <summary>
    /// Texture转Texture2D格式
    /// </summary>
    public static Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);

        return texture2D;
    }

    /// <summary>
    /// 获取RawImage的bytes
    /// </summary>
    public static byte[] GetBytes(RawImage image)
    {
        Texture2D tex2d = TextureToTexture2D(image.texture);
        return tex2d.EncodeToPNG();
    }

    /// <summary>
    /// 获取RenderTexture的bytes
    /// </summary>
    public static byte[] GetBytes(RenderTexture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = texture;
        texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;

        return texture2D.EncodeToPNG();
    }

    #endregion
}