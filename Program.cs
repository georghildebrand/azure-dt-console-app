using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Azure;
using System;
using System.Text.Json;

namespace DigitalTwinsCodeTutorial
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string adtInstanceUrl = "<youradturl>"; 
            var credential = new DefaultAzureCredential();
            DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
            Console.WriteLine($"Service client created – ready to go");
            // uploading data
            Console.WriteLine();
            Console.WriteLine($"Upload a model");
            var typeList = new List<string>();
            string dtdl = File.ReadAllText("new.json");
            typeList.Add(dtdl);
            // Upload the model to the service
            try
            {
                await client.CreateModelsAsync(typeList);
                
            }
            catch (RequestFailedException rex)
            {
                Console.WriteLine($"Load model: {rex.Status}:{rex.Message}");
            }
            // now lets add more models based on the one just uploaded
            // Initialize twin data
            BasicDigitalTwin twinData = new BasicDigitalTwin();
            twinData.Metadata.ModelId = "dtmi:example:SampleModel;1";
            twinData.Contents.Add("data", $"Hello World!");

            string prefix = "sampleTwin-";
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    twinData.Id = $"{prefix}{i}";
                    await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinData.Id, twinData);
                    Console.WriteLine($"Created twin: {prefix}{i}");
                }
                catch (RequestFailedException rex)
                {
                    Console.WriteLine($"Create twin error: {rex.Status}:{rex.Message}");
                }
            }
            // Connect the twins with relationships
            await CreateRelationship(client, "sampleTwin-0", "sampleTwin-1");
            await CreateRelationship(client, "sampleTwin-0", "sampleTwin-2");
            //List the relationships
            await ListRelationships(client, "sampleTwin-0");
            // Run a query for all twins   
            string query = "SELECT * FROM digitaltwins";
            AsyncPageable<BasicDigitalTwin> result = client.QueryAsync<BasicDigitalTwin>(query);
            Console.WriteLine("---------------");
            await foreach (BasicDigitalTwin twin in result)
            {
                Console.WriteLine(JsonSerializer.Serialize(twin));
                Console.WriteLine("---------------");
            }
        }
        public async static Task CreateRelationship(DigitalTwinsClient client, string srcId, string targetId)
        {
            var relationship = new BasicRelationship
            {
                TargetId = targetId,
                Name = "contains"
            };

            try
            {
                string relId = $"{srcId}-contains->{targetId}";
                await client.CreateOrReplaceRelationshipAsync(srcId, relId, relationship);
                Console.WriteLine("Created relationship successfully");
            }
            catch (RequestFailedException rex)
            {
                Console.WriteLine($"Create relationship error: {rex.Status}:{rex.Message}");
            }
        }
        public async static Task ListRelationships(DigitalTwinsClient client, string srcId)
        {
            try
            {
                AsyncPageable<BasicRelationship> results = client.GetRelationshipsAsync<BasicRelationship>(srcId);
                Console.WriteLine($"Twin {srcId} is connected to:");
                await foreach (BasicRelationship rel in results)
                {
                    Console.WriteLine($" -{rel.Name}->{rel.TargetId}");
                }
            }
            catch (RequestFailedException rex)
            {
                Console.WriteLine($"Relationship retrieval error: {rex.Status}:{rex.Message}");
            }
        }
    }
}
