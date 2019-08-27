// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
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
        private const string QuestionPrompt = "question-prompt";
        private const string QuestionTypePrompt = "question-type-prompt";
        private const string RetryPrompt = "question-retry-prompt";


        private const string KeyAsked = "asked";

        // The dictionary containing the adaptive cards to the menu prompts
        private readonly Dictionary<string, string> cards = new Dictionary<string, string>()
        {
            { QuestionTypePrompt, Path.Combine(".", "Resources", "KpmgCareerCard.json") },
            { RetryPrompt, Path.Combine(".", "Resources", "RetryKpmgCareerCard.json") }
        };

        /// <summary>
        /// The localized strings
        /// </summary>
        private StringResource stringResource;

        // The LUIS recognizer
        private CareerAdviseRecognizer recognizer;

        // The QnA Maker
        private QnAMaker qnaMaker;

        private UserState userState;

        public KpmgCareerDialog(
            string dialogId,
            StringResource stringResource,
            CareerAdviseRecognizer recognizer,
            KnowledgeBaseFactory factory,
            UserState userState) : base(dialogId)
        {
            this.stringResource = stringResource;
            this.recognizer = recognizer;
            this.userState = userState;
            this.qnaMaker = factory?.CreateKpmgQnAMaker();

            // Add the prompts
            AddDialog(new ChoicePrompt(QuestionTypePrompt, ChoiceValidation));
            AddDialog(new TextPrompt(QuestionPrompt));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptQuestionTypeAsync,
                QuestionTypePromptResultAsync,
                QuestionPromptResultAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// Validates the response from the choice prompt
        /// </summary>
        /// <param name="promptContext">The context of the prompt</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        private async Task<bool> ChoiceValidation(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            bool result = false;
            string value = null;

            if (recognizer != null && recognizer.IsConfigured)
            {
                CareerAdvise luisResult = await recognizer.RecognizeAsync<CareerAdvise>(promptContext.Context, cancellationToken);
                value = luisResult.QuestionType;
                switch (value?.ToLowerInvariant())
                {
                    case "general":
                    case "application":
                    case "assessment":
                    case "interviews":
                    case "offer":
                    case "starting":
                        result = true;
                        break;
                }
            }
            else
            {
                // Without LUIS, we need to list all the acceptable answers from user
                value = promptContext.Context.Activity.Text;
                switch (value.ToLowerInvariant())
                {
                    case "general questions":
                        value = "general";
                        result = true;
                        break;
                    case "applying":
                        value = "application";
                        result = true;
                        break;
                    case "assessment test":
                        value = "assessment";
                        result = true;
                        break;
                    case "interviews":
                        value = "interviews";
                        result = true;
                        break;
                    case "the offer stage":
                        value = "offer";
                        result = true;
                        break;
                    case "starting new job":
                        value = "starting";
                        result = true;
                        break;
                }
            }

            promptContext.Recognized.Value = CreateChoice(value);
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Prompt user the type of question s/he like to ask.
        /// </summary>
        /// <param name="stepContext">The context of the dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from prompting user.</returns>
        private async Task<DialogTurnResult> PromptQuestionTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext == null)
            {
                throw new ArgumentNullException(nameof(stepContext));
            }

            if (stepContext.Options == null)
            {
                // Prompt user for the type of questions
                return await RunPromptAsync(stepContext, cancellationToken);
            }
            else
            {
                // Move on to process the response
                return await stepContext.NextAsync(CreateChoice(stepContext.Options.ToString()), cancellationToken);
            }
        }

        /// <summary>
        /// Processes result from the question type prompt
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="System.Threading.Tasks.Task{TResult}"/> from processing the result</returns>
        private async Task<DialogTurnResult> QuestionTypePromptResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var val = stepContext.Context.Activity.Text;
            if (stepContext.Result != null)
            {
                val = ((FoundChoice)stepContext.Result).Value;
            }

            if (String.IsNullOrEmpty(val))
            {
                // Prompt user for the type of question s/he has
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else
            {
                stepContext.Values[QuestionType] = val;
                
                return await stepContext.PromptAsync(
                    QuestionPrompt,
                    new PromptOptions
                    {
                        Prompt = (Activity)MessageFactory.Text(stringResource.PromptQuestion),
                    },
                    cancellationToken);
            }
        }

        /// <summary>
        /// Process result from question prompt
        /// </summary>
        /// <param name="stepContext">The context of the waterfall dialog</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/> from processing the result</returns>
        private async Task<DialogTurnResult> QuestionPromptResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Sends activity indicator
            await SendTypingAsync(stepContext.Context, cancellationToken);

            if (recognizer != null && recognizer.IsConfigured)
            {
                await SendTypingAsync(stepContext.Context, cancellationToken);
                CareerAdvise luisResult = await recognizer.RecognizeAsync<CareerAdvise>(stepContext.Context, cancellationToken);

                switch (luisResult.TopIntent(0.8).Intent)
                {
                    case CareerAdvise.Intent.Finish:
                        // Ends the current dialog
                        return await stepContext.EndDialogAsync();
                    case CareerAdvise.Intent.CareerQuestionType:
                        switch (luisResult.Organization)
                        {
                            case Companies.Deloitte:
                            case Companies.EY:
                            case Companies.PWC:
                                // User is asking question for another organization, we end the dialog
                                return await stepContext.EndDialogAsync(luisResult.Organization.ToString());
                        }

                        if (luisResult.QuestionType == null)
                        {
                            stepContext.Values.Remove(QuestionType);
                        }
                        else
                        {
                            stepContext.Values[QuestionType] = luisResult.QuestionType;
                        }
                        break;
                }
            }

            string answer = null;
            if (qnaMaker != null)
            {
                QnAMakerOptions options = new QnAMakerOptions()
                {
                    Top = 3,
                    ScoreThreshold = 0.5F,
                };

                if (stepContext.Values.ContainsKey(QuestionType))
                {
                    options.MetadataBoost = new Metadata[]
                    {
                        new Metadata()
                        {
                            Name = "type",
                            Value = stepContext.Values[QuestionType]?.ToString(),
                        }
                    };
                }

                var response = await qnaMaker.GetAnswersAsync(stepContext.Context, options);
                if (response != null && response.Length > 0)
                {
                    answer = response[0].Answer;
                }
            }

            if (answer != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(stringResource.ErrorHelpNotFound), cancellationToken);
            }

            var opts = stepContext.Values.ContainsKey(QuestionType) ? stepContext.Values[QuestionType]?.ToString() : null;
            return await stepContext.ReplaceDialogAsync(InitialDialogId, opts, cancellationToken);
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
                QuestionTypePrompt,
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[QuestionTypePrompt])),
                    RetryPrompt = (Activity)MessageFactory.Attachment(CreateAdaptiveCardAttachment(cards[RetryPrompt])),
                },
                cancellationToken
            );
        }
    }
}