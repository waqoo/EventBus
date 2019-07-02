﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YiDian.EventBus.MQ;
using YiDian.Soa.Sp;
using YiDian.Soa.Sp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        struct XA
        {
            public int IA { get; set; }
            public string Name { get; set; }
            public List<Tuple<string, string>> List { get; set; }
        }
        static void Main(string[] args)
        {
            var a = new byte[2] { 5, 1 };
            var b = new byte[2] { 2, 1 };

            var sa = BitConverter.ToInt16(a);
            var sb = BitConverter.ToInt16(b);

            var sa1 = BitConverter.ToUInt16(a);
            var sb2 = BitConverter.ToUInt16(b);
            //XA x = new XA
            //{
            //    List = new List<Tuple<string, string>>()
            //};
            //x.IA = 2;
            //x.List.Add(new Tuple<string, string>("zs", "ls"));
            //XA y = x;
            //y.IA = 3;
            //y.List.Add(new Tuple<string, string>("zs2", "ls2"));
            //Console.WriteLine();
            //List<Tuple<string, string>> list = new List<Tuple<string, string>>
            //{
            //    new Tuple<string, string>("zs", "ls"),
            //    new Tuple<string, string>("zs2", "ls2"),
            //    new Tuple<string, string>("zs3", "ls3"),
            //};
            //XA xa = new XA
            //{
            //    List = list,
            //    Name = "zs"
            //};
            //var arr = xa.List.ToJson();
            //var obj = JsonString.Unpack(arr);
            //Console.WriteLine();
            ServiceHost.CreateBuilder(args)
                 .ConfigApp(e => e.AddJsonFile("appsettings.json"))
                 .UserStartUp<StartUp>()
                 .Build()
                 .Run(e => e["sysname"]);

            //var task = WithTask();
            //var awaiter = task.GetAwaiter();
            //awaiter.UnsafeOnCompleted(() =>
            //{
            //    var f = task.IsCompletedSuccessfully;
            //    Console.WriteLine("2");
            //});
            //Console.WriteLine("Hello World!");
            //Console.ReadKey();
        }

        private static void WriteProperty(Type t)
        {
            foreach (var p in t.GetProperties())
            {
                Console.Write(p.Name);
                Console.Write(" ");
                Console.Write(p.PropertyType.Name);
                Console.WriteLine();
            }
        }

        static Task<int> WithTask()
        {
            return Task.Delay(1000).ContinueWith<int>(x =>
            {
                throw new ArgumentException();
            });
        }
    }
}
