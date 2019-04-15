using System.Threading.Tasks;
using Microsoft.Rest;
using System.Net.Http;
using System.Threading;
using System;

namespace Theatreers.Review
{
        /// </summary>
        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("textAnalyticsSubscriptionKey"));
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
}