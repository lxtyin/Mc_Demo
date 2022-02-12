using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPool : MonoBehaviour {
    static Dictionary<string, GameObject> insPlayer = new Dictionary<string, GameObject>();//已经存在的玩家模型

    public static void Remove(string playername) {
        if (playername == Global.playerName && !Title.inTitle) {
            Title.ins.ShowWarn("你网不好");
            Title.ins.BackToTitle();
        } else if (insPlayer.ContainsKey(playername)) {
            Destroy(insPlayer[playername]);
            insPlayer.Remove(playername);
        }
    }

    /// <summary>
    /// 更新世界中的玩家信息
    /// </summary>
    public static void Fresh(List<UserInfo> allPlayer) {
        foreach(UserInfo p in allPlayer) {
            if(p.playername == Global.playerName) continue;
            if(!insPlayer.ContainsKey(p.playername)) {
                insPlayer[p.playername] = Instantiate(Global.ins.playerModel);
                insPlayer[p.playername].transform.Find("Name").GetComponent<TextMesh>().text = p.playername;
            }
            var g = insPlayer[p.playername];
            g.transform.position = new Vector3(p.px, p.py, p.pz);
            g.transform.eulerAngles = new Vector3(0, p.rh, 0);
        }
    }
}
