﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace YiDian.EventBus.MQ
{
    internal class FanoutEventBus : EventBusBase<IFanoutEventBus, FanoutSubscriber>, IFanoutEventBus
    {
        readonly string brokerName = "amq.fanout";
        public FanoutEventBus(ILogger<IFanoutEventBus> logger, IServiceProvider autofac, IEventSeralize seralize, IRabbitMQPersistentConnection persistentConnection, int cacheCount = 100)
            : base(logger, autofac, seralize, persistentConnection, cacheCount)
        {

        }
        public FanoutEventBus(string brokerName, ILogger<IFanoutEventBus> logger, IServiceProvider autofac, IRabbitMQPersistentConnection persistentConnection, IEventSeralize seralize, int cacheCount = 100)
          : base(logger, autofac, seralize, persistentConnection, cacheCount)
        {
            this.brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName), "broker name can not be null");
            persistentConnection.TryConnect();
            var channel = persistentConnection.CreateModel();
            channel.ExchangeDeclare(brokerName, "fanout", false, true, null);
            channel.Dispose();
        }
        public override string BROKER_NAME => brokerName;

        public override bool Publish<T>(T @event, out ulong tag, bool enableTransaction = false)
        {
            return Publish(@event, "", out _, out tag, false);
        }

        public void RegisterConsumer(string queueName, Action<FanoutSubscriber> action, ushort fetchCount = 200, int queueLength = 100000, bool autodel = true, bool durable = false, bool autoAck = true, bool autoStart = true)
        {
            if (string.IsNullOrEmpty(queueName)) return;
            var config = consumerInfos.Find(x => x.Name == queueName);
            if (config != null) return;
            var scriber = new FanoutSubscriber(this, queueName);
            var submgr = GetSubscriber(queueName);
            config = new ConsumerConfig<IFanoutEventBus, FanoutSubscriber>(scriber, submgr)
            {
                AutoAck = autoAck,
                MaxLength = queueLength,
                Durable = durable,
                AutoDel = autodel,
                FetchCount = fetchCount,
                Name = queueName,
                SubAction = action
            };
            consumerInfos.Add(config);
            CreateConsumerChannel(config, autoStart);
        }

        public override void Subscribe<T, TH>(string queueName)
        {
            SubscribeInternal<T, TH>(queueName, "");
        }

        public override void Unsubscribe<T, TH>(string queueName)
        {
            UnsubscribeInternal<T, TH>(queueName, "");
        }

        public void SubscribeBytes<TH>(string queueName) where TH : IBytesHandler
        {
            SubscribeBytesInternal<TH>(queueName, "");
        }
        public void UnsubscribeBytes<TH>(string queueName) where TH : IBytesHandler
        {
            UnsubscribeBytesInternal<TH>(queueName, "");
        }

        protected override IEnumerable<SubscriptionInfo> GetDymaicHandlers(IEventBusSubManager mgr, string key)
        {
            return mgr.GetDymaicHandlersBySubKey("*", BROKER_NAME);
        }
    }
}