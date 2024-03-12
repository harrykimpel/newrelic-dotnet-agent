// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NewRelic.Agent.Core.Config;
using NUnit.Framework;

namespace NewRelic.Agent.Core.Config
{
    [TestFixture]
    [TestOf(typeof(configuration))]
    public class ServerlessModeConfigurationTests
    {
        private Func<string, string> _originalGetEnvironmentVar;
        private Dictionary<string, string> _envVars = new Dictionary<string, string>();

        private void SetEnvironmentVar(string name, string value)
        {
            _envVars[name] = value;
        }

        private void ClearEnvironmentVars() => _envVars.Clear();

        private string MockGetEnvironmentVar(string name)
        {
            if (_envVars.TryGetValue(name, out var value)) return value;
            return null;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _originalGetEnvironmentVar = ConfigurationLoader.GetEnvironmentVar;
            ConfigurationLoader.GetEnvironmentVar = MockGetEnvironmentVar;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ConfigurationLoader.GetEnvironmentVar = _originalGetEnvironmentVar;
        }

        [SetUp]
        public void Setup()
        {
            ClearEnvironmentVars();
        }


        [Test]
        public void ServerlessModeEnabled_When_ServerlessEnvVarSet_LambdaFuncEnvVarNotSet_ConfigHasNoSetting()
        {
            // Arrange
            SetEnvironmentVar("NEW_RELIC_SERVERLESS_MODE_ENABLED", "true");

            var xml =
                "<configuration xmlns=\"urn:newrelic-config\"  >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ServerlessModeEnabled_When_ServerlessEnvVarNotSet_LambdaFuncEnvVarSet_ConfigHasNoSetting()
        {
            // Arrange
            SetEnvironmentVar("AWS_LAMBDA_FUNCTION_NAME", "myFunc");

            var xml =
                "<configuration xmlns=\"urn:newrelic-config\"  >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ServerlessModeNotEnabled_When_ServerlessEnvVarNotSet_LambdaFuncEnvVarNotSet_ConfigHasNoSetting()
        {
            // Arrange
            var xml =
                "<configuration xmlns=\"urn:newrelic-config\"  >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ServerlessModeNotEnabled_When_ServerlessEnvVarSetToFalse_ConfigHasNoSetting()
        {
            // Arrange
            SetEnvironmentVar("NEW_RELIC_SERVERLESS_MODE_ENABLED", "false");

            var xml =
                "<configuration xmlns=\"urn:newrelic-config\"  >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ServerlessModeEnvVar_TakesPrecedenceOver_ConfigSetting()
        {
            // Arrange
            SetEnvironmentVar("NEW_RELIC_SERVERLESS_MODE_ENABLED", "false");
            var xml =
                "<configuration xmlns=\"urn:newrelic-config\" serverlessModeEnabled=\"true\"  >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ServerlessModeEnvVar_TakesPrecedenceOver_LambdaFunctionEnvVar()
        {
            // Arrange
            SetEnvironmentVar("NEW_RELIC_SERVERLESS_MODE_ENABLED", "false");
            SetEnvironmentVar("AWS_LAMBDA_FUNCTION_NAME", "myFunc");

            var xml =
                "<configuration xmlns=\"urn:newrelic-config\" >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ServerlessModeEnabled_WhenOnly_ConfigIsSet()
        {
            // Arrange

            var xml =
                "<configuration xmlns=\"urn:newrelic-config\" serverlessModeEnabled=\"true\" >" +
                "   <service licenseKey=\"dude\"/>" +
                "   <application>" +
                "       <name>Test</name>" +
                "   </application>" +
                "</configuration>";


            var configuration = CreateBootstrapConfiguration(xml);

            // Act
            var result = configuration.ServerlessModeEnabled;

            // Assert
            Assert.That(result, Is.False);
        }

        private BootstrapConfiguration CreateBootstrapConfiguration(string xml)
        {
            var configuration = new configuration();
            configuration.Initialize(xml, "");
            return new BootstrapConfiguration(configuration);
        }
    }
}
