using UnityEngine;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Threading;

/// <summary>
/// 和服务端通信的消息类，存储请求类型和具体信息
/// </summary>
public class Message {
    public string type;
    public string info;
    public Message(string a, string b) {
        type = a;
        info = b;
    }
}

public class UserClient : MonoBehaviour {

    //服务端的IP和端口
    public static string HOST;
    public static int PORT;
    public static Socket socket;

    //是否畅通
    public static bool isAvailable() { return socket != null && socket.Connected; }

    /// <summary>
    /// 连接服务器
    /// </summary>
    public static void Connect() {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(HOST, PORT);
    }

    /// <summary>
    /// 向服务端发送一条消息
    /// </summary>
    public static void SendMessage(Message msg) {
        string info = JsonConvert.SerializeObject(msg) + "&";
        byte[] bytes = System.Text.Encoding.Default.GetBytes(info);
        if (isAvailable()) socket.Send(bytes);
    }

    /// <summary>
    /// 发送重要信息，要求一定时间内必须得到某种回复（阻塞方法）
    /// </summary>
    /// <param name="waitType">等待消息的type</param>
    /// <param name="maxWait">最大等待时间</param>
    /// <returns>回复内容</returns>
    public static string SendVitalMessage(Message msg, string waitType, int maxWait = 3000) {
        string info = JsonConvert.SerializeObject(msg) + "&";
        byte[] bytes = System.Text.Encoding.Default.GetBytes(info);

        Listener.waiting[waitType] = "";
        if(isAvailable()) socket.Send(bytes);

        while(maxWait > 0) {
            Thread.Sleep(50);
            maxWait -= 50;
            if(Listener.waiting[waitType] != "") {
                string res = Listener.waiting[waitType];
                Listener.waiting.Remove(waitType);
                return res;
            }
        }
        return null;
    }
}
