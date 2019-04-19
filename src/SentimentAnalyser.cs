
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using Microsoft.Rest;
using System.Threading;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Theatreers.Review
{
    public static class SentimentAnalyser
    {


        [FunctionName("SentimentAnalyser")]
        public static void Run(
            [ServiceBusTrigger("newreview", "sentiment", Connection = "topicConnectionString")]string topicMessage,
            ILogger log,
            [Blob("reviewsentiment", FileAccess.Read, Connection = "storageConnectionString")] CloudBlobContainer blobContainer
        )
        {
            DecoratedReviewerMessage decoratedMessage = JsonConvert.DeserializeObject<DecoratedReviewerMessage>(topicMessage);

            CloudBlockBlob blob = blobContainer.GetBlockBlobReference($"{decoratedMessage.MessageProperties.RequestCorrelationId}.json");
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = Environment.GetEnvironmentVariable("textAnalyticsEndpoint")
            }; 

            
            SentimentBatchResult sentimentResult = client.SentimentAsync(false,
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>()
                    {
                        new MultiLanguageInput("en", "0", decoratedMessage.verbatim)
                    }
                )
            ).Result;

            try {
                blob.UploadTextAsync(JsonConvert.SerializeObject(sentimentResult.Documents[0].Score));
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Sentiment score submitted :: Score of {sentimentResult.Documents[0].Score:0.000}");
            } catch(Exception ex) {
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Incomplete Sentiment Score Analysis :: {ex.Message}");
            }
        }
    }
}
