using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保存对一个方块的修改信息
/// </summary>
public class WorldModify {
    public int x, y, z, to;
    /// <summary>
    /// 这个方块是否在限制范围内
    /// </summary>
    public bool inside() {
        if (x < 0 || x > Global.maxX)
            return false;
        if (y < 0 || y > Global.maxY)
            return false;
        if (z < 0 || z > Global.maxZ)
            return false;
        return true;
    }
}


public class World : MonoBehaviour {

    public int[,,] cube = new int[50, 20, 50];

    [System.NonSerialized]
    public GameObject[,,] inscube = new GameObject[50, 20, 50];

    /// <summary>
    /// 修改一个方块
    /// </summary>
    public void modify(WorldModify p) {
        cube[p.x, p.y, p.z] = p.to;
        if (inscube[p.x, p.y, p.z] != null) Destroy(inscube[p.x, p.y, p.z]);
        if (p.to != 0) {
            inscube[p.x, p.y, p.z] = Instantiate(Global.ins.cubePrefab[p.to], new Vector3(p.x, p.y, p.z), Quaternion.identity);
            inscube[p.x, p.y, p.z].transform.SetParent(Global.ins.CubeBox.transform);
        }
    }

    public void clear() {
        for(int i = 0; i < 50; i++) {
            for(int j = 0;j < 20; j++) {
                for(int k = 0;k < 50; k++) {
                    if (inscube[i, j, k] != null)
                        Destroy(inscube[i, j, k]);
                    cube[i, j, k] = 0;
                }
            }
        }
    }

    public static World ins;
    private void Awake() {
        ins = this;
    }
}