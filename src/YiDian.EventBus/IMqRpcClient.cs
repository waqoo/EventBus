﻿using System;

namespace YiDian.EventBus
{
    public interface IMQRpcClient
    {
        string ServerId { get; }
        IEventSeralize Seralize { get; }
        ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data) where Tin : IMQEvent where TOut : IMQEvent;
        ResponseBase<T> Call<T>(string uri) where T : IMQEvent;
        void Cancel(long mid);
        bool IsConnect { get; set; }
        event EventHandler ConnectionError;
    }
    public interface IRpcServer
    {
        IEventSeralize Seralize { get; }
        string ServerId { get; }
    }
}
