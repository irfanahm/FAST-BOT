using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding;
using System;

namespace FA.Actions
{
    [Serializable]
    public abstract class FABaseLuisAction : BaseLuisAction
    {
        public object Result { get; set; }
    }
}