﻿/*
* Copyright 2020 New Relic Corporation. All rights reserved.
* SPDX-License-Identifier: Apache-2.0
*/
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTestHelpers.Models;
using NewRelic.Agent.IntegrationTests.CatInbound;
using NewRelic.Testing.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.IntegrationTests.CatOutbound
{
    public class CatDisabledChainedRequests : IClassFixture<RemoteServiceFixtures.BasicMvcApplication>
    {
        private RemoteServiceFixtures.BasicMvcApplication _fixture;

        private HttpResponseHeaders _responseHeaders;

        public CatDisabledChainedRequests(RemoteServiceFixtures.BasicMvcApplication fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.TestLogger = output;
            _fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configPath = fixture.DestinationNewRelicConfigFilePath;
                    var configModifier = new NewRelicConfigModifier(configPath);

                    configModifier.ForceTransactionTraces();

                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(_fixture.DestinationNewRelicConfigFilePath, new[] { "configuration" }, "crossApplicationTracingEnabled", "false");
                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(_fixture.DestinationNewRelicConfigFilePath, new[] { "configuration", "crossApplicationTracer" }, "enabled", "false");
                },
                exerciseApplication: () =>
                {
                    _fixture.GetIgnored();
                    _responseHeaders = _fixture.GetWithCatHeaderChained(requestData: new CrossApplicationRequestData("guid", false, "tripId", "pathHash"));
                }
            );
            _fixture.Initialize();
        }

        [Fact]
        public void Test()
        {
            var metrics = _fixture.AgentLog.GetMetrics().ToList();
            var calleeTransactionEvent = _fixture.AgentLog.TryGetTransactionEvent("WebTransaction/MVC/DefaultController/Index");
            Assert.NotNull(calleeTransactionEvent);

            var callerTransactionTrace = _fixture.AgentLog.TryGetTransactionSample("WebTransaction/MVC/DefaultController/Chained");
            Assert.NotNull(callerTransactionTrace);

            // Note: we are checking the metrics that are generated by the *Caller* as a result of receiving a CAT response.
            var expectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric { metricName = @"External/all", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = @"External/allWeb", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $@"External/{_fixture.RemoteApplication.DestinationServerName}/all", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $@"External/{_fixture.RemoteApplication.DestinationServerName}/Stream/GET", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $@"External/{_fixture.RemoteApplication.DestinationServerName}/Stream/GET", metricScope = @"WebTransaction/MVC/DefaultController/Chained", callCount = 1 }
            };

            var expectedCrossProcessId = $"{_fixture.AgentLog.GetAccountId()}#{_fixture.AgentLog.GetApplicationId()}";
            var unexpectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric { metricName = $@"ExternalApp/{_fixture.RemoteApplication.DestinationServerName}/{expectedCrossProcessId}/all", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $@"ExternalTransaction/{_fixture.RemoteApplication.DestinationServerName}/{expectedCrossProcessId}/WebTransaction/MVC/DefaultController/Index" },
                new Assertions.ExpectedMetric { metricName = $@"ExternalTransaction/{_fixture.RemoteApplication.DestinationServerName}/{expectedCrossProcessId}/WebTransaction/MVC/DefaultController/Index", metricScope = @"WebTransaction/MVC/DefaultController/Chained" },
                new Assertions.ExpectedMetric { metricName = @"ClientApplication/[^/]+/all", IsRegexName = true }
            };
            var expectedCallerTraceSegmentRegexes = new List<string>
            {
                "External/[^/]+/Stream/GET"
            };

            NrAssert.Multiple
            (
                () => Assert.False(_responseHeaders.Contains(@"X-NewRelic-App-Data")),

                () => Assertions.MetricsExist(expectedMetrics, metrics),
                () => Assertions.MetricsDoNotExist(unexpectedMetrics, metrics),

                // Note: It is difficult (perhaps impossible) to ensure that a transaction trace is generate for the chained request. This is because only one transaction trace is collected per harvest, and the chained requests will both be eligible.

                // calleeTransactionEvent attributes
                () => Assertions.TransactionEventDoesNotHaveAttributes(Expectations.UnexpectedTransactionTraceIntrinsicAttributesCatDisabled, TransactionEventAttributeType.Intrinsic, calleeTransactionEvent),

                // callerTransactionTrace segments
                () => Assertions.TransactionTraceSegmentsExist(expectedCallerTraceSegmentRegexes, callerTransactionTrace, true)
            );
        }
    }
}