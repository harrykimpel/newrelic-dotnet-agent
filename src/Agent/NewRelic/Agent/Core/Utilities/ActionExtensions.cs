// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Core.Logging;
using System;
using System.Threading.Tasks;

namespace NewRelic.Agent.Core.Utilities
{
    public static class ActionExtensions
    {
        public static void CatchAndLog(this Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Error($"An exception occurred while doing some background work: {ex}");
                }
                catch
                {
                }
            }
        }
    }
    public static class AsyncFuncExtensions
    {
        public static async Task CatchAndLogAsync(this Func<Task> asyncFunc)
        {
            try
            {
                await asyncFunc();
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Error($"An exception occurred while doing some background work: {ex}");
                }
                catch
                {
                }
            }
        }
    }
}
