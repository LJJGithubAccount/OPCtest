using NLog;
using ProtoBuf;
using RabbitMQ.Client;
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
    [ProtoContract]
    public struct Msg
    {
        //信号名称，信号值字典
        [ProtoMember(1)]
        public Dictionary<string, string> MsgDic;
    }

    public struct AGVSMSG
    {
        public string Name;

        public Dictionary<string, object> MsgDic;
    }

	/// <summary>
	/// 和消息队列的数据通信
	/// </summary>
    public static class MQ_Client

    {
        private static ConnectionFactory factory;
        private static IConnection connection;
        private static List<IModel> channelList;
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static bool state = false;
        public static bool IsOpen { get { return state; } }

        static MQ_Client()
        {
            Connect();
            state = true;
        }
        static void Connect()

        {
            try
            {
                factory = new ConnectionFactory()
                {
                    HostName = ConfigurationManager.AppSettings["Host"],
                    UserName = ConfigurationManager.AppSettings["User"],
                    Password = "Server2016!",
                    VirtualHost = ConfigurationManager.AppSettings["VirtualHost"]
                };
                connection = factory.CreateConnection();
                channelList = new List<IModel>();
            }
            catch(Exception e)
            {
                log.Error(e.StackTrace + "::" + e.Message);
                log.Error("1000ms 后尝试重连");
                Thread.Sleep(1000);
                Connect();
            }
        }

        /// <summary>
        /// 消息接受回调注册方法
        /// </summary>
        /// <param name="Received">需要注册的方法</param>
        /// <param name="queue">队列名</param>
        public static void ReceivedMsg(EventHandler<BasicDeliverEventArgs> Received, string queue)
        {
            var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += Received;
            channel.BasicConsume(queue: queue,
                                 autoAck: true,
                                 consumer: consumer);
            channelList.Add(channel);
        }
    }
}