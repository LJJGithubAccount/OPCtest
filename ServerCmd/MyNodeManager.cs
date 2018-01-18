using Newtonsoft.Json;
using NLog;
using Opc.Ua;
using Opc.Ua.Server;
using ProtoBuf;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPC.Server
{
	/// <summary>
	/// 将消息队列的接受值绑定到消息队列节点上
	/// </summary>
    internal class MyNodeManager : CustomNodeManager2
    {
        private ReferenceServerConfiguration m_configuration;
        private const string ReferenceApplications = "http://opcfoundation.org/Quickstarts/ReferenceApplications";
        private NodeTools tools;
        private FolderState root;
        private Dictionary<string, BaseDataVariableState> NodePool;
        private Dictionary<string, FolderState> FolderPool;
        private Dictionary<string, Dictionary<string, string>> AGVS;
        Logger log = LogManager.GetCurrentClassLogger();

        public MyNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        :
            base(server, configuration, ReferenceApplications)
        {
            SystemContext.NodeIdFactory = this;

            // get the configuration for the node manager.
            m_configuration = configuration.ParseExtension<ReferenceServerConfiguration>();

            // use suitable defaults if no configuration exists.
            if (m_configuration == null)
            {
                m_configuration = new ReferenceServerConfiguration();
            }
            tools = new NodeTools(SystemContext, NamespaceIndex, ReferenceApplications, Server);
            //m_dynamicNodes = new List<BaseDataVariableState>();


			///服务AGVS 直接固定配置
            AGVS = new Dictionary<string, Dictionary<string, string>>();
            AGVS.Add("8.1,AGV1", new Dictionary<string, string>());
            AGVS["8.1,AGV1"].Add("state", "ns=2;s=8.1.1");
            AGVS["8.1,AGV1"].Add("battery", "ns=2;s=8.1.2");
            AGVS["8.1,AGV1"].Add("linear", "ns=2;s=8.1.3");
            AGVS["8.1,AGV1"].Add("angular", "ns=2;s=8.1.4");
            AGVS["8.1,AGV1"].Add("x", "ns=2;s=8.1.5");
            AGVS["8.1,AGV1"].Add("y", "ns=2;s=8.1.6");
            AGVS["8.1,AGV1"].Add("yaw", "ns=2;s=8.1.7");
            AGVS["8.1,AGV1"].Add("WarningCode", "ns=2;s=8.1.8");
            AGVS["8.1,AGV1"].Add("JobCode", "ns=2;s=8.1.9");
            AGVS["8.1,AGV1"].Add("BatchCode", "ns=2;s=8.1.10");

            AGVS.Add("8.2,AGV2", new Dictionary<string, string>());
            AGVS["8.2,AGV2"].Add("state", "ns=2;s=8.2.1");
            AGVS["8.2,AGV2"].Add("battery", "ns=2;s=8.2.2");
            AGVS["8.2,AGV2"].Add("linear", "ns=2;s=8.2.3");
            AGVS["8.2,AGV2"].Add("angular", "ns=2;s=8.2.4");
            AGVS["8.2,AGV2"].Add("x", "ns=2;s=8.2.5");
            AGVS["8.2,AGV2"].Add("y", "ns=2;s=8.2.6");
            AGVS["8.2,AGV2"].Add("yaw", "ns=2;s=8.2.7");
            AGVS["8.2,AGV2"].Add("WarningCode", "ns=2;s=8.2.8");
            AGVS["8.2,AGV2"].Add("JobCode", "ns=2;s=8.2.9");
            AGVS["8.2,AGV2"].Add("BatchCode", "ns=2;s=8.2.10"); ;
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TBD
            }
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            BaseInstanceState instance = node as BaseInstanceState;

            if (instance != null && instance.Parent != null)
            {
                string id = instance.Parent.NodeId.Identifier as string;

                if (id != null)
                {
                    return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
                }
            }

            return node.NodeId;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }
                NodePool = new Dictionary<string, BaseDataVariableState>();
                FolderPool = new Dictionary<string, FolderState>();
                CreateAddressSpace cr = new CreateAddressSpace(tools, NodePool, FolderPool);

                root = cr.AddressSpace();

				///下三行代码就是将OPC所有节点添加到节点管理器中
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));

                AddRootNotifier(root);

                AddPredefinedNode(SystemContext, root);

				while (!MQ_Client.IsOpen)
					Thread.Sleep(1000);
				MQ_Client.ReceivedMsg(ReceivedMsg, ConfigurationManager.AppSettings["QueueName"]);
				MQ_Client.ReceivedMsg(ReceivedAGV1, ConfigurationManager.AppSettings["AGVSQueueName"]);
			}
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that are not in the namespace.
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                NodeHandle handle = new NodeHandle();

                handle.NodeId = nodeId;
                handle.Node = node;
                handle.Validated = true;

                return handle;
            }
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        protected override NodeState ValidateNode(
           ServerSystemContext context,
           NodeHandle handle,
           IDictionary<NodeId, NodeState> cache)
        {
            // not valid if no root.
            if (handle == null)
            {
                return null;
            }

            // check if previously validated.
            if (handle.Validated)
            {
                return handle.Node;
            }

            // TBD

            return null;
        }

        private void ReceivedMsg(Object model, BasicDeliverEventArgs ea)
        {
            var body = new MemoryStream(ea.Body);
            var msg = Serializer.Deserialize<Msg>(body);
            var ss = JsonConvert.SerializeObject(msg);
            log.Info(ss);

            lock (Lock)
            {
                foreach (var k in msg.MsgDic.Keys)
                {
					//将消息队列传过来的值赋值给OPC节点值，他们使用相同的Key，即NodeId
                    NodePool[k].Value = msg.MsgDic[k];
					//官方代码
                    NodePool[k].ClearChangeMasks(SystemContext, false);
					//六院要求增加的时间戳
                    NodePool[k].Timestamp = DateTime.Now;
                }
            }
        }

        private void ReceivedAGV1(Object model, BasicDeliverEventArgs ea)
        {
            string ss = Encoding.UTF8.GetString(ea.Body);
            var msg = JsonConvert.DeserializeObject<AGVSMSG>(ss);
            log.Info(ss);
            lock (Lock)
            {
                foreach (var k in AGVS[msg.Name].Keys)
                {
                    NodePool[AGVS[msg.Name][k]].Value = msg.MsgDic[k];
                    NodePool[AGVS[msg.Name][k]].ClearChangeMasks(SystemContext, false);
                    NodePool[AGVS[msg.Name][k]].Timestamp = DateTime.Now;
                }
            }
        }
    }
}