using FA.Actions;
using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace FA.Bot.Dialogs
{
    [Serializable]
    public class RootDialog : FALuisActionDialog<object>
    {
        public RootDialog() : base(
            new Assembly[] { typeof(GetFileInfoAction).Assembly },
            (context, action) =>
            {
                PopulateActionFromContext(action, context);
            },
            (action, context) =>
            {
                // any action context initialization code goes here
            },
            new LuisService(new LuisModelAttribute(WebConfigurationManager.AppSettings["Luis_ModelId"],
                WebConfigurationManager.AppSettings["Luis_SubscriptionKey"])
            { Verbose = true }))
        {
        }

        [LuisIntent("GetFileInfo")]
        public async Task GetFileInfoResultHandler(IDialogContext context, object actionResult)
        {
            UpdateContext(context, actionResult);
            await context.PostAsync((actionResult as GetFileInfoAction)?.Result?.ToString() ?? "Sorry, could not fetch the file info.");
        }

        [LuisIntent("GetFileDueAmount")]
        public async Task GetFileDueAmountResultHandler(IDialogContext context, object actionResult)
        {
            UpdateContext(context, actionResult);
            await context.PostAsync((actionResult as GetFileDueAmountAction)?.Result?.ToString() ?? "Sorry, could not fetch the account info.");
        }

        protected override async Task DispatchToLuisActionActivityHandler(IDialogContext context, IAwaitable<IMessageActivity> item, string intentName, ILuisAction luisAction)
        {
            await base.DispatchToLuisActionActivityHandler(context, item, intentName, luisAction);
            await ProcessNextIntent(context, item);
        }

        protected override async Task NoActionDetectedAsync(IDialogContext context, IMessageActivity message)
        {
            await context.PostAsync("Nothing dictected");
        }

        private void UpdateContext(IDialogContext context, object actionResult)
        {
            if (actionResult != null)
            {
                var dataDict = context.PrivateConversationData.GetValueOrDefault<Dictionary<string, object>>("fa_context_data");
                if (dataDict == null)
                    dataDict = new Dictionary<string, object>();

                var properties = actionResult
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes<AddToContextAttribute>().Any());
                foreach (var property in properties)
                {
                    dataDict[property.Name.ToLowerInvariant()] = property.GetValue(actionResult);
                }

                context.PrivateConversationData.SetValue("fa_context_data", dataDict);
            }
        }

        private static void PopulateActionFromContext(ILuisAction action, IDialogContext context)
        {
            if (action != null)
            {
                var dataDict = context.PrivateConversationData.GetValueOrDefault<Dictionary<string, object>>("fa_context_data");
                if (dataDict == null)
                    return;

                var properties = action
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes<AddToContextAttribute>().Any());
                foreach (var property in properties)
                {
                    if (dataDict.TryGetValue(property.Name.ToLowerInvariant(), out var value))
                    {
                        property.SetValue(action, value);
                    }
                }
            }
        }
    }
}