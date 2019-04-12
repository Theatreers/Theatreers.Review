
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using Microsoft.Rest;
using System.Threading;
using Microsoft.Azure.WebJobs.Host;

namespace Theatreers.Reviews
{
    public static class EntityExtractor
    {
        [FunctionName("EntityExtractor")]
        public static void Run([ServiceBusTrigger("newreview", "entity", Connection = "serviceBusConnectionString")]string topicMessage, ILogger log)
        {

            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = Environment.GetEnvironmentVariable("textAnalyticsEndpoint")
            }; 

            ReviewerMessage reviewerMessage = JsonConvert.DeserializeObject<ReviewerMessage>(topicMessage);
            
            log.LogInformation("Analysing entities...");


            EntitiesBatchResult entityResult = client.EntitiesAsync(false,
                    new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput("en", "0", reviewerMessage.review)
                        })).Result;
            
            log.LogInformation("Entities analysed...");
            
            foreach (var document in entityResult.Documents){
                foreach (EntityRecord entity in document.Entities){
                    log.LogInformation($"{entity.Name} was found ${entity.Type} ${entity.SubType}. Find more details at {entity.WikipediaUrl}");
                }
            }
        }
    }
}
