// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using NewRelic.Agent.Core.Errors;

namespace NewRelic.Agent.Core.Transactions
{
    public interface ITransactionErrorState : IReadOnlyTransactionErrorState
    {
        void AddCustomErrorData(ErrorData errorData);
        void AddExceptionData(ErrorData errorData);
        void AddStatusCodeErrorData(ErrorData errorData);
        void SetIgnoreCustomErrors();
        void SetIgnoreAgentNoticedErrors();
        void TrySetSpanIdForErrorData(ErrorData errorData, string spanId);
    }

    public interface IReadOnlyTransactionErrorState
    {
        bool HasError { get; }
        ErrorData ErrorData { get; }
        string ErrorDataSpanId { get; }
        bool IgnoreCustomErrors { get; }
        bool IgnoreAgentNoticedErrors { get; }
    }

    public class TransactionErrorState : ITransactionErrorState
    {
        // It makes sense to have multiple custom errors per transation,
        // but only one unhandled exception and status code per transaction
        private List<ErrorDataWithSpanId> _customErrorData = new List<ErrorDataWithSpanId>();
        private ErrorDataWithSpanId _transactionExceptionData;
        private ErrorDataWithSpanId _statusCodeErrorData;

        public bool HasError => GetErrorsToReport().Any();

        public ErrorData ErrorData => GetErrorsToReport().FirstOrDefault().ErrorData;
        public string ErrorDataSpanId => GetErrorsToReport().FirstOrDefault().SpanId;

        public bool IgnoreCustomErrors { get; private set; }
        public bool IgnoreAgentNoticedErrors { get; private set; }

        private List<ErrorDataWithSpanId> GetErrorsToReport()
        {
            var errorsToReport = new List<ErrorDataWithSpanId>();

            // I don't think this previous logic makes any sense; if a custom error was reported and should be ignored,
            // why would we not still report any other errors that exist?
            //if (IgnoreCustomErrors) return (null, null);

            if (! IgnoreCustomErrors && _customErrorData.Any())
            {
                errorsToReport.AddRange(_customErrorData);
            }

            if (!IgnoreAgentNoticedErrors && _transactionExceptionData.ErrorData != null)
            {
                errorsToReport.Add(_transactionExceptionData);
            }

            if (_statusCodeErrorData.ErrorData != null)
            {
                errorsToReport.Add(_statusCodeErrorData);
            }

            return errorsToReport;
        }

        public void AddCustomErrorData(ErrorData errorData)
        {
            _customErrorData.Add(new ErrorDataWithSpanId(errorData));
        }

        public void AddExceptionData(ErrorData errorData)
        {
            if (_transactionExceptionData == null)
            {
                _transactionExceptionData = new ErrorDataWithSpanId(errorData);
            }
        }

        public void AddStatusCodeErrorData(ErrorData errorData)
        {
            if (_statusCodeErrorData.ErrorData == null)
            {
                _statusCodeErrorData = new ErrorDataWithSpanId(errorData);
            }
        }

        public void SetIgnoreAgentNoticedErrors() => IgnoreAgentNoticedErrors = true;
        public void SetIgnoreCustomErrors() => IgnoreCustomErrors = true;

        public void TrySetSpanIdForErrorData(ErrorData errorData, string spanId)
        {
            foreach (var errorDataWithSpanId in _customErrorData)
            {
                if (errorDataWithSpanId.ErrorData == errorData)
                {
                    errorDataWithSpanId.SetSpanId(spanId); break;
                }
            }
            if (_transactionExceptionData.ErrorData == errorData)
            {
                _transactionExceptionData.SetSpanId(spanId);
            }
            if (_statusCodeErrorData.ErrorData == errorData)
            {
                _statusCodeErrorData.SetSpanId(spanId);
            }
        }
    }

    public class ErrorDataWithSpanId
    {
        private ErrorData _errorData;
        private string _spanId;

        public ErrorDataWithSpanId(ErrorData errorData)
        {
            _errorData = errorData;
            _spanId = null;
        }

        public void SetSpanId(string spanId)
        {
            _spanId = spanId;
        }

        public ErrorData ErrorData => _errorData;
        public string SpanId => _spanId;
    }
}
