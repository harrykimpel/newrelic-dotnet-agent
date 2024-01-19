// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using NewRelic.Agent.Core.AgentHealth;
using NewRelic.Agent.Core.Attributes;
using NewRelic.Agent.Core.DataTransport;
using NewRelic.Agent.Core.Events;
using NewRelic.Agent.Core.Time;
using NewRelic.Agent.Core.WireModels;
using NewRelic.Collections;
using NewRelic.Core.Logging;
using NewRelic.SystemInterfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NewRelic.Agent.Core.Aggregators
{
    public interface IDimensionalMetricAggregator
    {
        void AddCount(string name, int val = 1);
        void AddSummary(string name, double val);
    }

    /// <summary>
    /// An service for collecting and managing custom events.
    /// </summary>
    public class DimensionalMetricAggregator : AbstractAggregator<DimensionalMetricWireModel>, IDimensionalMetricAggregator
    {
        private readonly IAttributeDefinitionService _attribDefSvc;
        private IAttributeDefinitions _attribDefs => _attribDefSvc?.AttributeDefs;
        private ConcurrentDictionary<string, IDimensionalMetric> _metrics;

        private readonly IAgentHealthReporter _agentHealthReporter;

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public DimensionalMetricAggregator(IAttributeDefinitionService attribDefSvc, IDataTransportService dataTransportService, IScheduler scheduler, IProcessStatic processStatic, IAgentHealthReporter agentHealthReporter)
            : base(dataTransportService, scheduler, processStatic)
        {
            _attribDefSvc = attribDefSvc;
            _agentHealthReporter = agentHealthReporter;
            _metrics = new ConcurrentDictionary<string, IDimensionalMetric>();
        }

        public override void Dispose()
        {
            base.Dispose();
            _readerWriterLockSlim.Dispose();
        }

        protected override bool IsEnabled => true;

        public void AddCount(string name, int val = 1)
        {
            if (_metrics.TryGetValue(name, out IDimensionalMetric metric))
            {
                metric.AddCount(val);
            }
            else
            {
                _metrics[name] = new DimensionalMetricCount(val);
            }
        }

        public void AddSummary(string name, double val)
        {
            if (_metrics.TryGetValue(name, out IDimensionalMetric metric))
            {
                metric.UpdateSummary(val);
            }
            else
            {
                _metrics[name] = new DimensionalMetricSummary(val);
            }
        }

        protected override void Harvest()
        {
            Log.Finest("Dimensional Metric harvest starting.");
            List<DimensionalMetricWireModel> convertedEvents = new List<DimensionalMetricWireModel>();
            foreach (var metric in _metrics)
            {
                var attribValues = new AttributeValueCollection(AttributeDestinations.DimensionalMetric);

                _attribDefs.DmType.TrySetValue(attribValues, "Metric");
                _attribDefs.DmName.TrySetValue(attribValues, metric.Key);
                metric.Value.AddToAttributes(_attribDefs, attribValues);
                //_attribDefs.DmDuration.TrySetValue(attribValues, duration);
                //_attribDefs.DmUnit.TrySetValue(attribValues, units);

                var data = new DimensionalMetricWireModel(attribValues);
                convertedEvents.Add(data);
            }
            var responseStatus = DataTransportService.Send(convertedEvents);
            //HandleResponse(responseStatus, convertedEvents);

            Log.Debug("Dimensional Metric harvest finished.");
        }

        private void HandleResponse(DataTransportResponseStatus responseStatus, ICollection<IDimensionalMetric> metrics)
        {
            switch (responseStatus)
            {
                case DataTransportResponseStatus.RequestSuccessful:
                    _agentHealthReporter.ReportCustomEventsSent(metrics.Count);
                    break;
                case DataTransportResponseStatus.Retain:
                    //RetainEvents(metrics);
                    break;
                case DataTransportResponseStatus.ReduceSizeIfPossibleOtherwiseDiscard:
                case DataTransportResponseStatus.Discard:
                default:
                    break;
            }
        }

        public override void Collect(DimensionalMetricWireModel wireModel)
        {
            throw new NotImplementedException();
        }

        protected override void OnConfigurationUpdated(ConfigurationUpdateSource configurationUpdateSource)
        {
            // Nothing to do right now
        }
    }
}
