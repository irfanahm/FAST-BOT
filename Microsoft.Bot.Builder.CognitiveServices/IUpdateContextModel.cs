using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding
{
    /// <summary>
    /// Interface to support data context update
    /// </summary>
    public interface IUpdateContextModel
    {
        /// <summary>
        /// Update Context method will be called by the LUIS Action Dialog, when any changes in state data is detected
        /// this should be used to update any bot state data
        /// </summary>
        /// <param name="context"></param>
        void UpdateContext(IDialogContext context);
    }
}