﻿using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace YiDian.EventBus.MQ.DefaultConnection
{
    internal class DefaultRabbitMQPersistentConnection
       : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;
        private readonly int _retryCount;
        IConnection _connection;
        bool _disposed;
        readonly object sync_root = new object();

        public event EventHandler<string> ConnectFail;

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, string name, int retryCount, IEventBusSubManagerFactory fact)
        {
            Name = name;
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(IConnectionFactory));
            _logger = new ConsoleLog();
            _retryCount = retryCount;
            SubsFactory = fact;
        }
        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }
        public IEventBusSubManagerFactory SubsFactory { get; }

        public string Name { get; }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            lock (sync_root)
            {
                if (IsConnected) return true;
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning("TryConnect Failed ," + ex.Message);
                    }
                );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                    _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.LocalPort.ToString()} and is subscribed to failure events");
                    return true;
                }
                else
                {
                    _logger.LogWarning("FATAL ERROR: RabbitMQ connections could not be created and opened");
                    ConnectFail?.Invoke(this, "After several attempts RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
        }
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is shutdown.OnConnectionBlocked Trying to re-connect...");
            TryConnect();

        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception.OnCallbackException Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is on shutdown. OnConnectionShutdown Trying to re-connect...");
            TryConnect();
        }
    }
}
