using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

namespace LUIS_CSharp
{
    class Program
    {
        struct ApplicationInfo
        {
            public Guid ID;
            public string Version;
        }

        private static readonly string authoring_key = "your key";
        private static readonly string authoring_endpoint = "url endpoint";
        async static Task Main(string[] args)
        {
            var credentials = new ApiKeyServiceClientCredentials(authoring_key);
            var client = new LUISAuthoringClient(credentials, new DelegatingHandler[] { })
            {
                Endpoint = authoring_endpoint
            };

            ApplicationInfo appInfo = await CreateAppplication(client);
            await AddIntents(client, appInfo);
            await AddEntities(client, appInfo);
            await AddUtterances(client, appInfo);
            await Train_App(client, appInfo);

            // insert logic for a pause, then call this method to check the status before publishing
            await CheckTrainStatus(client, appInfo);

            await Publish_App(client, appInfo);
        }

        async static Task<ApplicationInfo> CreateAppplication(LUISAuthoringClient client)
        {
            string appName = string.Format("PictureBotLUIS");
            string app_description = "LUIS APP Built With The LUIS SDK";
            string app_version = "0.1";
            string app_culture = "en-us";

            var app_info = new ApplicationCreateObject()
            {
                Name = appName,
                Description = app_description,
                InitialVersionId = app_version,
                Culture = app_culture
            };

            var app_id = await client.Apps.AddAsync(app_info);
            Console.WriteLine("Created New LUIS Application {0} \n with ID {1}", app_info.Name, app_id);
            return new ApplicationInfo() { ID = app_id, Version = app_version };
        }

        async static Task AddIntents(LUISAuthoringClient client, ApplicationInfo app_info)
        {
            List<String> intents = new List<String>();
            intents.Add("Greeting");
            intents.Add("SearchPic");
            intents.Add("OrderPic");
            intents.Add("SharePic");

            foreach (string intent in intents)
            {
                await client.Model.AddIntentAsync(app_info.ID, app_info.Version, new ModelCreateObject()
                {
                    Name = intent
                });
                Console.WriteLine("Created intent {0}", intent);
            }
        }

        async static Task AddEntities(LUISAuthoringClient client, ApplicationInfo app_info)
        {
            // Add machine learned entity
            var facetEntityId = await client.Model.AddEntityAsync(app_info.ID, app_info.Version, new ModelCreateObject()
            {
                Name = "facet"
            });

            Console.WriteLine("Created entity facet");
        }

        async static Task AddUtterances(LUISAuthoringClient client, ApplicationInfo app_info)
        {
            var utterances = new List<ExampleLabelObject>()
            {
                // SearchPic example utterances
                CreateUtterance ("SearchPic", "find outdoor pics", new Dictionary<string, string>() { {"facet", "outdoor"} } ),
                CreateUtterance ("SearchPic", "are there pictures of a train?", new Dictionary<string, string>() { { "facet", "train" } }),
                CreateUtterance ("SearchPic", "find pictures of food", new Dictionary<string, string>()  { { "facet", "food" } }),
                CreateUtterance ("SearchPic", "search for photos of boys playing", new Dictionary<string, string>()  { { "facet", "boys playing" } }),
                CreateUtterance ("SearchPic", "give me colorful pictures", new Dictionary<string, string>()  { { "facet", "colorful" } }),
                CreateUtterance ("SearchPic", "show me beach pics", new Dictionary<string, string>()  { { "facet", "beach" } }),
                CreateUtterance ("SearchPic", "I want to find dog photos", new Dictionary<string, string>()  { { "facet", "dog" } }),
                CreateUtterance ("SearchPic", "find pictures of German shepherds", new Dictionary<string, string>()  { { "facet", "German shepherds" } }),
                CreateUtterance ("SearchPic", "search for pictures of men indoors", new Dictionary<string, string>()  { { "facet", "men indoors" } }),
                CreateUtterance ("SearchPic", "show me pictures of men wearing glasses", new Dictionary<string, string>()  { { "facet", "men wearing glasses" } }),
                CreateUtterance ("SearchPic", "I want to see pics of smiling people", new Dictionary<string, string>()  { { "facet", "smiling people" } }),
                CreateUtterance ("SearchPic", "show me baby pics", new Dictionary<string, string>()  { { "facet", "baby" } }),

            };
            var resultsList = await client.Examples.BatchAsync(app_info.ID, app_info.Version, utterances);

            foreach (var x in resultsList)
            {
                var result = (!x.HasError.GetValueOrDefault()) ? "succeeded" : "failed";
                Console.WriteLine("{0} {1}", x.Value.ExampleId, result);
            }
        }

        // Create utterance with marked text for entities
        static ExampleLabelObject CreateUtterance(string intent, string utterance, Dictionary<string, string> labels)
        {
            var entity_labels = labels.Select(kv => CreateLabel(utterance, kv.Key, kv.Value)).ToList();
            return new ExampleLabelObject()
            {
                IntentName = intent,
                Text = utterance,
                EntityLabels = entity_labels
            };
        }
        // Mark beginning and ending of entity text in utterance
        static EntityLabelObject CreateLabel(string utterance, string key, string value)
        {
            var start_index = utterance.IndexOf(value, StringComparison.InvariantCultureIgnoreCase);
            return new EntityLabelObject()
            {
                EntityName = key,
                StartCharIndex = start_index,
                EndCharIndex = start_index + value.Length
            };
        }

        async static Task Train_App(LUISAuthoringClient client, ApplicationInfo app)
        {
            var response = await client.Train.TrainVersionAsync(app.ID, app.Version);
            Console.WriteLine("Training status: " + response.Status);
        }

        // Publish app, display endpoint URL for the published application.
        async static Task Publish_App(LUISAuthoringClient client, ApplicationInfo app)
        {
            ApplicationPublishObject obj = new ApplicationPublishObject
            {
                VersionId = app.Version,
                IsStaging = true
            };
            var info = await client.Apps.PublishAsync(app.ID, obj);
            Console.WriteLine("Endpoint URL: " + info.EndpointUrl);
        }

        async static Task CheckTrainStatus(LUISAuthoringClient client, ApplicationInfo app)
        {
            var response = await client.Train.GetStatusAsync(app.ID, app.Version);
            Console.WriteLine(response[0].Details.Status.ToString());
        }

    }
}
