using Microsoft.Bot.Builder.CognitiveServices.LuisActionBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FA.Actions
{
    [Serializable]
    [LuisActionBinding("GetFileInfo", FriendlyName = "Get party information")]
    public class GetFileInfoAction : FABaseLuisAction, IValidatableObject
    {
        [AddToContext]
        [Required(ErrorMessage = "Please provide a file number")]
        public string FileNumber { get; set; }

        public string PartyInfo { get; set; }

        public string PartyType { get; set; }

        public string Status { get; set; }

        public override Task<object> FulfillAsync()
        {
            var returnData = $"GetFileInfo: {FileNumber}";
            bool dataFound = false;

            if (!string.IsNullOrWhiteSpace(PartyType))
            {
                returnData += $" Party Data: {PartyType}:{PartyInfo}";
                dataFound = true;
            }

            if (!string.IsNullOrWhiteSpace(Status) || !dataFound)
            {
                returnData += " Status provided.";
            }

            this.Result = returnData;

            return Task.FromResult<object>(this);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool isPartyInfoEmpty = string.IsNullOrWhiteSpace(PartyInfo);
            bool isPartyTypeEmpty = string.IsNullOrWhiteSpace(PartyType);

            if (!(isPartyInfoEmpty && isPartyTypeEmpty))
            {
                if (isPartyTypeEmpty)
                {
                    yield return new ValidationResult($"Who's {PartyInfo} you want? eg. buyers, sellers...", new[] { nameof(PartyType) });
                }
            }
        }
    }
}