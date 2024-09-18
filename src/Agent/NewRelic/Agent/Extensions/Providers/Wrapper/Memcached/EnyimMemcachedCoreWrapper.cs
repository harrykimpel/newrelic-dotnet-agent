// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Providers.Wrapper.Memcached
{
    public class EnyimMemcachedCoreWrapper : IWrapper
    {
        public string[] WrapperNames = new string[] { "EnyimMemcachedCoreWrapper" };

        public bool IsTransactionRequired => true;

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            var canWrap = WrapperNames.Contains(methodInfo.RequestedWrapperName, StringComparer.OrdinalIgnoreCase);

            return new CanWrapResponse(canWrap);
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            ParsedSqlStatement parsedStatement;
            string key;

            // Operation is the first argument in all cases, Key is the second argument
            if (instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformStore")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformStoreAsync")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformMutate")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformMutateAsync")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformConcatenate"))
            {
                key = instrumentedMethodCall.MethodCall.MethodArguments[1].ToString();
                parsedStatement = new ParsedSqlStatement(DatastoreVendor.Memcached,
                    key,
                    instrumentedMethodCall.MethodCall.MethodArguments[0].ToString());
            }
            // Operation is always Get, Key is the first argument
            else if (instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformTryGet")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformGet")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("GetAsync"))
            {
                key = instrumentedMethodCall.MethodCall.MethodArguments[0].ToString();
                parsedStatement = new ParsedSqlStatement(DatastoreVendor.Memcached,
                    key,
                    "Get");
            }
            // Operation is always Remove, Key is the first argument
            else if (instrumentedMethodCall.MethodCall.Method.MethodName.Equals("Remove")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("RemoveAsync"))
            {
                key = instrumentedMethodCall.MethodCall.MethodArguments[0].ToString();
                parsedStatement = new ParsedSqlStatement(DatastoreVendor.Memcached,
                    key,
                    "Remove");
            }
            // Method is not instrumented
            else
            {
                return Delegates.NoOp;
            }

            var connectionInfo = MemcachedHelpers.GetConnectionInfo(
                key,
                instrumentedMethodCall.MethodCall.InvocationTarget,
                agent);

            var segment = transaction.StartDatastoreSegment(instrumentedMethodCall.MethodCall, parsedStatement, connectionInfo, isLeaf: true);
            segment.AddCustomAttribute("key", key); // node also stores the key - not required!

            return Delegates.GetDelegateFor(
                onFailure: (ex) => segment.End(ex),
                onComplete: () => segment.End()
            );
        }
    }
}
