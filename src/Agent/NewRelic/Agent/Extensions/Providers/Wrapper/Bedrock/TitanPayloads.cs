// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NewRelic.Providers.Wrapper.Bedrock
{
    public class TitanRequestPayload : IRequestPayload
    {
        [JsonPropertyName("inputText")]
        public string Prompt { get; set; }

        public float Temperature {
            get
            {
                return TextGenerationConfig.Temperature;
            }
            set { }
        }

        public int MaxTokens {
            get
            {
                return TextGenerationConfig.TokenCount;
            }
            set { }
        }

        [JsonPropertyName("textGenerationConfig")]
        public TextGenerationConfigData TextGenerationConfig { get; set; }

        public class TextGenerationConfigData
        {
            [JsonPropertyName("maxTokenCount")]
            public int TokenCount { get; set; }

            [JsonPropertyName("temperature")]
            public float Temperature { get; set; }
        }
    }

    public class TitanResponsePayload : IResponsePayload
    {
        public string Content
        {
            get
            {
                return Results[0].OutputText;
            }
            set { }
        }

        [JsonPropertyName("inputTextTokenCount")]
        public int PromptTokenCount { get; set; }

        public int CompletionTokenCount {
            get
            {
                return Results[0].TokenCount;
            }
            set { }
        }

        public int TotalTokenCount
        {
            get
            {
                return PromptTokenCount + CompletionTokenCount;
            }
        }

        public string StopReason
        {
            get
            {
                return Results[0].CompletionReason;
            }
            set { }
        }

        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }

        public class Result
        {
            [JsonPropertyName("tokenCount")]
            public int TokenCount { get; set; }

            [JsonPropertyName("outputText")]
            public string OutputText { get; set; }

            [JsonPropertyName("completionReason")]
            public string CompletionReason { get; set; }
        }
    }
}
