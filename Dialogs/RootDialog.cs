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
        /// The names of the dialogs
        /// </summary>
        public const string MenuPrompt = "menu-prompt";
        public const string MenuRetryPrompt = "menu-retry-prompt";
        public const string MenuOptions = "menu-options";

        // The dictionary containing the adaptive cards to the menu prompts
        private readonly Dictionary<string, string> cards = new Dictionary<string, string>()
        {
            { MenuPrompt, Path.Combine(".", "Resources", "MenuCard.json") },
            { MenuRetryPrompt, Path.Combine(".", "Resources", "RetryMenuCard.json") }
        };

        /// <summary>
        /// The localized strings
        /// </summary>
        private StringResource stringResource;

        // The LUIS recognizer
        private CareerAdviseRecognizer recognizer;

        private UserState userState;

        /// <summary>
        /// Creates a new instance of the root dialog
        /// </summary>
        /// <param name="userState">The state of the user dialog</param>
        /// <returns></returns>
        public RootDialog(
            StringResource stringResource,
            CareerAdviseRecognizer recognizer,
            KnowledgeBaseFactory factory,
            UserState userState) : base("root")
        {
            this.stringResource = stringResource;
            this.recognizer = recognizer;
            this.userState = userState;

            // Add the prompts
            AddDialog(new ChoicePrompt(MenuPrompt, ChoiceValidation));
            AddDialog(new KpmgCareerDialog(Companies.KPMG.ToString(), stringResource, recognizer, factory, userState));
            AddDialog(new TextPrompt(Companies.EY.ToString()));

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
        private async Task<bool> ChoiceValidation(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            Companies value;
            if (recognizer != null && recognizer.IsConfigured)
            {
                await SendTypingAsync(promptContext.Context, cancellationToken);
                CareerAdvise luisResult = await recognizer.RecognizeAsync<CareerAdvise>(promptContext.Context, cancellationToken);
                value = luisResult.Organization;
            }
            else
            {
                var val = promptContext.Context.Activity.Text;
                if (!Enum.TryParse(val, true, out value))
                {
                    value = Companies.NotSupported;
                }
            }

            switch (value)
            {
                case Companies.KPMG:
                case Companies.Deloitte:
                case Companies.EY:
                case Companies.PWC:
                    promptContext.Recognized.Value = CreateChoice(value.ToString());
                    return await Task.FromResult(true);
                default:
                    return await Task.FromResult(false);

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

            var userStateAccessor = userState.CreateProperty<ConversationMember>(nameof(ConversationMember));
            var user = await userStateAccessor.GetAsync(stepContext.Context, () => null);

            if (recognizer == null || !recognizer.IsConfigured)
            {
                if (!user.WasGreeted)
                {
                    var response = String.Format(stringResource.ResponseGreeting, stepContext.Context.Activity.From.Name);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response), cancellationToken);
                }

                // If the LUIS model is not configured, we start the menu prompt
                return await RunPromptAsync(stepContext, cancellationToken);
            }

            // The step is called to re-prompt user
            if (stepContext.Options != null)
            {
                Companies value = Companies.NotSupported;
                if (!Enum.TryParse(stepContext.Options?.ToString(), true, out value))
                {
                    value = Companies.NotSupported;
                }

                switch (value)
                {
                    case Companies.KPMG:
                    case Companies.Deloitte:
                    case Companies.EY:
                    case Companies.PWC:
                        return await stepContext.NextAsync(CreateChoice(value.ToString()), cancellationToken);
                    default:
                        return await RunPromptAsync(stepContext, cancellationToken);
                }
            }
            else
            {
                await SendTypingAsync(stepContext.Context, cancellationToken);
                CareerAdvise luisResult = await recognizer.RecognizeAsync<CareerAdvise>(stepContext.Context, cancellationToken);

                switch (luisResult.TopIntent().Intent)
                {
                    case CareerAdvise.Intent.Greeting:
                        string response = !user.WasGreeted ? String.Format(stringResource.ResponseGreeting, stepContext.Context.Activity.From.Name) : stringResource.ResponseWelcome;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(response), cancellationToken);

                        return await RunPromptAsync(stepContext, cancellationToken);
                    case CareerAdvise.Intent.CareerQuestionType:
                        stepContext.Values[Organization] = luisResult.Organization;
                        stepContext.Values[QuestionType] = luisResult.QuestionType;

                        if (luisResult.Organization == Companies.NotSupported)
                        {
                            return await RunPromptAsync(stepContext, cancellationToken);
                        }
                        else
                        {
                            return await stepContext.NextAsync(CreateChoice(luisResult.Organization.ToString()), cancellationToken);
                        }
                    default:
                        // Sends a message to user to indicate the bot is unable to help and end the dialog
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ErrorHelpNotFound), cancellationToken);
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ResponseEnd), cancellationToken);

                        return await stepContext.EndDialogAsync();
                }
            }
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
            if (stepContext.Result != null)
            {
                val = ((FoundChoice)stepContext.Result).Value;
            }

            Companies value;
            if (!Enum.TryParse(val, true, out value))
            {
                value = Companies.NotSupported;
            }

            switch (value)
            {
                case Companies.KPMG:
                    return await stepContext.BeginDialogAsync(
                        Companies.KPMG.ToString(),
                        stepContext.Values.ContainsKey(QuestionType) ? stepContext.Values[QuestionType] : null,
                        cancellationToken);
                case Companies.EY:
                    return await stepContext.PromptAsync(
                        Companies.EY.ToString(),
                        new PromptOptions
                        {
                            Prompt = (Activity)MessageFactory.Text("This is for testing..."),
                        },
                        cancellationToken);
                default:
                    // Sends a message to user to indicate the selection is not supported and restart the dialog
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ErrorUnsupportedOrganization), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, Companies.NotSupported.ToString(), cancellationToken);
            }

        }

        /// <summary>
        /// Called when user returns to the main menu as a result of ending a previous dialog.
        /// </summary>
        /// <param name="stepContext">The context object of the waterfall</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> for ending the dialog</returns>
        private async Task<DialogTurnResult> ResumeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result != null)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, stepContext.Result?.ToString(), cancellationToken);
            }
            else
            {
                var userStateAccessor = userState.CreateProperty<ConversationMember>(nameof(ConversationMember));
                var user = await userStateAccessor.GetAsync(stepContext.Context, () => new ConversationMember());

                // Clears user selection
                user.Company = Companies.NotSupported;
                user.QuestionType = null;

                // Sends a message to user then ends the dialog
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ResponseEnd), cancellationToken);
                return await stepContext.EndDialogAsync();
            }
        }

        /// <summary>
        /// Runs the menu prompt
        /// </summary>
        /// <param name="stepContext">The current context object</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A <see cref="DialogTurnResult"/> indicating the state of this dialog to the caller.</returns>
        private Task<DialogTurnResult> RunPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.PromptAsync(
                MenuPrompt,
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[MenuPrompt])),
                    RetryPrompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[MenuRetryPrompt])),
                },
                cancellationToken
            );
        }
    }
}