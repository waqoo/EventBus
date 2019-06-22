﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus
{
    public interface IAppEventsManager
    {
        void RegisterEvents(AppMetas metas);
        void RegisterEvent<T>(string appName, string version) where T : IntegrationMQEvent;
        CheckResult VaildityTest(string appName, string version);
        AppMetas GetAppEventTypes(string appName, string version = "");
        string GetEventID<T>(string appName, string version);
    }
    public class CheckResult
    {
        public bool IsVaild { get; set; }
        public string InVaildMessage { get; set; }
    }
    public enum AttrType
    {
        None = 0,
        Index = 1
    }
    public class PropertyMetaInfo
    {
        public static readonly string P_Double = "double";
        public static readonly string P_Int32 = "int32";
        public static readonly string P_Int64 = "int64";
        public static readonly string P_UInt32 = "uint32";
        public static readonly string P_UInt64 = "uint64";
        public static readonly string P_String = "string";
        public static readonly string P_Boolean = "boolean";
        public static string[] MetaTypeValues = new string[] { P_Int32, P_Int64, P_UInt32, P_UInt64, P_String, P_Boolean, P_Double };

        public string Name { get; set; }
        public string Type { get; set; }
        public MetaAttr Attr { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }
    public class ClassMeta
    {
        public ClassMeta()
        {
            Properties = new List<PropertyMetaInfo>();
        }
        public MetaAttr Attr { get; set; }
        public string Name { get; set; }
        public List<PropertyMetaInfo> Properties { get; set; }
        public void ToJson(StringBuilder sb)
        {
            sb.Append("{\"Name\":\"");
            sb.Append(Name);
            sb.Append("\",\"Attr\":");
            Attr.ToJson(sb);
            sb.Append('[');
            foreach (var p in Properties)
            {
                p.ToJson(sb);
            }
            sb.Append("]}");
        }
    }
    public struct MetaAttr
    {
        public AttrType AttrType { get; set; }
        public string Value { get; set; }

        internal void ToJson(StringBuilder sb)
        {
            throw new NotImplementedException();
        }
    }
    public class AppMetas
    {
        public List<ClassMeta> MetaInfos { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
    }
}