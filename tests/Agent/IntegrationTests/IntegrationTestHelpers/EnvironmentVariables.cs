// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;

namespace NewRelic.Agent.IntegrationTestHelpers
{
    public static class EnvironmentVariables
    {
        private const string LegacyEnvVarPrefix = "NEWRELIC_";
        private const string DefaultEnvVarPrefix = "NEW_RELIC_";

        public static readonly string DestinationWorkingDirectoryRemotePath = Environment.GetEnvironmentVariable("INTEGRATION_TEST_WORKING_DIRECTORY_DESTINATION");

        public static void RemoveEnvVarFromProcess(string envVarName, ProcessStartInfo startInfo)
        {
            if (envVarName.StartsWith(LegacyEnvVarPrefix))
            {
                var baseName = envVarName.Substring(LegacyEnvVarPrefix.Length);
                var defaultName = DefaultEnvVarPrefix + baseName;
                startInfo.EnvironmentVariables.Remove(defaultName);
            }

            startInfo.EnvironmentVariables.Remove(envVarName);
        }

    }
}
