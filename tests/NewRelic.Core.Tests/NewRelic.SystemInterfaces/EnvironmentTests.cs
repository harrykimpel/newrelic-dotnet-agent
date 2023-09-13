// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelicEnvironment = NewRelic.SystemInterfaces.Environment;
using NUnit.Framework;
using System.Collections.Generic;

namespace NewRelic.Core.Tests.NewRelic.SystemInterfaces
{
    [TestFixture]
    public class EnvironmentTests
    {
        private Dictionary<string, string> _mockEnvironment;
        private NewRelicEnvironment _newRelicEnvironment;

        [SetUp]
        public void Setup()
        {
            _mockEnvironment = new Dictionary<string, string>()
            {
                { "NEW_RELIC_LOG_LEVEL", "INFO" },
                { "NEWRELIC_LOG_LEVEL", "DEBUG" },
                { "NEWRELIC_HOME", "BigPink" },
                { "NEW_RELIC_LICENSE_KEY", "SuperSecret" }
            };
            _newRelicEnvironment = new NewRelicEnvironment(GetMockEnvVar);
        }


        [Test]
        public void GetUnsetEnvVar_ReturnsNull()
        {
            var value = _newRelicEnvironment.GetEnvironmentVariable("UNSET");
            Assert.IsNull(value);
        }

        [Test]
        public void GetEnvVarWithLegacyPrefix_DefaultPrefixUnset_ReturnsLegacyValue()
        {
            var legacyPrefixVariable = "NEWRELIC_HOME";
            var value = _newRelicEnvironment.GetEnvironmentVariable(legacyPrefixVariable);
            Assert.AreEqual(value, _mockEnvironment[legacyPrefixVariable]);
        }

        [Test]
        public void GetEnvVarWithLegacyPrefix_DefaultPrefixSet_ReturnsDefaultValue()
        {
            var legacyPrefixVariable = "NEWRELIC_LOG_LEVEL";
            var defaultPrefixVariable = "NEW_RELIC_LOG_LEVEL";
            var value = _newRelicEnvironment.GetEnvironmentVariable(legacyPrefixVariable);
            Assert.AreEqual(value, _mockEnvironment[defaultPrefixVariable]);
        }

        [Test]
        public void GetEnvVarWithDefaultPrefix_LegacyPrefixSet_ReturnsDefaultValue()
        {
            var defaultPrefixVariable = "NEW_RELIC_LOG_LEVEL";
            var value = _newRelicEnvironment.GetEnvironmentVariable(defaultPrefixVariable);
            Assert.AreEqual(value, _mockEnvironment[defaultPrefixVariable]);
        }

        [Test]
        public void GetEnvVarWithDefaultPrefix_LegacyPrefixUnset_ReturnsDefaultValue()
        {
            var defaultPrefixVariable = "NEW_RELIC_LICENSE_KEY";
            var value = _newRelicEnvironment.GetEnvironmentVariable(defaultPrefixVariable);
            Assert.AreEqual(value, _mockEnvironment[defaultPrefixVariable]);
        }

        // This should mimic the behavior of System.Environment.GetEnvironmentVariable
        // i.e. it returns null if the requested key doesn't exist in the mock environment
        private string GetMockEnvVar(string key)
        {
            if (_mockEnvironment.TryGetValue(key, out var value))
            {
                return value;
            }
            else return null;
        }
    }
}
