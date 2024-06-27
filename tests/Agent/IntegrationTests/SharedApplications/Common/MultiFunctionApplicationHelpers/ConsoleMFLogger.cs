// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;

namespace MultiFunctionApplicationHelpers
{
    public static class ConsoleMFLogger
    {

        private static string LogTs => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff");

        private static int Tid => System.Threading.Thread.CurrentThread.ManagedThreadId;

        public static void Info()
        {
            Info("");
        }

        public static void Info(params string[] message)
        {
            foreach (var msg in message)
            {
                var logMsg = $"{LogTs} tid:{Tid} {msg}";

                if (MultiFunctionApplication.EnableSocketListener)
                    MultiFunctionApplication.SendToSocket(logMsg+Environment.NewLine);

                Console.WriteLine(logMsg);
            }
        }

        public static void Error()
        {
            Error("");
        }

        public static void Error(params string[] message)
        {
            foreach (var msg in message)
            {
                var logMsg = $"{LogTs} tid:{Tid} {msg}";

                if (MultiFunctionApplication.EnableSocketListener)
                    MultiFunctionApplication.SendToSocket(logMsg+Environment.NewLine);

                Console.Error.WriteLine(logMsg);
            }
        }

        public static void Error(Exception ex)
        {
            Error(ex.ToString());
        }

    }
}
