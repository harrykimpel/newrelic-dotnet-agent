// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using NewRelic.Core.Logging;

namespace NewRelic.SystemInterfaces
{
    public class Environment : IEnvironment
    {
        public const string LegacyEnvVarPrefix = "NEWRELIC_";
        public const string DefaultEnvVarPrefix = "NEW_RELIC_";

        private Func<string, string> _getEnvVar = System.Environment.GetEnvironmentVariable;

        public Environment(Func<string,string> getEnvironmentVariable)
        {
            _getEnvVar = getEnvironmentVariable;
        }

        public string[] GetCommandLineArgs()
        {
            return System.Environment.GetCommandLineArgs();
        }

        public string GetEnvironmentVariable(string variable)
        {
            // The environment variables used to configure the agent should start with "NEW_RELIC_"
            // There are a few older variables (e.g. "NEWRELIC_LOG_LEVEL") that start with "NEWRELIC_"
            // We want to standardize on "NEW_RELIC_" but we also want to provide a soft landing for our customers,
            // so we will look for both spellings, and in the unlikely case that both exist, prefer the "NEW_RELIC_" one.
            // Log a warning if the legacy spelling is used.

            if (variable.StartsWith(LegacyEnvVarPrefix))
            {
                var legacyValue = _getEnvVar(variable);
                var baseName = variable.Substring(LegacyEnvVarPrefix.Length);
                var defaultName = DefaultEnvVarPrefix + baseName;
                var defaultValue = _getEnvVar(defaultName);
                if (defaultValue != null)
                {
                    return defaultValue;
                }
                else if (legacyValue != null)
                {
                    Log.Warn($"Found agent config environment variable {variable}. Please update to use {defaultName} instead. {variable} will be removed in a future major version.");
                    return legacyValue;
                }
                return null;
            }
            return _getEnvVar(variable);
        }

        public string GetEnvironmentVariable(string variable, EnvironmentVariableTarget environmentVariableTarget)
        {
            return System.Environment.GetEnvironmentVariable(variable, environmentVariableTarget);
        }

        public Dictionary<string, string> GetEnvironmentVariablesWithPrefix(string prefix)
        {
            var environmentVariables = System.Environment.GetEnvironmentVariables();

            Dictionary<string, string> result = null;

            foreach (DictionaryEntry entry in environmentVariables)
            {
                var key = entry.Key.ToString();
                if (key.StartsWith(prefix))
                {
                    if (result == null)
                    {
                        result = new Dictionary<string, string>();
                    }

                    result.Add(key.ToString(), entry.Value.ToString());
                }
            }
            return result;
        }
    }
}
