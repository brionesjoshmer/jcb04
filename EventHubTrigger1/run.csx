#r "Microsoft.Azure.EventHubs"
#r "Newtonsoft.Json"

using System;
using System.Text;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

public static async Task Run(EventData[] events, Stream inputBlob, ILogger log)
{
    var exceptions = new List<Exception>();

    foreach (EventData eventData in events)
    {
        try
        {
            string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

            // Shows that the function was triggered and logs the event/message received by the Event Hub.
            log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

            // Extract and log all the email addresses from the blob file that is in JSON format.
            using (StreamReader file = new StreamReader(inputBlob))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject holder = (JObject)JToken.ReadFrom(reader);         
                var postEmails = from p in holder["Users"] select (string)p["email"];

                foreach (var item in postEmails)
                {
                    log.LogInformation(item);
                }    
            }
            
            // Access Key Vault.
            var username =  Environment.GetEnvironmentVariable("UsernameFromKeyVault", EnvironmentVariableTarget.Process);
            var password =  Environment.GetEnvironmentVariable("PasswordFromKeyVault", EnvironmentVariableTarget.Process);
            // Logs the Username and Password stored in the Key Vault.
            log.LogInformation($"Username: {username}");
            log.LogInformation($"Password: {password}");

            await Task.Yield();
        }
        catch (Exception e)
        {
            // We need to keep processing the rest of the batch - capture this exception and continue.
            // Also, consider capturing details of the message that failed processing so it can be processed again later.
            exceptions.Add(e);
        }
    }

    // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
    if (exceptions.Count > 1)
        throw new AggregateException(exceptions);

    if (exceptions.Count == 1)
        throw exceptions.Single();
}
