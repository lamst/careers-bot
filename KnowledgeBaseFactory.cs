// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.CareersBot
{
    public class KnowledgeBaseFactory
    {
        // The configuration interface
        private IConfiguration configuration;
        
        // The HTTP client factory
        private IHttpClientFactory httpClientFactory;

        public KnowledgeBaseFactory(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            this.configuration = configuration;
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a new QnA maker using configuration for KPMG
        /// </summary>
        /// <returns>The <see cref="QnAMaker"/> created.</returns>
        public QnAMaker CreateKpmgQnAMaker()
        {
            var config = new KpmgConfiguration();
            configuration.Bind("Kpmg", config);

            return new QnAMaker(new QnAMakerEndpoint()
            {
                KnowledgeBaseId = config.QnA.KnowledgebaseId,
                EndpointKey = config.QnA.EndpointKey,
                Host = config.QnA.EndpointHostName
            },
            null,
            httpClientFactory.CreateClient());
        }
    }
}