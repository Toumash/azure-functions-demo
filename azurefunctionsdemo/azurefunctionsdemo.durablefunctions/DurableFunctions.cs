using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading;

namespace azurefunctionsdemo.durablefunctions
{

    public static class DurableFunctions
    {
        [FunctionName("ChainSample_start")]
        public static async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
                   [DurableClient]IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = await starter.StartNewAsync("ChainSample_Orchestrator", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("ChainSample_Orchestrator")]
        public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"================= RunOrchestrator method executing =================");

            string greeting = await context.CallActivityAsync<string>("ChainSample_1", "London");
            string toUpper = await context.CallActivityAsync<string>("ChainSample_2", greeting);

            bool approved = await context.WaitForExternalEvent<bool>("Approval");

            if (approved) log.LogInformation("MAmy akcepta, generuj PDFka");
            else log.LogInformation("Idź błagać szefa");

            string withTimestamp = await context.CallActivityAsync<string>("ChainSample_3", toUpper);

            log.LogInformation(withTimestamp);
            return withTimestamp;
        }

        [FunctionName("ChainSample_1")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            return $"Hello {name}!";
        }

        [FunctionName("ChainSample_2")]
        public static string ToUpper([ActivityTrigger] string s, ILogger log)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            return s.ToUpperInvariant();
        }

        [FunctionName("ChainSample_3")]
        public static string AddTimeStamp([ActivityTrigger] string s, ILogger log)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            return $"{s} [{DateTimeOffset.Now}]";
        }
    }
}
