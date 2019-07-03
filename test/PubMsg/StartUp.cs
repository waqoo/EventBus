﻿using Autofac;
using EventModels.MyTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils.Seralize;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }
        public StartUp(IConfiguration config)
        {
            Configuration = config;
        }
        public void ConfigService(SoaServiceContainerBuilder soa, ContainerBuilder builder)
        {
            //soa.UseRabbitMq(Configuration["mqconnstr"], new JsonSeralizer())
            //     .UseDirectEventBus()
            //     .UseTopicEventBus();
        }
        public void Start(IServiceProvider sp, string[] args)
        {
            MqA xa = new MqA()
            {
                PropertyA = "a",
                PropertyB = "a",
                PropertyLC = new string[2] { "a", "a" },
                Type = MqType.LS,
                PropertyD = new string[2] { "a", "a" },
                PropertyQB = new MqB() { D = new string[] { "a", "a" }, C = "a" },
                Date = DateTime.Now,
                Flag = false,
                QBS = new MqB[]
                {
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" },
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" },
                       new MqB(){ D = new string[] { "a", "a" }, C = "a" }
                }
            };

            var json222 = xa.ToJson();
            var l1 = Encoding.UTF8.GetBytes(json222).Length;
            var l2 = xa.Size();

            var size = xa.Size();
            var stream = new WriteStream(size);
            xa.ToBytes(stream);
            var datas = stream.GetBytes();
            var reads = new ReadStream(datas);
            MqA xb = new MqA();
            xb.BytesTo(reads);

            Console.ReadKey();
            GC.Collect(2);

            var count = 1000000;
            var watch = Stopwatch.StartNew();

            for (var xx = 0; xx < count; xx++)
            {
                var json = xa.ToJson();
                var bytes = Encoding.UTF8.GetBytes(json);
                var json2 = Encoding.UTF8.GetString(bytes);
                json2.JsonTo<MqA>();
            }
            watch.Stop();
            Console.WriteLine("json test:" + watch.ElapsedMilliseconds.ToString());
            GC.Collect(2);
            Console.ReadKey();
            watch.Restart();
            for (var xx = 0; xx < count; xx++)
            {
                size = xa.Size();
                stream = new WriteStream(size);
                xa.ToBytes(stream);
                datas = stream.GetBytes();
                reads = new ReadStream(datas);
                 xb = new MqA();
                xb.BytesTo(reads);
            }
            Console.WriteLine("mystream test:" + watch.ElapsedMilliseconds.ToString());
            GC.Collect(2);
            Console.ReadKey();

            var eventsMgr = sp.GetRequiredService<IAppEventsManager>();
            var res = eventsMgr.RegisterEvent<MqA>("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            res = eventsMgr.VaildityTest("pub_test", "1.2");
            if (!res.IsVaild) Console.WriteLine(res.InvaildMessage);
            var a = new MqA() { PropertyA = "a", PropertyB = "b2" };
            var b = new MqA() { PropertyA = "b", PropertyB = "b1" };
            var direct = sp.GetService<IDirectEventBus>();
            var topic = sp.GetService<ITopicEventBus>();
            var qps = sp.GetService<IQpsCounter>();
            var ps = int.Parse(Configuration["ps"]);
            var type = Configuration["type"];
            var sleep = int.Parse(Configuration["sleep"]);
            Task.Run(() =>
            {
                for (; ; )
                {
                    var i = ps;
                    for (var j = 0; j < i; j++)
                    {
                        //topic.PublishPrefix(a, "s1");
                        //topic.Publish(a);
                        //direct.Publish(b);
                        //direct.Publish(a);
                        //direct.Publish(b);
                        qps.Add("p");
                        if (type == "direct")
                        {
                            direct.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-where")
                        {
                            topic.Publish(a);
                            qps.Add("i");
                        }
                        else if (type == "top-pre")
                        {
                            topic.PublishPrefix(a, "s1");
                            qps.Add("i");
                        }
                    }
                    Thread.Sleep(sleep);
                }
            });
        }
    }
}
