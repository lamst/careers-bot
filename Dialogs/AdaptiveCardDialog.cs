// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using AdaptiveCards;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;

namespace Microsoft.CareersBot
{
    public abstract class AdaptiveCardDialog : ComponentDialog
    {
        protected const string Organization = "organization";
        protected const string QuestionType = "question-type";
        
        public AdaptiveCardDialog(string dialogId) : base(dialogId) { }

        /// <summary>
        /// Creates a choice with the given value
        /// </summary>
        /// <param name="value">The value of the choice</param>
        /// <returns>The <see cref="FoundChoice"/> created.</returns>
        protected FoundChoice CreateChoice(string value)
        {
            return new FoundChoice()
            {
                Value = value,
            };
        }

        /// <summary>
        /// Sends typing indicator
        /// </summary>
        /// <param name="context">The current context object</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="Task"/> of sending the typing indicator</returns>
        protected Task<ResourceResponse> SendTypingAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var reply = context.Activity.CreateReply();
            reply.Type = ActivityTypes.Typing;
            reply.Text = null;
            return context.SendActivityAsync(reply, cancellationToken);
        }

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