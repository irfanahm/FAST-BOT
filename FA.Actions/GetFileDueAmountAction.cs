using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FA.Actions
{
    [Serializable]
    [LuisActionBinding("GetFileDueAmount", FriendlyName = "Get file account info")]
    public class GetFileDueAmountAction : FABaseLuisAction
    {
        [AddToContext]
        [Required(ErrorMessage = "Please provide a file number.")]
        public string FileNumber { get; set; }

        [AddToContext]
        [Required(ErrorMessage = "What account information you are looking for?")]
        public string AccountInfo { get; set; }

        public override Task<object> FulfillAsync()
        {
            Result = $"Account Info: {FileNumber}:{AccountInfo}";
            return Task.FromResult<object>(this);
        }
    }
}