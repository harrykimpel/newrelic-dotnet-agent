// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Net;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Reflection;

namespace NewRelic.Providers.Wrapper.Memcached
{
    public class EnyimMemcachedCoreWrapper : IWrapper
    {
        public string[] WrapperNames = new string[] { "EnyimMemcachedCoreWrapper" };

        public bool IsTransactionRequired => true;

        private static bool _hasGetServerFailed = false;

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            var canWrap = WrapperNames.Contains(methodInfo.RequestedWrapperName, StringComparer.OrdinalIgnoreCase);

            return new CanWrapResponse(canWrap);
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            ParsedSqlStatement parsedStatement;
            ConnectionInfo connectionInfo;

            if (instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformStore")
                || instrumentedMethodCall.MethodCall.Method.MethodName.Equals("PerformStoreAsync"))
            {
                parsedStatement = new ParsedSqlStatement(DatastoreVendor.Memcached,
                    instrumentedMethodCall.MethodCall.MethodArguments[1].ToString(),
                    instrumentedMethodCall.MethodCall.MethodArguments[0].ToString());

                GetServerDetails(instrumentedMethodCall, out var address, out var port, agent);
                connectionInfo = new ConnectionInfo(DatastoreVendor.Memcached.ToKnownName(), address, port.HasValue ? port.Value : -1,  null);
            }
            else
            {
                return Delegates.NoOp;
            }

            var segment = transaction.StartDatastoreSegment(instrumentedMethodCall.MethodCall, parsedStatement, connectionInfo, isLeaf: true);
            segment.AddCustomAttribute("key", instrumentedMethodCall.MethodCall.MethodArguments[1]); // node also stores the key - not required!

            return Delegates.GetDelegateFor(
                onFailure: (ex) => segment.End(ex),
                onComplete: () => segment.End()
            );
        }

        private const string AssemblyName = "EnyimMemcachedCore";
        private static Func<object, object> _transformerGetter;
        private static Func<object, string, string> _transformMethod;
        private static Func<object, object> _poolGetter;
        private static Func<object, string, object> _locateMethod;
        private static Func<object, object> _endpointGetter;
        private static Func<object, object> _addressGetter;
        private static Func<object, int> _portGetter;
        private void GetServerDetails(InstrumentedMethodCall instrumentedMethodCall, out string address, out int? port, IAgent agent)
        {
            if (_hasGetServerFailed)
            {
                address = null;
                port = null;
            }

            try
            {
                var target = instrumentedMethodCall.MethodCall.InvocationTarget;
                var key = instrumentedMethodCall.MethodCall.MethodArguments[1].ToString();

                var targetType = target.GetType();
                _transformerGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>(targetType, "KeyTransformer");
                var transformer = _transformerGetter(target);

                _transformMethod ??= VisibilityBypasser.Instance.GenerateOneParameterMethodCaller<string, string>(AssemblyName, transformer.GetType().FullName, "Transform");
                var hashedKey = _transformMethod(transformer, key);

                _poolGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>(targetType, "Pool");
                var pool = _poolGetter(target);

                _locateMethod ??= VisibilityBypasser.Instance.GenerateOneParameterMethodCaller<string, object>(AssemblyName, pool.GetType().FullName, "Enyim.Caching.Memcached.IServerPool.Locate");
                var node = _locateMethod(pool, hashedKey);

                _endpointGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>(node.GetType(), "EndPoint");
                var endpoint = _endpointGetter(node);

                var endpointType = endpoint.GetType();
                _addressGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<object>(endpointType, "Address");
                address = _addressGetter(endpoint).ToString();

                _portGetter ??= VisibilityBypasser.Instance.GeneratePropertyAccessor<int>(endpointType, "Port");
                port = _portGetter(endpoint);
            }
            catch (Exception exception)
            {
                agent.Logger.Warn(exception, "Unable to get Memcached server address/port, likely to due to type differences. Server address/port will not be available.");
                _hasGetServerFailed = true;
                address = null;
                port = null;
            }
        }
    }
}
