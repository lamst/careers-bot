// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.CareersBot
{
    public class CareerAdviseRecognizer : IRecognizer
    {
        // The LUIS recognizer
        private readonly LuisRecognizer recognizer;

        public CareerAdviseRecognizer(IConfiguration configuration)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"])
                && !string.IsNullOrEmpty(configuration["LuisAPIKey"])
                && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);

            if (luisIsConfigured)
            {
                string hostName = configuration["LuisAPIHostName"];
                if (!hostName.ToLowerInvariant().StartsWith("https://"))
                {
                    hostName = "https://" + configuration["LuisAPIHostName"];
                }

                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    hostName);

                recognizer = new LuisRecognizer(luisApplication);
            }
        }

        public virtual bool IsConfigured => recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}