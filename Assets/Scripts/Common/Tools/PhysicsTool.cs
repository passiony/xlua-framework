using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsTool : MonoBehaviour
{
    /// <summary>
    /// 线性射线检测
    /// </summary>
    /// <param name="beginPos"></param>
    /// <param name="endPos"></param>
    /// <param name="hitInfo"></param>
    /// <returns></returns>
    public static bool Linecast(Vector3 beginPos,Vector3 endPos,out RaycastHit hitInfo)
    {
        return Physics.Linecast(beginPos, endPos, out hitInfo);
    }


    /// <summary>
    /// 获取摄像机发射的射线检测
    /// </summary>
    /// <param name="layer">单个层的index</param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static RaycastHit? RayCastLayer(int distance, int layer)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit, distance, 1<<layer))
        {
            return hit;
        }
        return null;
    }

    /// <summary>
    /// 摄像机发射射线检测多个层
    /// </summary>
    /// <param name="distance">距离</param>
    /// <param name="layer">多个层级的layerMask</param>
    /// <returns></returns>
    public static RaycastHit? RayCastLayers(int distance, int layer)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, distance, layer))
        {
            return hit;
        }
        return null;
    }

    /// <summary>
    /// Line射线，检测单个层
    /// </summary>
    /// <param name="origin">起点</param>
    /// <param name="drection">方向</param>
    /// <param name="distance">距离</param>
    /// <param name="layer">单个层级的index</param>
    /// <returns></returns>
    public static RaycastHit? LineCastLayer(Vector3 origin,Vector3 drection, int distance, int layer)
    {
        RaycastHit hit;
        Debug.DrawLine(origin, origin + drection * distance,Color.red);
        if (Physics.Raycast(origin, drection, out hit, distance, 1<<layer))
        {
            return hit;
        }
        return null;
    }

    /// <summary>
    /// Line射线，检测多个层
    /// </summary>
    /// <param name="origin">起点</param>
    /// <param name="drection">方向</param>
    /// <param name="distance">距离</param>
    /// <param name="layer">多个层级的layerMask</param>
    /// <returns></returns>
    public static RaycastHit? LineCastLayers(Vector3 origin, Vector3 drection, int distance, int layer)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, drection, out hit, distance, layer))
        {
            return hit;
        }
        return null;
    }

}
