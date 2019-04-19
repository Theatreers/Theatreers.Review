
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Azure.WebJobs.Extensions.CosmosDB;
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
using System.Linq;

namespace Theatreers.Review
{
    public static class Aggregator
    {

        [FunctionName("Aggregator")]
        public static async void Run(
            [ServiceBusTrigger("newreview", "aggregator", Connection = "topicConnectionString")]string topicMessage,
            ILogger log,
            [Blob("reviewsentiment", FileAccess.Read, Connection = "storageConnectionString")] CloudBlobContainer sentimentBlobContainer,
            [Blob("reviewentity", FileAccess.Read, Connection = "storageConnectionString")] CloudBlobContainer entityBlobContainer,
            [CosmosDB(databaseName: "theatreers", collectionName: "items", ConnectionStringSetting = "cosmosConnectionString")] IAsyncCollector<DecoratedReviewerMessage> reviewOutput
        )
        {
            DecoratedReviewerMessage decoratedMessage = JsonConvert.DeserializeObject<DecoratedReviewerMessage>(topicMessage);
            string filename = $"{decoratedMessage.MessageProperties.RequestCorrelationId}.json";
            CloudBlockBlob entityBlob = entityBlobContainer.GetBlockBlobReference(filename);
            CloudBlockBlob sentimentBlob = sentimentBlobContainer.GetBlockBlobReference(filename);
            int backoff = 300;

            IList<EntityRecord> entitiesObject = await findBlob<IList<EntityRecord>>(entityBlob, backoff);
            string sentimentObject = await findBlob<string>(sentimentBlob, backoff);
            
            decoratedMessage.entities = entitiesObject;
            decoratedMessage.sentiment = sentimentObject;
            decoratedMessage.doctype = "review";

            log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: entity downloaded :: {entitiesObject.FirstOrDefault().ToString()}");
            log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: entity downloaded :: {sentimentObject}");

            try
            {
                await reviewOutput.AddAsync(decoratedMessage);
                await entityBlob.DeleteAsync();
                await sentimentBlob.DeleteAsync();

                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Upload Complete {reviewOutput.ToString()}");
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Aggregation Clean-up Complete");
            }
            catch (Exception ex)
            {
                log.LogInformation($"[Request Correlation ID: {decoratedMessage.MessageProperties.RequestCorrelationId}] :: Aggregation Clean-up Incomplete :: {ex.Message}");
            }
        }

        public static async Task<T> findBlob<T>(CloudBlockBlob blob, int delay){
            while (!await blob.ExistsAsync()){
                await Task.Delay(delay);
                delay*= 2;
            }

            return JsonConvert.DeserializeObject<T>(await blob.DownloadTextAsync());
        }
    }
}