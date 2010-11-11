﻿using System;
using Newtonsoft.Json.Linq;
using Raven.Munin;
using System.Linq;
using RavenMQ.Data;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class MessagesStorageActions
    {
        private readonly Table messages;
        private readonly QueuesStorageActions queuesStorageActions;
        private readonly IUuidGenerator uuidGenerator;

        public MessagesStorageActions(Table messages, QueuesStorageActions queuesStorageActions, IUuidGenerator uuidGenerator)
        {
            this.messages = messages;
            this.queuesStorageActions = queuesStorageActions;
            this.uuidGenerator = uuidGenerator;
        }

        public Guid Enqueue(string queue, DateTime expiry, byte[] data)
        {
            var msgId = uuidGenerator.CreateSequentialUuid();
            messages.Put(new JObject
            {
                {"MsgId", msgId.ToByteArray()},
                {"Queue", queue},
                {"Expiry", expiry}
            }, data);
            return msgId;
        }

        public OutgoingMessage Dequeue(string queue, Guid after)
        {
            var key = new JObject
            {
                { "Queue", queue },
                { "MsgId", after.ToByteArray() }
            };
            var result = messages["ByQueueNameAndMsgId"].SkipAfter(key).FirstOrDefault();
            if (result == null)
                return null;

            var readResult = messages.Read(result);

            return new OutgoingMessage
            {
                Id = new Guid(readResult.Key.Value<byte[]>("MsgId")),
                Queue = readResult.Key.Value<string>("Queue"),
                Data = readResult.Data(),
                Expiry = readResult.Key.Value<DateTime>("Expiry")
            };
        }

        public void ConsumeMessage(Guid msgId, Func<DateTime, string, bool> shouldConsumeMessage)
        {
            var key = new JObject { { "MsgId", msgId.ToByteArray() } };
            var readResult = messages.Read(key);
            if (readResult == null)
                return;

            var queue = readResult.Key.Value<string>("Queue");
            var epxiry = readResult.Key.Value<DateTime>("Expiry");
            if (shouldConsumeMessage(epxiry, queue) == false)
                return;
            messages.Remove(key);
            queuesStorageActions.DecrementMessageCount(queue);
        }

    }
}