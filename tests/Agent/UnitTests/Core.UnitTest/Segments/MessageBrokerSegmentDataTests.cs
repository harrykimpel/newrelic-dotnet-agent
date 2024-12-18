// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using NewRelic.Agent.Core.Metrics;
using NewRelic.Agent.Core.Spans;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Data;
using NewRelic.Agent.Extensions.SystemExtensions.Collections.Generic;
using NewRelic.Testing.Assertions;
using NUnit.Framework;

namespace NewRelic.Agent.Core.Segments.Tests
{
    [TestFixture]
    public class MessageBrokerSegmentDataTests
    {
        #region IsCombinableWith

        [Test]
        public void IsCombinableWith_ReturnsTrue_ForIdenticalSegments()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, "messagingSystem", "cloudAccountId", "cloudRegion", "localhost", 1234, "routingKey");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, "messagingSystem", "cloudAccountId", "cloudRegion", "localhost", 1234, "routingKey");

            Assert.That(segment1.IsCombinableWith(segment2), Is.True);
        }


        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentCombinable()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, false);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfBothNotCombinable()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, false);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, false);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentHashCode()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 2), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentTypeName()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type2", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentMethodName()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method2", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentVendor()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor2", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentDestination()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueB", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentDestinationType()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Topic, MetricNames.MessageBrokerAction.Consume, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }


        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentAction()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true);
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Produce, true);

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentMessagingSystem()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, "messagingSystem1");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, "messagingSystem2");

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentCloudAccountId()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, cloudAccountId:"cloudAccountId1");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, cloudAccountId:"cloudAccountId2");

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentCloudRegion()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, cloudRegion:"cloudRegion1");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, cloudRegion:"cloudRegion2");

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentServerAddress()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "localhost", serverPort: 1234, routingKey: "routingKey");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "remotehost", serverPort: 1234, routingKey: "routingKey");

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsFalse_IfDifferentServerPort()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "localhost", serverPort: 1234, routingKey: "routingKey");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "localhost", serverPort: 5678, routingKey: "routingKey");

            Assert.That(segment1.IsCombinableWith(segment2), Is.False);
        }

        [Test]
        public void IsCombinableWith_ReturnsTrue_IfDifferentRoutingKey()
        {
            var segment1 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "localhost", serverPort: 1234, routingKey: "routingKey1");
            var segment2 = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(new TimeSpan(), TimeSpan.FromSeconds(2), 2, 1, new MethodCallData("type", "method", 1), Enumerable.Empty<KeyValuePair<string, object>>(), "vendor1", "queueA", MetricNames.MessageBrokerDestinationType.Queue, MetricNames.MessageBrokerAction.Consume, true, serverAddress: "localhost", serverPort: 1234, routingKey: "routingKey2");

            Assert.That(segment1.IsCombinableWith(segment2), Is.True);
        }

        #endregion IsCombinableWith

        #region CreateSimilar

        [Test]
        public void CreateSimilar_ReturnsCorrectValues()
        {
            var oldStartTime = DateTime.Now;
            var oldDuration = TimeSpan.FromSeconds(2);
            var oldParameters = new Dictionary<string, object> { { "flim", "flam" } };
            var oldSegment = MessageBrokerSegmentDataTestHelpers.CreateMessageBrokerSegmentBuilder(
                new TimeSpan(),
                oldDuration,
                2,
                1,
                new MethodCallData("type", "method", 1),
                oldParameters,
                "vendor1",
                "queueA",
                MetricNames.MessageBrokerDestinationType.Queue,
                MetricNames.MessageBrokerAction.Consume,
                true);

            var newStartTime = TimeSpan.FromSeconds(5);
            var newDuration = TimeSpan.FromSeconds(5);
            var newParameters = new Dictionary<string, object> { { "foo", "bar" }, { "zip", "zap" } };
            var newSegment = oldSegment.CreateSimilar(newStartTime, newDuration, newParameters);

            var segmentData = newSegment.Data as MessageBrokerSegmentData;
            Assert.That(segmentData, Is.Not.Null);

            NrAssert.Multiple(
                () => Assert.That(newSegment.RelativeStartTime, Is.EqualTo(newStartTime)),
                () => Assert.That(newSegment.Duration, Is.EqualTo(newDuration)),
                () => Assert.That(newSegment.MethodCallData.TypeName, Is.EqualTo("type")),
                () => Assert.That(newSegment.MethodCallData.MethodName, Is.EqualTo("method")),
                () => Assert.That(newSegment.MethodCallData.InvocationTargetHashCode, Is.EqualTo(1)),
                () => Assert.That(segmentData.DestinationType, Is.EqualTo(MetricNames.MessageBrokerDestinationType.Queue)),
                () => Assert.That(segmentData.Action, Is.EqualTo(MetricNames.MessageBrokerAction.Consume)),
                () => Assert.That(segmentData.Vendor, Is.EqualTo("vendor1")),
                () => Assert.That(segmentData.Destination, Is.EqualTo("queueA")),
                () => Assert.That(newSegment.Parameters.Count(), Is.EqualTo(2)),
                () => Assert.That(newSegment.Parameters.ToDictionary()["foo"], Is.EqualTo("bar")),
                () => Assert.That(newSegment.Parameters.ToDictionary()["zip"], Is.EqualTo("zap")),
                () => Assert.That(newSegment.Combinable, Is.EqualTo(true))
                );
        }

        #endregion CreateSimilar
    }

    public static class MessageBrokerSegmentDataTestHelpers
    {
        public static Segment CreateMessageBrokerSegmentBuilder(
            TimeSpan start,
            TimeSpan duration,
            int uniqueId,
            int? parentId,
            MethodCallData methodCallData,
            IEnumerable<KeyValuePair<string, object>> parameters,
            string vendor,
            string destination,
            MetricNames.MessageBrokerDestinationType destinationType,
            MetricNames.MessageBrokerAction action,
            bool combinable,
            string messagingSystemName = null,
            string cloudAccountId = null,
            string cloudRegion = null,
            string serverAddress = null,
            int? serverPort = null,
            string routingKey = null)
        {
            var segment = new Segment(SimpleSegmentDataTestHelpers.CreateTransactionSegmentState(uniqueId, parentId), methodCallData);
            var messageBrokerSegmentData = new MessageBrokerSegmentData(
                vendor,
                destination,
                destinationType,
                action,
                messagingSystemName,
                cloudAccountId,
                cloudRegion,
                serverAddress,
                serverPort,
                routingKey);
            segment.SetSegmentData(messageBrokerSegmentData);
            segment.Combinable = combinable;

            return new Segment(start, duration, segment, parameters);
        }
    }
}
