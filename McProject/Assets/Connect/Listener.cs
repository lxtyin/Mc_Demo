using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading;
using System.Net.Sockets;

public class Listener : MonoBehaviour {

    public Text Warn;

    /// <summary>
    /// 一个侦听表，在侦听到特定message时，将消息内容填入
    /// </summary>
    public static Dictionary<string, string> waiting = new Dictionary<string, string>();

    /// <summary>
    /// 启动本地对服务器的监听
    /// </summary>
    public static void StartListening() {
        Thread Listening = new Thread(new ThreadStart(Listen));
        Listening.Start();
    }

    static byte[] readBuff = new byte[1024000];
    static void Listen() {//监听，子线程
        while(UserClient.isAvailable()) {
            try {
                int count = UserClient.socket.Receive(readBuff);
                string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                foreach (string s in str.Split('&')) {
                    if(s.Length > 0) {
                        Message recv = JsonConvert.DeserializeObject<Message>(s);
                        //转到主线程中执行监听到的消息
                        toDolist.Enqueue(recv);
                        if (waiting.ContainsKey(recv.type))
                            waiting[recv.type] = recv.info;
                    }
                }
            } catch(SocketException) {
                break;
            } catch (System.Exception e) {
                Debug.Log(e.Message);
            }
        }
        Thread.Sleep(1000);
        if(!Title.inTitle) {
            toDolist.Enqueue(new Message("ExitGame", "炸服了"));
        }
    }

    /// <summary>
    /// Listener将待处理的Message通过此队列转交给主进程执行
    /// </summary>
    static Queue<Message> toDolist = new Queue<Message>();
    private void Update() {

        if(toDolist.Count > 0) {
            Message recv = toDolist.Dequeue();
            switch(recv.type) {
                case "Logout":
                    PlayerPool.Remove(recv.info);
                    break;
                case "UpdateWorld":
                    //世界被修改
                    WorldModify wd = JsonConvert.DeserializeObject<WorldModify>(recv.info);
                    World.ins.modify(wd);
                    break;
                case "UpdateAllUser":
                    var listinfo = JsonConvert.DeserializeObject<List<UserInfo>>(recv.info);
                    PlayerPool.Fresh(listinfo);
                    break;
                case "UpdateWorldList":
                    World.ins.clear();
                    List<WorldModify> allmodi = JsonConvert.DeserializeObject<List<WorldModify>>(recv.info);
                    foreach(var i in allmodi) {
                        World.ins.modify(i);
                    }
                    break;
                case "ExitGame":
                    Title.ins.ShowWarn(recv.info);
                    Title.ins.BackToTitle();
                    break;
            }
        }
    }
}