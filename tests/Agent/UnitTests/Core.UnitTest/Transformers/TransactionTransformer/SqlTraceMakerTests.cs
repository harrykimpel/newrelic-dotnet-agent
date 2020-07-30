﻿/*
* Copyright 2020 New Relic Corporation. All rights reserved.
* SPDX-License-Identifier: Apache-2.0
*/
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Database;
using NewRelic.Agent.Core.Errors;
using NewRelic.Agent.Core.Transactions;
using NewRelic.Agent.Core.Transactions.TransactionNames;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Builders;
using NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Data;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NUnit.Framework;
using Telerik.JustMock;

namespace NewRelic.Agent.Core.Transformers.TransactionTransformer
{
    [TestFixture]
    public class SqlTraceMakerTests
    {
        private IDatabaseService _databaseService;
        private IConfigurationService _configurationService;
        private SqlTraceMaker _sqlTraceMaker;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Mock.Create<IDatabaseService>();
            Mock.Arrange(() => _databaseService.SqlObfuscator.GetObfuscatedSql(Arg.AnyString, Arg.IsAny<DatastoreVendor>())).Returns((string sql) => sql);
            _configurationService = Mock.Create<IConfigurationService>();
            Mock.Arrange(() => _configurationService.Configuration.InstanceReportingEnabled).Returns(true);
            Mock.Arrange(() => _configurationService.Configuration.DatabaseNameReportingEnabled).Returns(true);
            _sqlTraceMaker = new SqlTraceMaker(_configurationService);
        }

        [Test]
        public void TryGetSqlTrace_ReturnsTrace()
        {
            var uri = "sqlTrace/Uri";
            var commandText = "Select * from Table1";
            var duration = TimeSpan.FromMilliseconds(500);
            var transaction = BuildTestTransaction(uri);
            var transactionMetricName = new TransactionMetricName("WebTransaction", "Name");
            var datastoreSegment = BuildSegment(DatastoreVendor.MSSQL, "Table1", commandText, new TimeSpan(), duration, null, null, null, "myhost", "myport", "mydatabase");

            var sqlTrace = _sqlTraceMaker.TryGetSqlTrace(transaction, transactionMetricName, datastoreSegment);
            Assert.IsNotNull(sqlTrace);
            Assert.AreEqual(commandText, sqlTrace.Sql);
            Assert.AreEqual(uri, sqlTrace.Uri);
            Assert.AreEqual(duration, sqlTrace.TotalCallTime);
            Assert.AreEqual(3, sqlTrace.ParameterData.Count); // Explain plans will go here
            Assert.AreEqual("myhost", sqlTrace.ParameterData["host"]);
            Assert.AreEqual("myport", sqlTrace.ParameterData["port_path_or_id"]);
            Assert.AreEqual("mydatabase", sqlTrace.ParameterData["database_name"]);
            Assert.AreEqual("WebTransaction/Name", sqlTrace.TransactionName);
        }

        [Test]
        public void TryGetSqlTrace_ReturnsNullWhenDurationIsNull()
        {

            var uri = "sqlTrace/Uri";
            var commandText = "Select * from Table1";
            var transaction = BuildTestTransaction(uri);
            var transactionMetricName = new TransactionMetricName("WebTransaction", "Name");
            var datastoreSegment = BuildSegment(DatastoreVendor.MSSQL, "Table1", commandText, new TimeSpan(), null);

            var sqlTrace = _sqlTraceMaker.TryGetSqlTrace(transaction, transactionMetricName, datastoreSegment);
            Assert.IsNull(sqlTrace);
        }
        private static ImmutableTransaction BuildTestTransaction(string uri = null, string guid = null, int? statusCode = null, int? subStatusCode = null, IEnumerable<ErrorData> transactionExceptionDatas = null)
        {
            var txMetadata = new TransactionMetadata();
            if (uri != null)
                txMetadata.SetUri(uri);
            if (statusCode != null)
                txMetadata.SetHttpResponseStatusCode(statusCode.Value, subStatusCode);
            if (transactionExceptionDatas != null)
                transactionExceptionDatas.ForEach(data => txMetadata.AddExceptionData(data));

            var name = new WebTransactionName("foo", "bar");
            var segments = Enumerable.Empty<Segment>();
            var immutableMetadata = txMetadata.ConvertToImmutableMetadata();
            guid = guid ?? Guid.NewGuid().ToString();

            return new ImmutableTransaction(name, segments, immutableMetadata, DateTime.UtcNow, TimeSpan.FromSeconds(1), guid, false, false, false, SqlObfuscator.GetObfuscatingSqlObfuscator());
        }
        private static TypedSegment<DatastoreSegmentData> BuildSegment(DatastoreVendor vendor, string model, string commandText, TimeSpan startTime = new TimeSpan(), TimeSpan? duration = null, string name = "", MethodCallData methodCallData = null, IEnumerable<KeyValuePair<string, object>> parameters = null, string host = null, string portPathOrId = null, string databaseName = null)
        {
            var data = new DatastoreSegmentData()
            {
                DatastoreVendorName = vendor,
                Model = model,
                CommandText = commandText,
                Host = host,
                PortPathOrId = portPathOrId,
                DatabaseName = databaseName
            };
            methodCallData = methodCallData ?? new MethodCallData("typeName", "methodName", 1);
            return new TypedSegment<DatastoreSegmentData>(startTime, duration,
                new TypedSegment<DatastoreSegmentData>(Mock.Create<ITransactionSegmentState>(), methodCallData, data, false));
        }
    }
}