using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Configuration;

namespace OPC.Server
{
	/// <summary>
	/// 读取CSV配置文件并生成节点
	/// </summary>
    public class CreateAddressSpace
    {
		/// <summary>
		/// key - NodeId   value - OPC 节点对象，OPC原始代码
		/// </summary>
        private Dictionary<string, BaseDataVariableState> NodePool;

        private Dictionary<string, FolderState> FolderPool;
        private NodeTools Tools;

        public CreateAddressSpace(NodeTools tools, Dictionary<string, BaseDataVariableState> nodepool, Dictionary<string, FolderState> folderpool)
        {
            Tools = tools;
            NodePool = nodepool;
            FolderPool = folderpool;
        }

        public FolderState AddressSpace()
        {
            FolderState folder = Tools.CreateFolder(null, "Real_Time_Data", "RealTimeData");
            Dictionary<string, List<string>> dc = new Dictionary<string, List<string>>();

            string path = ConfigurationManager.AppSettings["CSVPath"];

            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var streamReader = new StreamReader(fileStream, Encoding.Default);

            Dictionary<string, Dictionary<string, List<string>>> dictionary = new Dictionary<string, Dictionary<string, List<string>>>();
            string s1 = "";
            string s2 = "";

            while (!streamReader.EndOfStream)
            {
                string ss = streamReader.ReadLine();
                string[] sss = ss.Split(',');
                if (sss[0] != "")
                {
                    s1 = sss[0];
                }
                if (sss[1] != "")
                {
                    s2 = sss[1];
                    dictionary.Add(s1 + "," + s2, new Dictionary<string, List<string>>());
                }
                dictionary[s1 + "," + s2].Add(sss[2], new List<string>());
                dictionary[s1 + "," + s2][sss[2]].Add(sss[3]);
                dictionary[s1 + "," + s2][sss[2]].Add(sss[4]);
            }

            foreach (var k in dictionary.Keys)
            {
                FolderState folderf = Tools.CreateFolder(folder, k.Split(',')[0], k);
                // NodePool.Add(k, new Dictionary<string, BaseDataVariableState>());
                foreach (var kk in dictionary[k].Keys)
                {
                    switch (dictionary[k][kk][1])
                    {
                        case "BOOL":
                            {
                                NodePool.Add(dictionary[k][kk][0], Tools.CreateVariable(folderf, dictionary[k][kk][0].Split('=')[2], kk, BuiltInType.Boolean, ValueRanks.Scalar));
                            }
                            break;

                        case "String":
                            {
                                NodePool.Add(dictionary[k][kk][0], Tools.CreateVariable(folderf, dictionary[k][kk][0].Split('=')[2], kk, BuiltInType.String, ValueRanks.Scalar));
                            }
                            break;

                        case "Int":
                            {
                                NodePool.Add(dictionary[k][kk][0], Tools.CreateVariable(folderf, dictionary[k][kk][0].Split('=')[2], kk, BuiltInType.Int32, ValueRanks.Scalar));
                            }
                            break;

                        case "Float":
                            {
                                NodePool.Add(dictionary[k][kk][0], Tools.CreateVariable(folderf, dictionary[k][kk][0].Split('=')[2], kk, BuiltInType.Float, ValueRanks.Scalar));
                            }
                            break;
                        case "Byte":
                            {
                                NodePool.Add(dictionary[k][kk][0], Tools.CreateVariable(folderf, dictionary[k][kk][0].Split('=')[2], kk, BuiltInType.Byte, ValueRanks.Scalar));
                            }break;
                    }
                }
            }

            return folder;
        }
    }
}