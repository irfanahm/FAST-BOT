using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding;
using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding.Bot;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FA.Actions
{
    [Serializable]
    public class FALuisActionDialog<TResult> : LuisActionDialog<TResult>
    {
        [NonSerialized]
        private LuisServiceResult ongoingResult;

        public FALuisActionDialog(IEnumerable<Assembly> assemblies,
            Action<IDialogContext, ILuisAction> onActionCreation,
            Action<ILuisAction, object> onContextCreation,
            params ILuisService[] services) : base(assemblies, onActionCreation, onContextCreation, services)
        {
        }

        protected async Task ProcessNextIntent(IDialogContext context, IAwaitable<IMessageActivity> activity)
        {
            if (ongoingResult?.Result.Intents.Count > 1)
            {
                ongoingResult.Result.Intents.RemoveAt(0);
                ongoingResult.Result.TopScoringIntent = ongoingResult.Result.Intents[0];

                var serviceResult = new LuisServiceResult(ongoingResult.Result,
                    ongoingResult.Result.TopScoringIntent, ongoingResult.LuisService);

                await ProcessLuisResultAsync(context, activity, serviceResult);
            }
        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            var messageText = await GetLuisQueryTextAsync(context, message);

            var tasks = this.services.Select(s => s.QueryAsync(messageText, context.CancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);

            var winners = from result in results.Select((value, index) => new { value, index })
                          let resultWinner = this.BestIntentFrom(result.value)
                          where resultWinner != null
                          select new LuisServiceResult(result.value, resultWinner, this.services[result.index]);

            var winner = this.BestResultFrom(winners);
            if (winner.BestIntent.Intent != "None")
            {
                winner.Result.Intents = (from intent in winner.Result.Intents
                                         let topIntent = winner.BestIntent
                                         where intent.Score >= 0.25 &&
                                               intent.Intent != "None" /*&&
                                               Math.Abs((intent.Score - topIntent.Score) ?? 0) <= 0.2*/
                                         select intent).ToList();
            }
            ongoingResult = winner;

            if (winner == null)
            {
                throw new InvalidOperationException("No winning intent selected from Luis results.");
            }

            var intentName = default(string);
            var luisAction = this.actionResolver.ResolveActionFromLuisIntent(winner.Result,
                (action) => onActionCreation?.Invoke(context, action), out intentName);
            if (luisAction != null)
            {
                var executionContextChain = new List<ActionExecutionContext> { new ActionExecutionContext(intentName, luisAction) };
                while (LuisActionResolver.IsContextualAction(luisAction))
                {
                    var luisActionDefinition = default(LuisActionBindingAttribute);
                    if (!LuisActionResolver.CanStartWithNoContextAction(luisAction, out luisActionDefinition))
                    {
                        await context.PostAsync($"Cannot start contextual action '{luisActionDefinition.FriendlyName}' without a valid context.");

                        return;
                    }

                    luisAction = LuisActionResolver.BuildContextForContextualAction(luisAction,
                        (action) => onActionCreation.Invoke(context, action), out intentName);
                    if (luisAction != null)
                    {
                        this.onContextCreation?.Invoke(luisAction, context);

                        executionContextChain.Insert(0, new ActionExecutionContext(intentName, luisAction));
                    }
                }

                var validationResults = default(ICollection<ValidationResult>);
                if (!luisAction.IsValid(out validationResults))
                {
                    var childDialog = new LuisActionMissingEntitiesDialog(winner.LuisService, executionContextChain, this.onActionCreation);

                    context.Call(childDialog, this.LuisActionMissingDialogFinished);
                }
                else
                {
                    await this.DispatchToLuisActionActivityHandler(context, item, intentName, luisAction);
                }
            }
            else
            {
                await this.NoActionDetectedAsync(context, message);
            }
        }

        protected async Task ProcessLuisResultAsync(IDialogContext context, IAwaitable<IMessageActivity> activity,
            LuisServiceResult luisResult)
        {
            var luisAction = this.actionResolver.ResolveActionFromLuisIntent(luisResult.Result,
                (action) => onActionCreation?.Invoke(context, action), out string intentName);
            if (luisAction != null)
            {
                var executionContextChain = new List<ActionExecutionContext> { new ActionExecutionContext(intentName, luisAction) };
                while (LuisActionResolver.IsContextualAction(luisAction))
                {
                    if (!LuisActionResolver.CanStartWithNoContextAction(luisAction, out LuisActionBindingAttribute luisActionDefinition))
                    {
                        await context.PostAsync($"Cannot start contextual action '{luisActionDefinition.FriendlyName}' without a valid context.");

                        return;
                    }

                    luisAction = LuisActionResolver.BuildContextForContextualAction(luisAction,
                        (action) => onActionCreation.Invoke(context, action), out intentName);
                    if (luisAction != null)
                    {
                        this.onContextCreation?.Invoke(luisAction, context);

                        executionContextChain.Insert(0, new ActionExecutionContext(intentName, luisAction));
                    }
                }

                if (!luisAction.IsValid(out ICollection<ValidationResult> validationResults))
                {
                    var childDialog = new LuisActionMissingEntitiesDialog(luisResult.LuisService, executionContextChain,
                        this.onActionCreation);

                    context.Call(childDialog, this.LuisActionMissingDialogFinished);
                }
                else
                {
                    await this.DispatchToLuisActionActivityHandler(context, activity, intentName, luisAction);
                }
            }
            else
            {
                await this.NoActionDetectedAsync(context, await activity);
            }
        }
    }
}