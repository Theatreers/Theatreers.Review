
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace Theatreers.Review
{
    public class ReviewerMessage {
        public string doctype {get; set;}
        public IList<EntityRecord> entities {get; set;}
        public int rating {get; set;}
        public string reference {get; set;}
        public string reviewerUser {get; set;}
        public string sentiment {get; set;}
        public string verbatim {get; set;}
        public string relatesto {get; set;}
    }

    public class DecoratedReviewerMessage : ReviewerMessage {        
        public MessageHeaders MessageProperties {get; set;}
    }

    public class MessageHeaders
    {
        public string RequestCorrelationId {get; set;}
        public string RequestCreatedAt {get; set;}
        public string RequestStatus {get; set;}
    }

    public class EnvelopedMessage {
        public MessageHeaders MessageProperties {get; set;}
        public string RequestObject {get; set;}
    }

    public static class MessageHelper {
        // Decorator - Takes the input and augments the object with an additional set of properties to the JSON body as am additional header level property (MessageProperties)
        // Returns a serialized string with the original object at the root
        public static string DecorateJsonBody(string inputjson, Dictionary<string, JToken> HeaderProperties)
        {
            JObject jObject = JObject.Parse(inputjson);
            jObject.Add("MessageProperties", JObject.FromObject(HeaderProperties));
            return jObject.ToString();
        }

        // Classic 'Envelope' - Takes the input and augments the object with an additional set of properties to the JSON body as an additional header level property (EnvelopeProperties) 
        // whilst appending the original object as a new property called 'requestobject' and returns a serialized string
        public static string EnvelopeJSONBody(string inputjson, Dictionary<string, JToken> HeaderProperties)
        {
            JObject jObject = new JObject();
            jObject.Add("RequestObject", JObject.Parse(inputjson));
            jObject.Add("EnvelopeProperties",JObject.FromObject(HeaderProperties)); 
            return jObject.ToString();
        }
    }
}