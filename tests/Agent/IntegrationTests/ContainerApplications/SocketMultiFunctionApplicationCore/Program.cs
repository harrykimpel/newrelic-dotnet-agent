// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using MultiFunctionApplicationHelpers;

//CreatePidFile();

MultiFunctionApplication.EnableSocketListener = true;
MultiFunctionApplication.Execute(args);

return;

//static void CreatePidFile()
//{
//    var pidFileNameAndPath = Path.Combine(Environment.GetEnvironmentVariable("NEWRELIC_LOG_DIRECTORY"), "containerizedapp.pid");
//    var pid = Environment.ProcessId;
//    using var file = File.CreateText(pidFileNameAndPath);
//    file.WriteLine(pid);
//}
