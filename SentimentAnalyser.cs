
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
    public static class SentimentAnalyser
    {
        [FunctionName("SentimentAnalyser")]
        public static void Run([ServiceBusTrigger("newreview", "sentiment", Connection = "serviceBusConnectionString")]string topicMessage, ILogger log)
        {

            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = Environment.GetEnvironmentVariable("textAnalyticsEndpoint")
            }; 

            ReviewerMessage reviewerMessage = JsonConvert.DeserializeObject<ReviewerMessage>(topicMessage);
            
            log.LogInformation("Analysing sentiment...");


            SentimentBatchResult sentimentResult = client.SentimentAsync(false,
                    new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput("en", "0", reviewerMessage.review)
                        })).Result;

            
            log.LogInformation("Sentiment analysed...");
            
            log.LogInformation($"{reviewerMessage.review} gave a Sentiment score of {sentimentResult.Documents[0].Score:0.000}");
        }
    }
}
