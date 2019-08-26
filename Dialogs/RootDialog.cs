// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.CareersBot
{
    /// <summary>
    /// This is the root dialog to begin a conversation.
    /// </summary>
    public class RootDialog : AdaptiveCardDialog
    {
        /// <summary>
        /// Used to reference persisted values in the dialog
        /// </summary>
        private const string PersistedValues = "values";
        private const string DialogUser = "user";

        /// <summary>
        /// The names of the dialogs
        /// </summary>
        private const string MenuPrompt = "menu-prompt";
        private const string MenuRetryPrompt = "menu-retry-prompt";
        private const string MenuOptions = "menu-options";

        // The dictionary containing the adaptive cards to the menu prompts
        private readonly Dictionary<string, string> cards = new Dictionary<string, string>()
        {
            { MenuPrompt, Path.Combine(".", "Resources", "MenuCard.json") },
            { MenuRetryPrompt, Path.Combine(".", "Resources", "RetryMenuCard.json") }
        };

        // An enum of the companies supported
        private enum Companies
        {
            KPMG,
            Deloitte,
            EY,
            PWC,
            NotSupported
        }

        private class User
        {
            /// <summary>
            /// Whether or not a greeting message was sent to the user in a conversation
            /// </summary>
            /// <value></value>
            public bool WasGreeted { get; set; } = false;
        }

        /// <summary>
        /// The localized strings
        /// </summary>
        private StringResource stringResource;

        private UserState userState;

        /// <summary>
        /// Creates a new instance of the root dialog
        /// </summary>
        /// <param name="userState">The state of the user dialog</param>
        /// <returns></returns>
        public RootDialog(StringResource stringResource, UserState userState) : base("root")
        {
            this.stringResource = stringResource;
            this.userState = userState;

            // Add the prompts
            AddDialog(new ChoicePrompt(MenuPrompt, ChoiceValidation));
            AddDialog(new KpmgCareerDialog(Companies.KPMG.ToString()));

            // Defines a simple two step Waterfall to test the slot dialog.
            AddDialog(new WaterfallDialog(MenuOptions, new WaterfallStep[]
            {
                MenuPromptAsync,
                ProcessResultsAsync,
                ResumeAsync
            }));

            InitialDialogId = MenuOptions;
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
            Companies value;
            if (!Enum.TryParse(val, true, out value))
            {
                value = Companies.NotSupported;
            }

            switch (value)
            {
                case Companies.KPMG:
                case Companies.Deloitte:
                case Companies.EY:
                case Companies.PWC:
                    return Task.FromResult(true);
                default:
                    return Task.FromResult(false);

            }
        }

        /// <summary>
        /// Start the child dialog. This will create the main menu adaptive card attachment.
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from prompting user.</returns>
        private async Task<DialogTurnResult> MenuPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext == null)
            {
                throw new ArgumentNullException(nameof(stepContext));
            }

            AdaptiveCard card = CreateAdaptiveCard(cards[MenuPrompt]);
            var userStateAccessor = userState.CreateProperty<ConversationMember>(nameof(ConversationMember));
            var user = await userStateAccessor.GetAsync(stepContext.Context, () => null);
            if (!user.WasGreeted)
            {
                var response = String.Format(stringResource.Greeting, stepContext.Context.Activity.From.Name);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response), cancellationToken);
            }

            return await stepContext.PromptAsync(
                MenuPrompt,
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(card)),
                    RetryPrompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[MenuRetryPrompt])),
                },
                cancellationToken
            );
        }

        /// <summary>
        /// Processes result from the prompt
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from processing the result.</returns>
        private async Task<DialogTurnResult> ProcessResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var val = stepContext.Context.Activity.Text;
            Companies value;
            if (!Enum.TryParse(val, true, out value))
            {
                value = Companies.NotSupported;
            }

            switch (value)
            {
                case Companies.KPMG:
                    return await stepContext.BeginDialogAsync(Companies.KPMG.ToString(), null, cancellationToken);
                default:
                    // Sends a message to user to indicate the bot is unable to help
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ErrorHelpNotFound), cancellationToken);

                    // Call EndDialogAsync to indicate to the runtime that this is the end of our waterfall.
                    return await stepContext.EndDialogAsync();
            }

        }

        /// <summary>
        /// Called when user return to the main menu
        /// </summary>
        /// <param name="stepContext">The context object of the waterfall</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        private async Task<DialogTurnResult> ResumeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Feedback to user and end the waterfall
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ErrorHelpNotFound), cancellationToken);
            return await stepContext.EndDialogAsync();
        }
    }
}