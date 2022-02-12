using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MyWorld {
    public class WorldModify {//更改世界用的结构体
        public int x, y, z, to;
        public WorldModify(int a, int b, int c, int d) {
            x = a;
            y = b;
            z = c;
            to = d;
        }
    }
    public class World {//世界信息
        public int[,,] cube = new int[50, 20, 50];
        public static string SAVE_PATH = "map/world.txt";

        public void modify(WorldModify p) {
            cube[p.x, p.y, p.z] = p.to;
        }
        /// <summary>
        /// 将世界打包成一个从零开始的WorldModify列表
        /// </summary>
        public List<WorldModify> modifyList() {
            List<WorldModify> res = new List<WorldModify>();
            for (int x = 0; x < 50; x++) {
                for (int y = 0; y < 20; y++) {
                    for (int z = 0; z < 50; z++) {
                        if (cube[x, y, z] != 0) {
                            res.Add(new WorldModify(x, y, z, cube[x, y, z]));
                        }
                    }
                }
            }
            return res;
        }
        /// <summary>
        /// 保存世界
        /// </summary>
        public void SaveWorld() {
            StreamWriter sw = new StreamWriter(SAVE_PATH);
            sw.Write(JsonConvert.SerializeObject(modifyList()));
            sw.Flush();
            sw.Close();
        }
        /// <summary>
        /// 读取或新建世界
        /// </summary>
        public World(string path) {
            SAVE_PATH = path;
            if (!File.Exists(path)) {
                for (int xx = 0; xx <= 40; xx++) {
                    for (int yy = 0; yy <= 7; yy++) {
                        for (int zz = 0; zz <= 40; zz++) {
                            if (yy == 7)
                                cube[xx, yy, zz] = 2;
                            else if (yy == 0)
                                cube[xx, yy, zz] = 4;
                            else
                                cube[xx, yy, zz] = 1;
                        }
                    }
                }
                SaveWorld();
            } else {
                StreamReader sw = new StreamReader(path);
                string str = sw.ReadToEnd();
                List<WorldModify> all = JsonConvert.DeserializeObject<List<WorldModify>>(str);
                foreach (WorldModify p in all) {
                    cube[p.x, p.y, p.z] = p.to;
                }
                sw.Close();
            }
        }
    }
}
