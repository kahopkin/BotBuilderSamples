using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace AdaptiveOAuthBot.Dialogs
{
    public class RootDialog : AdaptiveDialog
    {
        private OAuthInput MyOAuthInput { get; }

        public RootDialog(IConfiguration configuration) : base(nameof(RootDialog))
        {
            MyOAuthInput = new OAuthInput
            {
                ConnectionName = configuration["ConnectionName"],
                Title = "Please log in",
                Text = "This will give you access!",
                InvalidPrompt = new ActivityTemplate("Login was not successful please try again."),
                Timeout = 300000,
                MaxTurnCount = 3,
            };


            string[] paths = { ".", "Dialogs", $"RootDialog.lg" };
            string fullPath = Path.Combine(paths);

            // These steps are executed when this Adaptive Dialog begins
            Triggers = new List<OnCondition>
                {
                    // Add a rule to welcome user
                    new OnConversationUpdateActivity
                    {
                        Actions = WelcomeUserSteps(),
                    },

                    // Respond to user on message activity
                    new OnUnknownIntent
                    {
                        Actions = LoginSteps(),
                    },
                };
            Generator = new TemplateEngineLanguageGenerator(Templates.ParseFile(fullPath));
        }

        private static List<Dialog> WelcomeUserSteps()
        {
            return new List<Dialog>
            {
                // Iterate through membersAdded list and greet user added to the conversation.
                new Foreach()
                {
                    ItemsProperty = "turn.activity.membersAdded",
                    Actions =
                    {
                        // Note: Some channels send two conversation update events - one for the Bot added to the conversation and another for user.
                        // Filter cases where the bot itself is the recipient of the message. 
                        new IfCondition()
                        {
                            Condition = "$foreach.value.name != turn.activity.recipient.name",
                            Actions =
                            {
                                new SendActivity("Hello, I'm the multi-turn prompt bot. Please send a message to get started!")
                            }
                        }
                    }
                }
            };
        }

        private List<Dialog> LoginSteps()
        {
            return new List<Dialog>
            {
                MyOAuthInput,
                //new SendActivity("Turn result = $turn.lastResult"),
                new IfCondition
                {
                    Condition = "turn.lastResult && length($turn.lastResult) > 0",
                    Actions =
                    {
                        new SendActivity("You are now logged in."),
                        new ConfirmInput
                        {
                            Prompt = new ActivityTemplate("Would you like to view your token?"),
                            InvalidPrompt = new ActivityTemplate("Oops, I didn't understand. Would you like to view your token?"),
                            MaxTurnCount = 3,
                        },
                        new IfCondition
                        {
                            Condition = "turn.lastResult == true",
                            Actions =
                            {
                                MyOAuthInput,
                                new SetProperty
                                {
                                    Property = "$token",
                                    Value = "turn.lastResult",
                                },
                                new IfCondition
                                {
                                    Condition = "$token && length($token) > 0",
                                    Actions =
                                    {
                                        new SendActivity("Here is your token `$token`."),
                                    },
                                    ElseActions =
                                    {
                                        new SendActivity("We were unable to retrieve your token for some reason."),
                                    },
                                },
                            },
                        },
                    },
                    ElseActions =
                    {
                        new SendActivity("Sorry, we were unable to log you in."),
                    },
                },
                new EndDialog(),
            };
        }
    }
}
