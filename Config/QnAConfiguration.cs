// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.CareersBot
{
    /// <summary>
    /// Helper class to retrieve configuration of QnA Maker
    /// </summary>
    public class QnAConfiguration
    {
        public string KnowledgebaseId { get; set; }

        public string EndpointKey { get; set; }

        public string EndpointHostName { get; set; }
    }
}