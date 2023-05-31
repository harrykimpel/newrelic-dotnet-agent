// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System.Net.Http.Headers;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTestHelpers.Models;
using NewRelic.Testing.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.IntegrationTests.CatInbound
{
    [NetFrameworkTest]
    public class CatEnabledWithUntrustedAccountId : NewRelicIntegrationTest<RemoteServiceFixtures.BasicMvcApplicationTestFixture>
    {
        private readonly RemoteServiceFixtures.BasicMvcApplicationTestFixture _fixture;

        private HttpResponseHeaders _responseHeaders;

        public CatEnabledWithUntrustedAccountId(RemoteServiceFixtures.BasicMvcApplicationTestFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _fixture = fixture;
            _fixture.SetLogger(output);
            _fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configPath = fixture.DestinationNewRelicConfigFilePath;
                    var configModifier = new NewRelicConfigModifier(configPath);

                    configModifier.ForceTransactionTraces();
                    configModifier.EnableCat();
                },
                exerciseApplication: () =>
                {
                    _fixture.GetIgnored();
                    _responseHeaders = _fixture.GetWithUntrustedCatHeader();
                }
            );
            _fixture.Initialize();
        }

        [Fact]
        [Trait("feature", "CAT-DistributedTracing")]
        public void Test()
        {
            var transactionSample = _fixture.AgentLog.TryGetTransactionSample("WebTransaction/MVC/DefaultController/Index");
            var transactionEvent = _fixture.AgentLog.TryGetTransactionEvent("WebTransaction/MVC/DefaultController/Index");
            var metrics = _fixture.AgentLog.GetMetrics();

            NrAssert.Multiple
            (
                () => Assert.NotNull(transactionSample),
                () => Assert.NotNull(transactionEvent)
            );

            NrAssert.Multiple
            (
                () => Assert.False(_responseHeaders.Contains(@"X-NewRelic-App-Data")),

                // Trace attributes
                () => Assertions.TransactionTraceDoesNotHaveAttributes(Expectations.UnexpectedTransactionTraceIntrinsicAttributesCatDisabled, TransactionTraceAttributeType.Intrinsic, transactionSample),

                // Event attributes
                () => Assertions.TransactionEventDoesNotHaveAttributes(Expectations.UnexpectedTransactionEventIntrinsicAttributesCatDisabled, TransactionEventAttributeType.Intrinsic, transactionEvent),

                () => Assertions.MetricsDoNotExist(Expectations.UnexpectedMetricsCatDisabled, metrics)
            );
        }
    }
}
