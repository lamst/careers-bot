// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using AdaptiveCards;
using System.IO;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.CareersBot
{
    public abstract class AdaptiveCardDialog : ComponentDialog
    {
        public AdaptiveCardDialog(string dialogId) : base(dialogId) { }

        /// <summary>
        /// Creates an adaptive card from the template.
        /// </summary>
        /// <param name="filePath">The full path to the adaptive JSON definition file</param>
        /// <returns>The adaptive card</returns>
        protected AdaptiveCard CreateAdaptiveCard(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            return AdaptiveCard.FromJson(adaptiveCardJson).Card;
        }

        /// <summary>
        /// Create the adaptive card attachment.
        /// </summary>
        /// <param name="filePath">The full path to the adaptive JSON definition file.</param>
        /// <returns>The attachment</returns>
        protected Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            AdaptiveCard card = CreateAdaptiveCard(filePath);
            return CreateAdaptiveCardAttachment(card);
        }

        /// <summary>
        /// Creates an attachment from an adaptive card
        /// </summary>
        /// <param name="card">The adaptive card to embed in the attachment</param>
        /// <returns>The attachment</returns>
        protected Attachment CreateAdaptiveCardAttachment(AdaptiveCard card)
        {
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(card.ToJson()),
            };
            return adaptiveCardAttachment;
        }
    }
}