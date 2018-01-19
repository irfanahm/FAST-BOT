using Microsoft.Bot.Builder.Luis;
using System;
using System.Linq;
using System.Threading;

namespace TestApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var model = new LuisModelAttribute("4cdcd3ea-9642-4148-bd7f-09bf295c32f6", "2937581657af4f46b085b16eb3974b10")
            {
                Verbose = true
            };
            ILuisService service = new LuisService(model);

            while (true)
            {
                var text = Console.ReadLine();
                if (text == "quit")
                    break;

                var result = service.QueryAsync(text, CancellationToken.None).Result;

                result.Intents = (from intent in result.Intents
                                  let topIntent = result.TopScoringIntent
                                  where intent.Score >= 0.25 &&
                                        Math.Abs((intent.Score - topIntent.Score) ?? 0) <= 0.2
                                  select intent).ToList();

                foreach (var intent in result.Intents)
                {
                    Console.WriteLine($"{intent.Intent}:{intent.Score}");
                }

                foreach (var entity in result.Entities)
                {
                    Console.WriteLine($"Entity: {entity.Type}:{entity.Entity}");
                }
            }
        }
    }
}