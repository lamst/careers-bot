// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json;

namespace Microsoft.CareersBot
{
    public partial class CareerAdvise : IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        /// <summary>
        /// The intents in the LUIS model
        /// </summary>
        public enum Intent
        {
            CareerQuestionType,
            Greeting,
            None,
            GoBack,
            Finish
        }
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class Entity
        {
            // List
            public string[][] CareerQuestion_Type;

            public string[][] CareerQuestion_Organization;

            public class Instance
            {
                public InstanceData[] CareerQuestion_Organization;

                public InstanceData[] CareerQuestion_Type;
            }

            [JsonProperty("$instance")]
            public Instance instance;
        }
        [JsonProperty("entities")]
        public Entity Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Converts the given object to <see cref="CareerAdvise"/>.
        /// </summary>
        /// <param name="result">The JSON payload to convert</param>
        public void Convert(dynamic result)
        {
            // Deserialize the JSON payload
            var app = JsonConvert.DeserializeObject<CareerAdvise>(
                JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        /// <summary>
        /// Returns the top intent recognized
        /// </summary>
        /// <returns>The top intent recognized by LUIS</returns>
        public (Intent Intent, double Score) TopIntent()
        {
            return TopIntent(0.0);
        }

        /// <summary>
        /// Returns the top intent recognized that exceeds the given minimum score
        /// </summary>
        /// <param name="minScore">The minimum score that the recognized intent must exceed</param>
        /// <returns>The top intent that exceeded the given minimum score</returns>
        public (Intent Intent, double Score) TopIntent(double minScore)
        {
            Intent maxIntent = Intent.None;
            var max = minScore;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }

        public Companies Organization
        {
            get
            {
                string result = null;
                try
                {
                    if (Entities.CareerQuestion_Organization[0].GetLength(0) == 1)
                    {
                        result = Entities.CareerQuestion_Organization[0][0];
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }
                
                Companies value;
                if (!Enum.TryParse(result, true, out value))
                {
                    value = Companies.NotSupported;
                }
                return value;
            }
        }

        public string QuestionType
        {
            get
            {
                string result = null;
                try
                {
                    if (Entities.CareerQuestion_Type[0].GetLength(0) == 1)
                    {
                        result = Entities.CareerQuestion_Type[0][0];
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }
                return result;
            }
        }
    }
}