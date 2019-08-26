// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.CareersBot
{
    public class KpmgCareerDialog : AdaptiveCardDialog
    {
        /// <summary>
        /// The names of the dialogs
        /// </summary>
        private const string QuestionPrompt = "career-question-prompt";
        private const string RetryPrompt = "question-retry-prompt";

        // The dictionary containing the adaptive cards to the menu prompts
        private readonly Dictionary<string, string> cards = new Dictionary<string, string>()
        {
            { QuestionPrompt, Path.Combine(".", "Resources", "KpmgCareerCard.json") },
            { RetryPrompt, Path.Combine(".", "Resources", "RetryKpmgCareerCard.json") }
        };

        public KpmgCareerDialog(string dialogId) : base(dialogId)
        {
            // Add the prompts
            AddDialog(new ChoicePrompt(QuestionPrompt, ChoiceValidation));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                QuestionPromptAsync,
                ProcessResultsAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// Validates the response from the choice prompt
        /// </summary>
        /// <param name="promptContext">The context of the prompt</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        private Task<bool> ChoiceValidation(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var val = promptContext.Context.Activity.Text;

            return Task.FromResult(true);
        }

        /// <summary>
        /// Start the child dialog. This will create the main menu adaptive card attachment.
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from prompting user.</returns>
        private async Task<DialogTurnResult> QuestionPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext == null)
            {
                throw new ArgumentNullException(nameof(stepContext));
            }

            return await stepContext.PromptAsync(
                QuestionPrompt,
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[QuestionPrompt])),
                    RetryPrompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[RetryPrompt])),
                },
                cancellationToken
            );
        }

        /// <summary>
        /// Processes result from the prompt
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from processing the result</returns>
        private async Task<DialogTurnResult> ProcessResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call EndDialogAsync to indicate to the runtime that this is the end of our waterfall.
            return await stepContext.EndDialogAsync();
        }
    }
}