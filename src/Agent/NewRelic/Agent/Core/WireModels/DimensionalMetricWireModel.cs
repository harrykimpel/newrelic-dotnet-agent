
// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NewRelic.Agent.Core.Attributes;

namespace NewRelic.Agent.Core.WireModels
{
    public interface IDimensionalMetric
    {
        public void AddToAttributes(IAttributeDefinitions attribDefs, AttributeValueCollection attribValues);
        public void AddCount(int count);
        public void UpdateSummary(double val);
    }
    public class DimensionalMetricWireModel : IEventWireModel
    {
        public IAttributeValueCollection AttributeValues { get; private set; }

        public DimensionalMetricWireModel(IAttributeValueCollection attribValues)
        {
            AttributeValues = attribValues;
            AttributeValues.MakeImmutable();
        }
    }

    internal class DimensionalMetricCount : IDimensionalMetric
    {
        public int Count;

        public DimensionalMetricCount(string name)
        {
            Count = 0;
        }

        public DimensionalMetricCount(int count)
        {
            Count = count;
        }

        public void AddCount(int count = 1)
        {
            Count += count;
        }
        public void UpdateSummary(double val) { }

        public void AddToAttributes(IAttributeDefinitions attribDefs, AttributeValueCollection attribValues)
        {
            attribDefs.DmCount.TrySetValue(attribValues, Count);
        }
    }
    internal class DimensionalMetricSummary : IDimensionalMetric
    {
        public long Count;
        public double Total;
        public double Min;
        public double Max;

        public DimensionalMetricSummary(string name)
        {
            Count = 0;
            Total = 0;
            Min = double.MaxValue;
            Max = double.MinValue;
        }

        public DimensionalMetricSummary(double initial)
        {
            Count = 0;
            Total = 0;
            Min = double.MaxValue;
            Max = double.MinValue;

            UpdateSummary(initial);
        }


        public void UpdateSummary(double val)
        {
            Count++;
            Total += val;
            Min = Math.Min(Min, val);
            Max = Math.Max(Max, val);
        }

        public void AddCount(int count) { }


        public void AddToAttributes(IAttributeDefinitions attribDefs, AttributeValueCollection attribValues)
        {
            attribDefs.DmSummary.TrySetValue(attribValues, [Count, Total, Min, Max]);
        }

    }
}
