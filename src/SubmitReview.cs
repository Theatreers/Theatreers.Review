using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace Theatreers.Review
{
    public static class SubmitReview
    {
        [FunctionName("SubmitReviewAsync")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,             
            [ServiceBus("newreview", EntityType.Topic, Connection = "topicConnectionString")] IAsyncCollector<string> outputs
            )
        {
            string rawRequestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string CorrelationId = Guid.NewGuid().ToString();  
            string statusCheckEndpoint = $"{Environment.GetEnvironmentVariable("statusCheckURL")}/{CorrelationId}";

            string reviewerMessageEnvelope = MessageHelper.DecorateJsonBody(rawRequestBody, 
                new Dictionary<string, JToken>(){
                    { "RequestCorrelationId", CorrelationId},
                    { "RequestCreatedAt", DateTime.Now},
                    { "RequestStatus", statusCheckEndpoint}
                }
            );

            await outputs.AddAsync(reviewerMessageEnvelope);

            AcceptedResult acceptedResultObject = new AcceptedResult();
            acceptedResultObject.Location = statusCheckEndpoint;
            return (ActionResult) acceptedResultObject;
        }
    }
}