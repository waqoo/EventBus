﻿using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.DefaultConnection;

namespace YiDian.Soa.Sp.Extensions
{
    public static class MQExtensions
    {
        const string mqsettings = "mqsettings";
        /// <summary>
        /// 注册MQ连接字符串
        /// <para>格式：server=ip:port;user=username;password=pwd;vhost=vhostname;name=zs</para>
        /// eventsmgr=inmemory
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getconnstr"></param>
        /// <returns></returns>
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Action<DefaultMqConnectSource> action = null, IAppEventsManager eventsManager = null, IEventSeralize seralizer = null)
        {
            var obj = builder.GetTag(mqsettings);
            if (obj != null) throw new ArgumentException("can not  repeat register the rabbit-mq depend items");
            builder.Services.AddSingleton(sp =>
            {
                var service = builder.Services;
                eventsManager = eventsManager ?? new DefaultEventsManager();
                service.AddSingleton(eventsManager);
                seralizer = seralizer ?? new DefaultSeralizer();
                var sub_logger = sp.GetService<ILogger<IEventBusSubManager>>();
                var subfact = new InMemorySubFactory(eventsManager, sub_logger);
                var connSource = new DefaultMqConnectSource(eventsManager, seralizer, sub_logger, 5, subfact);
                action?.Invoke(connSource);
                return connSource;
            });
            builder.Services.AddSingleton(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var dbs = sp.GetService<ILogger<IDirectEventBus>>();
                var tbs = sp.GetService<ILogger<ITopicEventBus>>();
                var busfact = new EventBusFactory(source, sp, dbs, tbs);
                return busfact;
            });
            builder.SetTag(mqsettings, new object());
            return builder;
        }
        public static SoaServiceContainerBuilder UseMqRpcClient(this SoaServiceContainerBuilder builder, string clientName)
        {
            if (string.IsNullOrEmpty(clientName)) return builder;
            var now = DateTime.Now.ToString("MMddHHmmss");
            clientName = "rpcC-" + now + "-" + clientName;
            builder.Services.AddSingleton<IRpcClientFactory, RpcClientFactory>(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var conn = source.Get("") ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var loggerfact = sp.GetService<ILoggerFactory>();
                var qps = sp.GetService<IQpsCounter>();
                var logger = sp.GetService<ILogger<IMQRpcClient>>();
                var rpc = new MQRpcClientBase(conn, clientName, logger, qps);
                return new RpcClientFactory(rpc);
            });
            return builder;
        }
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, string mqConnstr, string enven_mgr_api = "", IEventSeralize seralizer = null)
        {
            return UseRabbitMq(builder, x => x.Create(mqConnstr), string.IsNullOrEmpty(enven_mgr_api) ? null : new HttpEventsManager(enven_mgr_api), seralizer);
        }
        public static SoaServiceContainerBuilder UseRabbitMq(this SoaServiceContainerBuilder builder, Action<DefaultMqConnectSource> action, string enven_mgr_api = "", IEventSeralize seralizer = null)
        {
            return UseRabbitMq(builder, action, string.IsNullOrEmpty(enven_mgr_api) ? null : new HttpEventsManager(enven_mgr_api), seralizer);
        }
        /// <summary>
        /// 创建系统所依赖的消息总线中的消息类型
        /// </summary>
        /// <param name="builder">builder</param>
        /// <param name="all_apps">所依赖的消息系统名称列表，以逗号隔开
        /// <para>为空时，从配置文件中加载键为 dependApps 的值</para>
        /// </param>
        /// <param name="fileDir">创建消息 体的目录
        /// <para>为空时，使用当前系统目录 依赖命令行传入键为-pj_dir的参数</para>
        /// </param>
        /// <returns>builder</returns>
        public static SoaServiceContainerBuilder AutoCreateAppEvents(this SoaServiceContainerBuilder builder, string all_apps = "", string fileDir = "")
        {
            var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            if (string.IsNullOrEmpty(all_apps)) all_apps = config["dependApps"];
            if (string.IsNullOrEmpty(fileDir)) fileDir = builder.Project_Dir;
            if (string.IsNullOrEmpty(fileDir)) throw new ArgumentNullException(nameof(fileDir), "Project Dir is null ,Project_Dir depand the commandline '-pj_dir'");
            var service = builder.Services;
            builder.RegisterRun(new MqEventsLocalBuild());
            //--loadevents -app history,userapi -path /data/his
            var apps = all_apps.Split(',');
            if (apps.Length == 0) throw new ArgumentException("not set event app names");
            var data = new string[5];
            data[0] = "--loadevents";
            data[1] = "-app";
            var s_apps = "";
            for (var i = 0; i < apps.Length; i++)
            {
                s_apps += apps[i];
                if (i != apps.Length - 1)
                {
                    s_apps += ',';
                }
            }
            data[2] = s_apps;
            data[3] = "-path";
            data[4] = fileDir;
            builder.AppendArgs(data);
            return builder;
        }
        public static SoaServiceContainerBuilder UseDirectEventBus(this SoaServiceContainerBuilder builder, int cacheLength = 0)
        {
            builder.Services.AddSingleton<IDirectEventBus, DirectEventBus>(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var conn = source.Get("") ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var logger = sp.GetService<ILogger<DirectEventBus>>();
                var eventbus = new DirectEventBus(logger, sp, conn, cacheCount: cacheLength);
                return eventbus;
            });
            return builder;
        }
        public static SoaServiceContainerBuilder UseTopicEventBus(this SoaServiceContainerBuilder builder, int cacheLength = 0)
        {
            builder.Services.AddSingleton<ITopicEventBus, TopicEventBusMQ>(sp =>
            {
                var source = sp.GetService<DefaultMqConnectSource>();
                var conn = source.Get("") ?? throw new ArgumentNullException(nameof(IRabbitMQPersistentConnection));
                var logger = sp.GetService<ILogger<ITopicEventBus>>();
                var eventbus = new TopicEventBusMQ(logger, sp, conn, cacheCount: cacheLength);
                return eventbus;
            });
            return builder;
        }
    }
}
