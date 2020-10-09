namespace BatchPublish.Web.Core.Controllers
{
    using Autodesk.Forge;
    using Autodesk.Forge.Core;
    using Autodesk.Forge.DesignAutomation;
    using Autodesk.Forge.DesignAutomation.Model;
    using Autodesk.Forge.Model;
    using BatchPublish.Web.Core.Hubs;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="ResponseExtension" />.
    /// </summary>
    public static class ResponseExtension
    {
        /// <summary>
        /// The WriteJsonAsync.
        /// </summary>
        /// <param name="response">The response<see cref="Microsoft.AspNetCore.Http.HttpResponse"/>.</param>
        /// <param name="json">The json<see cref="string"/>.</param>
        /// <param name="contentType">The contentType<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public static async Task WriteJsonAsync(this Microsoft.AspNetCore.Http.HttpResponse response, string json, string contentType = null)
        {
            response.ContentType = (contentType ?? "application/json; charset=UTF-8");
            await response.WriteAsync(json);
            await response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// Defines the <see cref="DesignAutomationController" />.
    /// </summary>
    [ApiController]
    public class DesignAutomationController : ControllerBase
    {
        /// <summary>
        /// Defines the BucketKey.
        /// </summary>
        private static string BucketKey = "aumadbuckets";

        /// <summary>
        /// Defines the ActivityName.
        /// </summary>
        static readonly string ActivityName = "moogalm.Adsk_BatchPublish_v3+prod";

        /// <summary>
        /// Defines the FORGE_CLIENT_ID.
        /// </summary>
        public static string FORGE_CLIENT_ID = string.Empty;

        /// <summary>
        /// Defines the FORGE_CLIENT_SECRET.
        /// </summary>
        public static string FORGE_CLIENT_SECRET = string.Empty;

        /// <summary>
        /// Defines the FORGE_WEBHOOK_URL.
        /// </summary>
        public static string FORGE_WEBHOOK_URL = string.Empty;

        /// <summary>
        /// Defines the inputFileNameOSS.
        /// </summary>
        static string inputFileNameOSS = string.Empty;

        /// <summary>
        /// Defines the outputFileNameOSS.
        /// </summary>
        static string outputFileNameOSS = string.Format("{0}_output_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), "result.pdf");

        /// <summary>
        /// Defines the UploadUrl.
        /// </summary>
        static string UploadUrl = "";

        /// <summary>
        /// Defines the DownloadUrl.
        /// </summary>
        static string DownloadUrl = "";

        /// <summary>
        /// Gets or sets the InternalToken.
        /// </summary>
        private static dynamic InternalToken { get; set; }

        /// <summary>
        /// The GetInternalASync.
        /// </summary>
        /// <returns>The <see cref="Task{dynamic}"/>.</returns>
        public static async Task<dynamic> GetInternalASync()
        {
            if (InternalToken == null || InternalToken.ExpiresAt < DateTime.UtcNow)
            {
                InternalToken = await Get2LeggedTokenAsync(new Scope[] { Scope.BucketCreate, Scope.BucketRead, Scope.BucketDelete, Scope.DataRead, Scope.DataWrite, Scope.DataCreate, Scope.CodeAll });
                InternalToken.ExpiresAt = DateTime.UtcNow.AddSeconds(InternalToken.expires_in);
            }
            return InternalToken;
        }

        /// <summary>
        /// The Get2LeggedTokenAsync.
        /// </summary>
        /// <param name="scopes">The scopes<see cref="Scope[]"/>.</param>
        /// <returns>The <see cref="Task{dynamic}"/>.</returns>
        private static async Task<dynamic> Get2LeggedTokenAsync(Scope[] scopes)
        {
            TwoLeggedApi oauth = new TwoLeggedApi();
            string grantType = "client_credentials";
            dynamic bearer = await oauth.AuthenticateAsync(
              FORGE_CLIENT_ID,
              FORGE_CLIENT_SECRET,
              grantType,
              scopes);
            return bearer;
        }

        /// <summary>
        /// Defines the hubContext.
        /// </summary>
        private readonly IHubContext<DesignAutomationHub> hubContext;

        /// <summary>
        /// Defines the configuration.
        /// </summary>
        private static ForgeConfiguration configuration;

        /// <summary>
        /// Defines the browserConnectionId.
        /// </summary>
        private static string browserConnectionId = string.Empty;

        /// <summary>
        /// Defines the _env.
        /// </summary>
        private static IWebHostEnvironment _env;

        /// <summary>
        /// Defines the api.
        /// </summary>
        public DesignAutomationClient api;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignAutomationController"/> class.
        /// </summary>
        /// <param name="clientApi">The clientApi<see cref="DesignAutomationClient"/>.</param>
        /// <param name="env">The env<see cref="IWebHostEnvironment"/>.</param>
        /// <param name="hub">The hub<see cref="IHubContext{DesignAutomationHub}"/>.</param>
        /// <param name="config">The config<see cref="IConfiguration"/>.</param>
        public DesignAutomationController(DesignAutomationClient clientApi,
                                          IWebHostEnvironment env,
                                          IHubContext<DesignAutomationHub> hub,
                                          IOptions<ForgeConfiguration> config)
        {
            hubContext = hub;
            configuration = config.Value;
            FORGE_CLIENT_ID =
            Environment.GetEnvironmentVariable("FORGE_CLIENT_ID")
            ?? configuration.ClientId;
            FORGE_CLIENT_SECRET =
                 Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET")
            ?? configuration.ClientSecret;
            FORGE_WEBHOOK_URL = Environment.GetEnvironmentVariable("FORGE_WEBHOOK_URL");
            _env = env;
            api = clientApi;
        }

        /// <summary>
        /// The Upload.
        /// </summary>
        /// <param name="input">The input<see cref="StartWorkitemInput"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("/api/forge/designautomation/Upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromForm] StartWorkitemInput input)
        {
            JObject workItemData = JObject.Parse(input.data);
            browserConnectionId = workItemData["browserConnectionId"].Value<string>();

            var fileSavePath = Path.Combine(_env.ContentRootPath, Path.GetFileName(input.inputFile.FileName));
            using (var stream = new FileStream(fileSavePath, FileMode.Create))
            {
                await input.inputFile.CopyToAsync(stream);
            }
            dynamic oauth = await GetInternalASync();
            // upload file to OSS Bucket
            // 1. ensure bucket exists
            BucketKey = String.Format("{0}_{1}", BucketKey, DateTime.Now.ToString("yyyyMMddhhmmss").ToLower());
            BucketsApi buckets = new BucketsApi();
            buckets.Configuration.AccessToken = oauth.access_token;
            try
            {
                PostBucketsPayload bucketPayload = new PostBucketsPayload(BucketKey, null, PostBucketsPayload.PolicyKeyEnum.Transient);
                await buckets.CreateBucketAsync(bucketPayload, "US");
            }
            catch { }; // in case bucket already exists
            ObjectsApi objects = new ObjectsApi();
            objects.Configuration.AccessToken = oauth.access_token;


            inputFileNameOSS = string.Format("{0}_input_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), Path.GetFileName(input.inputFile.FileName)); // avoid overriding
            object res = default;

            using (StreamReader streamReader = new StreamReader(fileSavePath))
            {
                res = await objects.UploadObjectAsync(BucketKey, inputFileNameOSS, (int)streamReader.BaseStream.Length, streamReader.BaseStream, "application/octet-stream");

            }
            var json = JsonConvert.SerializeObject(res, Formatting.Indented);
            var response = Response;
            await response.WriteJsonAsync(json);
            // delete server copy
            System.IO.File.Delete(fileSavePath);
            return new EmptyResult();
        }

        /// <summary>
        /// The StartWorkitem.
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("/api/forge/designautomation/workitems")]
        public async Task<IActionResult> StartWorkitem()
        {
            // basic input validation



            try
            {
                dynamic oauth = await GetInternalASync();
                ObjectsApi objectsApi = new ObjectsApi();
                objectsApi.Configuration.AccessToken = oauth.access_token;
                PostBucketsSigned bucketsSigned = new PostBucketsSigned(60);
                dynamic signedResp = await objectsApi.CreateSignedResourceAsync(BucketKey, inputFileNameOSS, bucketsSigned, "read");
                DownloadUrl = signedResp.signedUrl;
                signedResp = await objectsApi.CreateSignedResourceAsync(BucketKey, outputFileNameOSS, bucketsSigned, "readwrite");
                UploadUrl = signedResp.signedUrl;
                Console.WriteLine($"\tSuccess: signed resource for input.zip created!\n\t{DownloadUrl}");
                Console.WriteLine($"\tSuccess: signed resource for result.pdf created!\n\t{UploadUrl}");
            }
            catch { }
            Console.WriteLine("Submitting up workitem...");
            string callbackUrl = string.Format("{0}/api/forge/designautomation/callback?id={1}&outputFileName={2}", FORGE_WEBHOOK_URL, browserConnectionId, outputFileNameOSS);
            var workItemStatus = await api.CreateWorkItemAsync(
                new Autodesk.Forge.DesignAutomation.Model.WorkItem()
                {
                    ActivityId = ActivityName,
                    Arguments = new Dictionary<string, IArgument>() {
                              {
                               "inputFile",
                               new XrefTreeArgument() {
                                Url = "http://download.autodesk.com/us/support/files/autocad_2015_templates/acad.dwt"
                               }
                              }, {
                               "inputZip",
                               new XrefTreeArgument() {
                                Url = DownloadUrl, Verb = Verb.Get, LocalName = "export"
                               }
                              }, {
                               "Result",
                               new XrefTreeArgument() {
                                Verb = Verb.Put, Url = UploadUrl
                               }
                                },
                              { "onComplete",
                                new XrefTreeArgument { Verb = Verb.Post, Url = callbackUrl } }
                              }
                });

            return Ok(new { WorkItemId = workItemStatus.Id });
        }

        /// <summary>
        /// The OnCallback.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <param name="outputFileName">The outputFileName<see cref="string"/>.</param>
        /// <param name="body">The body<see cref="dynamic"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("/api/forge/designautomation/callback")]
        public async Task<IActionResult> OnCallback(string id, string outputFileName, [FromBody] dynamic body)
        {
            try
            {
                Console.WriteLine($"\nCallback executed with Id:{id}, outputFile:{outputFileName}");
                // your webhook should return immediately!
                JObject bodyJson = JObject.Parse((string)body.ToString());
                await hubContext.Clients.Client(id).SendAsync("onComplete", bodyJson.ToString());

                var client = new RestClient(bodyJson["reportUrl"].Value<string>());
                var request = new RestRequest(string.Empty);

                byte[] bs = client.DownloadData(request);
                string report = System.Text.Encoding.Default.GetString(bs);
                await hubContext.Clients.Client(id).SendAsync("onComplete", report);

                ObjectsApi objectsApi = new ObjectsApi();
                dynamic signedUrl = await objectsApi.CreateSignedResourceAsyncWithHttpInfo(BucketKey, outputFileName, new PostBucketsSigned(10), "read");
                await hubContext.Clients.Client(id).SendAsync("downloadResult", (string)(signedUrl.Data.signedUrl));
            }
            catch (Exception) { }

            // ALWAYS return ok (200)
            return Ok();
        }
    }

    /// <summary>
    /// Defines the <see cref="StartWorkitemInput" />.
    /// </summary>
    public class StartWorkitemInput
    {
        /// <summary>
        /// Gets or sets the inputFile.
        /// </summary>
        public IFormFile inputFile { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public string data { get; set; }
    }
}
