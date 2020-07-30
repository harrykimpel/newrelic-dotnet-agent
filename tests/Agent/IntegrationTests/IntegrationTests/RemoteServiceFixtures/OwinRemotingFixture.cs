﻿/*
* Copyright 2020 New Relic Corporation. All rights reserved.
* SPDX-License-Identifier: Apache-2.0
*/
using System.Net;
using NewRelic.Agent.IntegrationTestHelpers.RemoteServiceFixtures;

namespace NewRelic.Agent.IntegrationTests.RemoteServiceFixtures
{
    public class OwinRemotingFixture : RemoteApplicationFixture
    {
        private const string ServerApplicationDirectoryName = @"OwinRemotingServer";
        private const string ServerExecutableName = @"OwinRemotingServer.exe";
        private const string ClientApplicationDirectoryName = @"OwinRemotingClient";
        private const string ClientExecutableName = @"OwinRemotingClient.exe";
        internal RemoteService OwinRemotingServerApplication { get; set; }

        public OwinRemotingFixture() : base(new RemoteService(ClientApplicationDirectoryName, ClientExecutableName, ApplicationType.Bounded, createsPidFile: false))
        {
            OwinRemotingServerApplication = new RemoteService(ServerApplicationDirectoryName, ServerExecutableName, ApplicationType.Bounded, createsPidFile: false);
            OwinRemotingServerApplication.CopyToRemote();
            OwinRemotingServerApplication.Start(string.Empty, captureStandardOutput: false, doProfile: false);
        }

        public string GetObjectTcp()
        {
            var address = string.Format(@"http://{0}:{1}/Remote/GetObjectTcp", DestinationServerName, Port);
            var webClient = new WebClient();
            var result = webClient.DownloadString(address);
            return result;
        }

        public string GetObjectHttp()
        {
            var address = string.Format(@"http://{0}:{1}/Remote/GetObjectHttp", DestinationServerName, Port);
            var webClient = new WebClient();
            var result = webClient.DownloadString(address);
            return result;
        }

        public override void Dispose()
        {
            OwinRemotingServerApplication.Shutdown();
            OwinRemotingServerApplication.Dispose();
            base.Dispose();
        }
    }
}