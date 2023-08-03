// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections.Generic;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTestHelpers.RemoteServiceFixtures;
using NewRelic.Testing.Assertions;

namespace NewRelic.Agent.IntegrationTests.RemoteServiceFixtures
{
    public class AspNetCoreDistTraceRequestChainFixture : RemoteApplicationFixture
    {
        private const string ApplicationDirectoryName = @"AspNetCoreDistTracingApplication";
        private const string ExecutableName = ApplicationDirectoryName + ".exe";

        public RemoteService FirstCallApplication { get; set; }
        public RemoteService SecondCallApplication { get; set; }

        public AspNetCoreDistTraceRequestChainFixture()
            : base(new RemoteService(ApplicationDirectoryName, ExecutableName, "net7.0", ApplicationType.Bounded, true, true, true))
        {
            Actions(setupConfiguration: () =>
            {
                var configModifier = new NewRelicConfigModifier(DestinationNewRelicConfigFilePath);
                configModifier.SetOrDeleteDistributedTraceEnabled(true);
                configModifier.SetOrDeleteSpanEventsEnabled(true);
                configModifier.SetLogLevel("all");

                //Do during setup so TestLogger is set.
                FirstCallApplication = SetupDistributedTracingApplication();
                SecondCallApplication = SetupDistributedTracingApplication();

                var environmentVariables = new Dictionary<string, string>();

                FirstCallApplication.Start(string.Empty, environmentVariables, captureStandardOutput: true);
                SecondCallApplication.Start(string.Empty, environmentVariables, captureStandardOutput: true);
            });
        }

        public void ExecuteTraceRequestChain(string firstAppAction, string secondAppAction, string thirdAppAction, IEnumerable<KeyValuePair<string, string>> headers = null)
        {
            var firstCallBaseUrl = $"http://localhost:{FirstCallApplication.Port}/FirstCall";
            var secondCallBaseUrl = $"http://localhost:{SecondCallApplication.Port}/SecondCall";
            var lastCallBaseUrl = $"http://localhost:{RemoteApplication.Port}/LastCall";

            var lastCallUrl = $"{lastCallBaseUrl}/{thirdAppAction}";
            var secondCallUrl = $"{secondCallBaseUrl}/{secondAppAction}?nextUrl={lastCallUrl}";
            var firstCallUrl = $"{firstCallBaseUrl}/{firstAppAction}?nextUrl={secondCallUrl}";

            TestLogger?.WriteLine($"[{nameof(AspNetCoreDistTraceRequestChainFixture)}]: Starting A -> B -> C request chain with URL: {firstCallUrl}");

            if (thirdAppAction.IsEqualTo("CallError"))
            {
                GetStringAndAssertContains(firstCallUrl, "Exception occurred in ");
            }
            else
            {
                GetStringAndAssertEqual(firstCallUrl, "Worked", headers);
            }
        }

        protected RemoteService SetupDistributedTracingApplication()
        {
            var service = new RemoteService(ApplicationDirectoryName, ExecutableName, "net7.0", ApplicationType.Bounded, true, true, true);
            service.TestLogger = TestLogger;
            service.DeleteWorkingSpace();
            service.CopyToRemote();

            SetSecrets(service.DestinationNewRelicConfigFilePath);

            var configModifier = new NewRelicConfigModifier(service.DestinationNewRelicConfigFilePath);
            configModifier.SetOrDeleteDistributedTraceEnabled(true);
            configModifier.SetOrDeleteSpanEventsEnabled(true);
            configModifier.SetLogLevel("all");

            return service;
        }

        public override void Initialize()
        {
            base.Initialize();

        }

        private void WriteApplicationAgentLogToTestLogger(string applicationName, RemoteService application)
        {
            TestLogger?.WriteLine("");
            TestLogger?.WriteLine($"===== Begin {applicationName} log file =====");

            try
            {
                TestLogger?.WriteLine(application.AgentLog.GetFullLogAsString());
            }
            catch (Exception)
            {
                TestLogger?.WriteLine($"No log file found for {applicationName}.");
            }

            TestLogger?.WriteLine($"----- End of {applicationName} log file -----");
        }

        public override void ShutdownRemoteApplication()
        {
            FirstCallApplication.Shutdown();
            WriteApplicationAgentLogToTestLogger($"{FirstCallApplication.AppName}:{FirstCallApplication.Id}", FirstCallApplication);
            //FirstCallApplication.CapturedOutput?.WriteProcessOutputToLog($"{nameof(FirstCallApplication)} application:");

            SecondCallApplication.Shutdown();
            WriteApplicationAgentLogToTestLogger($"{SecondCallApplication.AppName}:{SecondCallApplication.Id}", SecondCallApplication);
            //SecondCallApplication.CapturedOutput?.WriteProcessOutputToLog($"{nameof(SecondCallApplication)} application:");

            base.ShutdownRemoteApplication();
        }

        public override void Dispose()
        {
            FirstCallApplication.Dispose();
            SecondCallApplication.Dispose();

            base.Dispose();
        }
    }
}
