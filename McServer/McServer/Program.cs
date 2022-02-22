using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Threading;
using MyWorld;
using System.IO;

namespace MyServer {

    class UserInfo {//玩家信息
        public string playername;
        public float px, py, pz;
        public float rh;
    }

    /// <summary>
    /// 和服务端通信的消息类，存储请求类型和具体信息
    /// </summary>
    class Message {
        public string type;
        public string info;
        public Message(string a, string b) {
            type = a;
            info = b;
        }
    }

    class Program {

        const int PORT = 8888;             //服务端绑定的端口
        const int MAX_CONNECT = 20;        //最大连接数

        static World world;                 //保存世界信息

        static List<Socket> userOnline = new List<Socket>();//几个list一一对应
        static List<UserInfo> allPlayer = new List<UserInfo>();
        static List<bool> available = new List<bool>();

        /// <summary>
        /// 这个Socket是否可用
        /// </summary>
        static bool isAvailable(Socket x) { return x != null && x.Connected; }

        /// <summary>
        /// 主动向客户端发送消息
        /// </summary>
        static void sendToClient(Socket sclient, Message msg) {
            try {
                byte[] bytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(msg) + '&');
                sclient.Send(bytes);
            } catch(SocketException) {
                string clientIP = ((IPEndPoint)sclient.RemoteEndPoint).Address.ToString();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(clientIP + " 失去连接\n");
                userLogout(sclient);
                return;
            }
        }

        /// <summary>
        /// 向所有客户端广播消息
        /// </summary>
        static void sendToAllClient(Message msg) {
            for (int i = 0; i < userOnline.Count; i++) {
                if (isAvailable(userOnline[i])) sendToClient(userOnline[i], msg);
            }
        }

        /// <summary>
        /// 处理用户登录请求，再向其发送允许登录或失败消息
        /// </summary>
        static void userLoin(Socket sclient, string info) {//Loin是每个用户登录时发送的第一个请求，根据Loin结果为其建立存储信息
            UserInfo p = JsonConvert.DeserializeObject<UserInfo>(info);
            string clientIP = ((IPEndPoint)sclient.RemoteEndPoint).Address.ToString();
            int clientPort = ((IPEndPoint)sclient.RemoteEndPoint).Port;

            int idx = allPlayer.FindIndex(delegate (UserInfo cl) { return cl.playername == p.playername; });
            if(idx != -1) {
                sendToClient(sclient, new Message("LoinResult", p.playername + " has already in this game!"));
                sclient.Close();
            } else {
                sendToClient(sclient, new Message("LoinResult", "Success"));
                userOnline.Add(sclient);
                allPlayer.Add(p);
                available.Add(true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("Name:{0} Loined.\nIP:{1} Port:{2}", p.playername, clientIP, clientPort));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(DateTime.Now.ToString() + '\n');
            }
        }

        /// <summary>
        /// 处理用户登出
        /// </summary>
        static void userLogout(Socket sclient) {
            string clientIP = ((IPEndPoint)sclient.RemoteEndPoint).Address.ToString();
            int clientPort = ((IPEndPoint)sclient.RemoteEndPoint).Port;
            int idx = userOnline.FindIndex(delegate (Socket cl) { return cl == sclient; });
            if(idx == -1) return;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Name:{0} Logout.\nIP:{1} Port:{2}", allPlayer[idx].playername, clientIP, clientPort));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(DateTime.Now.ToString() + '\n');

            sendToAllClient(new Message("Logout", allPlayer[idx].playername));//广播此条登出信息
            userOnline.RemoveAt(idx);
            allPlayer.RemoveAt(idx);
            available.RemoveAt(idx);
            sclient.Close();
        }

        /// <summary>
        /// 子线程通过toDoList将请求转交给主线程完成，避免异步冲突
        /// </summary>
        static Queue<KeyValuePair<Socket, Message>> toDoList = new Queue<KeyValuePair<Socket, Message>>();
        private static readonly object toDoListLock = new object();

        /// <summary>
        /// 定期执行一些事务的子线程
        /// </summary>
        static void autoWork() {
            int time = 0;
            while(true) {
                Thread.Sleep(40);
                time = (time + 40) % 100000;
                lock (toDoListLock) {
                    if(time % 40 == 0)//T = 0.04s
                        toDoList.Enqueue(new KeyValuePair<Socket, Message>(null, new Message("UpdateAllUser", "")));
                    if(time % 5000 == 0) {//T = 5s
                        world.SaveWorld();
                        toDoList.Enqueue(new KeyValuePair<Socket, Message>(null, new Message("ClearUser", "")));
                    }
                }
            }
        }

        /// <summary>
        /// 监听子线程
        /// </summary>
        static void listenClient(object obj) {
            Socket sclient = (Socket)obj;

            byte[] readBuff = new byte[1024];
            while(isAvailable(sclient)) {
                try {
                    //Socket.Receive是阻塞方法，等待直到客户端发送数据
                    int count = sclient.Receive(readBuff);
                    string str = Encoding.UTF8.GetString(readBuff, 0, count);
                    foreach(string s in str.Split('&')) {
                        if(s.Length > 0) {
                            Message recv = JsonConvert.DeserializeObject<Message>(s);
                            //转交主线程程执行玩家的请求
                            lock(toDoListLock) {
                                toDoList.Enqueue(new KeyValuePair<Socket, Message>(sclient, recv));
                            }
                        }
                    }
                } catch {
                    //直接socket.close()来终止这个子线程
                    break;
                }
            }
        }

        /// <summary>
        /// 等待用户连接
        /// </summary>
        static void waitClient() {
            Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenfd.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), PORT));

            //开启监听（注意这只是一个开启，不是阻塞方法，参数为最大连接的客户端数量）
            listenfd.Listen(MAX_CONNECT);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nServer is working..");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(DateTime.Now.ToString() + '\n');
            while (true) {
                Socket sclient = listenfd.Accept();
                //每收到一个客户端的连接，就为其单独开一个子线程来接收消息
                Thread newthd = new Thread(new ParameterizedThreadStart(listenClient));
                newthd.Start(sclient);
            }
        }

        static void Main(string[] args) {

            Console.WriteLine("输入地图名以打开或新建一张地图：");
            string path = Console.ReadLine();

            //一些初始化，创建世界，开启子线程
            world = new World("map/" + path + ".txt");
            Thread auto = new Thread(new ThreadStart(autoWork));
            auto.Start();
            Thread waiter = new Thread(new ThreadStart(waitClient));
            waiter.Start();

            //主线程中处理请求
            while (true) {
                if(toDoList.Count > 0) {
                    Socket from = toDoList.Peek().Key;
                    Message msg = toDoList.Dequeue().Value;
                    switch(msg.type) {
                        case "Loin":
                            //登录
                            userLoin(from, msg.info);
                            break;
                        case "Logout":
                            //登出
                            userLogout(from);
                            break;
                        case "UpdateUser":
                            //更新用户位置信息
                            UserInfo p = JsonConvert.DeserializeObject<UserInfo>(msg.info);
                            int idx = allPlayer.FindIndex(delegate (UserInfo pl) { return pl.playername == p.playername; });
                            if (idx != -1) {
                                allPlayer[idx] = p;
                                available[idx] = true;
                            }
                            break;
                        case "UpdateWorld":
                            //更新一个方块
                            WorldModify wd = JsonConvert.DeserializeObject<WorldModify>(msg.info);
                            world.modify(wd);
                            sendToAllClient(msg);//广播此更新消息
                            break;
                        case "QueryAllWorld":
                            //请求发送全部世界信息（刚登陆）
                            sendToClient(from, new Message("UpdateWorldList", JsonConvert.SerializeObject(world.modifyList())));
                            break;
                        case "UpdateAllUser":
                            sendToAllClient(new Message("UpdateAllUser", JsonConvert.SerializeObject(allPlayer)));
                            break;
                        case "ClearUser":
                            List<Socket> tmp = new List<Socket>();
                            for(int i = 0; i < available.Count; i++) {
                                if (!available[i]) {
                                    string clientIP = ((IPEndPoint)userOnline[i].RemoteEndPoint).Address.ToString();
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(clientIP + " 长时间没有连接\n");
                                    tmp.Add(userOnline[i]);
                                }
                                available[i] = false;
                            }
                            foreach(Socket i in tmp) {
                                userLogout(i);
                            }
                            break;
                    }
                }
            }
        }
    }
}
