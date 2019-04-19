
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
using Microsoft.WindowsAzure.Storage.Blob;

namespace Theatreers.Review
{
    public static class EntityExtractor
    {
        [FunctionName("EntityExtractor")]
        public static void Run(
            [ServiceBusTrigger("newreview", "entity", Connection = "topicConnectionString")]string topicMessage, 
            ILogger log,            
            [Blob("reviewentity", FileAccess.Read, Connection = "storageConnectionString")] CloudBlobContainer blobContainer
        )
        {
            DecoratedReviewerMessage decoratedMessage = JsonConvert.DeserializeObject<DecoratedReviewerMessage>(topicMessage);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference($"{decoratedMessage.MessageProperties.RequestCorrelationId}.json");
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = Environment.GetEnvironmentVariable("textAnalyticsEndpoint")
            }; 

            log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Beginning entity extraction");

            EntitiesBatchResult entityResult = client.EntitiesAsync(false,
                    new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput("en", "0", decoratedMessage.verbatim)
                        })).Result;
            string entitiesJson = JsonConvert.SerializeObject(entityResult.Documents[0].Entities);

            try {
                blob.UploadTextAsync(entitiesJson);
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Completed entity extraction :: TBC items extracted");
            } catch(Exception ex) {
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Incomplete entity extraction :: {ex.Message}");
            }            
        }
    }
}
