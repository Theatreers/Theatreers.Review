using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using Microsoft.Azure.WebJobs.ServiceBus;


namespace Theatreers.Review
{
    public static class SubmitReview
    {
        [FunctionName("SubmitReview")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            [ServiceBus("newreview", EntityType.Topic, Connection = "serviceBusConnectionString")] ICollector<ReviewerMessage> outputs
            )
        {
            string rawRequestBody = new StreamReader(req.Body).ReadToEnd();
            ReviewerMessage reviewerMessageObject = JsonConvert.DeserializeObject<ReviewerMessage>(rawRequestBody);

            try
            {
                outputs.Add(reviewerMessageObject);
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        /*[FunctionName("SubmitReviewAsync")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,             
            [ServiceBus("newreview", EntityType.Topic, Connection = "topicConnectionString")] IAsyncCollector<ReviewerMessage> outputs
            )
        {
            string rawRequestBody = new StreamReader(req.Body).ReadToEnd();
            ReviewerMessage reviewerMessageObject = JsonConvert.DeserializeObject<ReviewerMessage>(rawRequestBody);

            await outputs.AddAsync(reviewerMessage);
            return new AcceptedResult();
        }*/
    }
}