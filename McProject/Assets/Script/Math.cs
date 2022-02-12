using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Math : MonoBehaviour
{
    /// <summary>
    /// 将水平旋转角度转化为水平向量
    /// </summary>
    /// <param name="Hrot">输入为角度制</param>
    public static Vector3 getHVector(float Hrot) {//输入为角度制
        Hrot *= Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(Hrot), 0, Mathf.Cos(Hrot)).normalized;
    }

    /// <summary>
    /// 将水平向量转化为水平旋转角度
    /// </summary>
    /// <returns>返回角度制</returns>
    public static float getHRotation(Vector3 dir) {//输出为角度制，上0右90
        float ag = Mathf.Atan(dir.x / dir.z) * Mathf.Rad2Deg;
        ag = (ag % 180 + 180) % 180;
        if(dir.x < 0) ag += 180;
        if(dir.x == 0) ag = dir.z > 0 ? 0 : 180;
        return ag;
    }

}
