// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0
using NewRelic.Agent.Core.JsonConverters;
using NewRelic.Core;
using NewRelic.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NewRelic.Agent.Core.Transactions
{
    /*
     * Sample of format:
        "X-NewRelic-Synthetics-Info": {
            "version": "1",
            "type": "scheduled|automatedTest|etc",
            "initiator": "<graphql|cli|integration>",
            "attributes": {
                "exampleAttribute": "value",
                "exampleAttribute2": "value2",
            }
        }
     */

    public class SyntheticsInfoHeader
    {
        public const long SupportedHeaderVersion = 1;
        public const string HeaderKey = "X-NewRelic-Synthetics-Info";

        public readonly long Version;

        public readonly string Type;

        public readonly string Initiator;

        public readonly IDictionary<string, string> Attributes;

        public SyntheticsInfoHeader(long version, string type, string initiator, IDictionary<string, string> attributes)
        {
            Version = version;
            Type = type;
            Initiator = initiator;
            Attributes = attributes;
        }

        public static SyntheticsInfoHeader TryCreate(string header)
        {
            if (header == null)
                throw new ArgumentNullException("header");

            try
            {
                var headerInfo = JsonConvert.DeserializeObject<SyntheticsInfoHeader>(header);
                return headerInfo;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "TryCreate() failed");
                return null;
            }
        }

        public string TryGet()
        {
            try
            {
                var serializedHeader = JsonConvert.SerializeObject(this);
                if (serializedHeader == null)
                    throw new JsonSerializationException("Failed to serialize synthetics info header.  Expected string out, received null.");

                return serializedHeader;
            }
            catch (Exception exception)
            {
                Log.Warn(exception, "TryGet() failed");
                return null;
            }
        }
    }
}
