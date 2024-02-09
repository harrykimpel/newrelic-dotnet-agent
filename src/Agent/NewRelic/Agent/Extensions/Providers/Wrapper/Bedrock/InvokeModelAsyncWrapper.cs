// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.BedrockRuntime.Model;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Providers.Wrapper.Bedrock
{
    public class InvokeModelAsyncWrapper : IWrapper
    {
        public bool IsTransactionRequired => true; // part of spec, only create events for transactions.

        private const string WrapperName = "BedrockInvokeModelAsync";

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(WrapperName.Equals(methodInfo.RequestedWrapperName));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgent agent, ITransaction transaction)
        {
            if (instrumentedMethodCall.MethodCall.MethodArguments[0] is not InvokeModelRequest invokeModelRequest)
            {
                return Delegates.NoOp;
            }

            if (instrumentedMethodCall.IsAsync)
            {
                transaction.AttachToAsync();
                transaction.DetachFromPrimary(); //Remove from thread-local type storage
            }

            var operationType = invokeModelRequest.ModelId.Contains("embed") ? "embedding" : "completion";
            var segment = transaction.StartCustomSegment(
                instrumentedMethodCall.MethodCall,
                "Llm/" + operationType + "/Bedrock/" + instrumentedMethodCall.MethodCall.Method.MethodName
            );

            // required per spec
            transaction.SetLlmTransaction(true);

            // required per spec
            var version = GetLibraryVersion(instrumentedMethodCall);
            agent.RecordCountMetric("DotNet/ML/Bedrock/" + version, 1);
            
            return Delegates.GetAsyncDelegateFor<Task<InvokeModelResponse>>(
                agent,
                segment,
                false,
                InvokeTryProcessResponse,
                TaskContinuationOptions.RunContinuationsAsynchronously
            );

            void InvokeTryProcessResponse(Task<InvokeModelResponse> responseTask)
            {
                if (responseTask.IsFaulted)
                {
                    HandleError(invokeModelRequest, responseTask, transaction, agent);
                }

                var invokeModelResponse = responseTask.Result;
                if (invokeModelResponse == null || invokeModelResponse.HttpStatusCode >= System.Net.HttpStatusCode.MultipleChoices)
                {
                    // do something drasatic?
                    segment.End();
                    return;
                }

                ProcessInvokeModel( segment.SpanId, invokeModelRequest, invokeModelResponse, transaction, agent);

                segment.End();
            }
        }

        private void ProcessInvokeModel(string spanId, InvokeModelRequest invokeModelRequest, InvokeModelResponse invokeModelResponse, ITransaction transaction, IAgent agent)
        {
            var requestPayload = Helpers.GetRequestPayload(invokeModelRequest);
            if (requestPayload == null)
            {
                return;
            }

            var responsePayload = Helpers.GetResponsePayload(invokeModelRequest.ModelId, invokeModelResponse);
            if (responsePayload == null)
            {
                return;
            }

            var completionId = Helpers.CreateChatCompletionEvent(agent, transaction, requestPayload, responsePayload, invokeModelRequest, invokeModelResponse);
            Helpers.CreateChatMessageEvents(agent, spanId, transaction, completionId, requestPayload, responsePayload, invokeModelRequest, invokeModelResponse);
        }

        private void HandleError(InvokeModelRequest invokeModelRequest, Task<InvokeModelResponse> responseTask, ITransaction transaction, IAgent agent)
        {
            var requestPayload = Helpers.GetRequestPayload(invokeModelRequest);
            if (requestPayload == null)
            {
                return;
            }

            IResponsePayload responsePayload = null;
            InvokeModelResponse invokeModelResponse = null;

            var completionId = Helpers.CreateChatCompletionEvent(agent, transaction, requestPayload, responsePayload, invokeModelRequest, invokeModelResponse);
        }

        private string GetLibraryVersion(InstrumentedMethodCall methodCall)
        {
            var fullName = methodCall.MethodCall.Method.Type.Assembly.ManifestModule.Assembly.FullName;
            var versionString = "Version=";
            var start = fullName.IndexOf(versionString) + versionString.Length;
            var length = fullName.IndexOf(',', start) - start;
            return fullName.Substring(start, length);
        }
    }
}
