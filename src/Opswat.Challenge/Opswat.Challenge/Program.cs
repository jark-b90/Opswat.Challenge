using Opswat.Challenge.Exceptions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Opswat.Challenge
{
    internal class Program
    {
        private const string BaseApiUri = "https://api.metadefender.com/v4";
        private const string GetByDataIdUri = BaseApiUri + "/file/{0}";
        private const string GetByHashUri = BaseApiUri + "/hash/{0}";
        private const string ostFileUri = BaseApiUri + "/file";

        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">App input arguments</param>
        /// <remarks>
        /// Tested also with test virus, Eicar, using hash: 275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f
        /// </remarks>
        static async Task Main(string[] args)
        {
            try
            {
                // to keep the app lightweigth i decided to use only .NET resources (no nugets)
                // so no RestSharp or Newtonsoft.Json used in the current code
                var ap = new ArgsParser(args);

                // Checks if 'ApiKey' argument was provided to the program -OR- checks if api is embeded into this assembly.
                var apiKey = GetApiKey(ap);

                // Checks if 'File' argument was provided to the program and calculates it's hash -OR- get hash directly from arguments
                string hashValue = GetFileHash(ap);

                using (var client = new HttpClient())
                using (var getByHashRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(GetByHashUri, hashValue)))
                {
                    getByHashRequest.Headers.Add("apiKey", apiKey);

                    Console.WriteLine($"Checking hash: {hashValue} against the server");

                    var consider404success = ap.TryGetValue("File", out _);
                    var getByHashResponse = await HttpSendReq(client, getByHashRequest, consider404success); // if code 404 occurs we shall continue... 

                    // Check if the file was not found (404 The requested page was not found).
                    if (getByHashResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        var dataId = await UploadFileToServerAsync(ap, apiKey);

                        bool isAnalysing = true;

                        while (isAnalysing)
                        {
                            Console.WriteLine($"Waiting 10 seconds for the server to analyse the file");
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            isAnalysing = await IsAnalysingAsync(dataId, apiKey);

                            if (isAnalysing)
                            {
                                Console.WriteLine($"Server is still analysing the file...");
                            }
                        }
                    }
                    else
                    {
                        var rawContent = getByHashResponse.Content.ReadAsStringAsync().Result;
                        PrintResult(rawContent);
                    }

                    Console.WriteLine(/* empty row */);
                    Console.WriteLine($"That's all folks, i'm out...");
                }
            }
            catch (HttpResponseException hre)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var json = JsonDocument.Parse(hre.Content);
                var error = json.RootElement.GetProperty("error");
                var messages = error.GetProperty("messages").EnumerateArray().Select(m => m.ToString());

                Console.WriteLine($"Error {(int)hre.StatusCode}: {hre.Message}. Messages: {string.Join(";", messages)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

#if DEBUG
            Console.ReadKey();
#endif
        }

        private static void PrintResult(string rawContent)
        {
            var json = JsonDocument.Parse(rawContent);
            var fileInfo = json.RootElement.GetProperty("file_info");
            var scanResults = json.RootElement.GetProperty("scan_results");
            Console.WriteLine($"Filename: {fileInfo.GetProperty("display_name")}");

            var overallStatus = scanResults.GetProperty("scan_all_result_a").ToString()
                .ToLower() == "no threat detected" ?
                    "Clean" :
                    "Infected";

            if (overallStatus == "Clean")
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"OverallStatus: {overallStatus}");
            Console.ResetColor();

            var scanDetails = scanResults.GetProperty("scan_details").EnumerateObject();
            foreach (var engine in scanDetails)
            {
                var engineDetails = engine.Value;
                var threatFound = engineDetails.GetProperty("threat_found").ToString();

                if (threatFound == string.Empty)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine(/* empty row */);
                Console.WriteLine($"Engine: {engine.Name}");
                Console.WriteLine($"  ThreatFound: {(threatFound == string.Empty ? "Clean" : threatFound)}");
                Console.WriteLine($"  ScanResult: {engineDetails.GetProperty("scan_result_i")}");
                Console.WriteLine($"  DefTime: {engineDetails.GetProperty("def_time")}");

                Console.ResetColor();
            }
        }

        private static async Task<bool> IsAnalysingAsync(string dataId, string apiKey)
        {
            using (var client = new HttpClient())
            using (var getByDataIdRequest = new HttpRequestMessage(HttpMethod.Get, string.Format(GetByDataIdUri, dataId)))
            {
                getByDataIdRequest.Headers.Add("apiKey", apiKey);
                getByDataIdRequest.Headers.Add("x-file-metadata", "1");

                Console.WriteLine($"Checking data id: {dataId} against the server");
                var getByDataIdResponse = await HttpSendReq(client, getByDataIdRequest);

                var rawContent = getByDataIdResponse.Content.ReadAsStringAsync().Result;
                var json = JsonDocument.Parse(rawContent);
                var scanResults = json.RootElement.GetProperty("scan_results");
                var progressPercentage = scanResults.GetProperty("progress_percentage").ToString();

                var isAnalysing = progressPercentage != "100";

                if (!isAnalysing)
                {
                    PrintResult(rawContent);
                }

                return isAnalysing;
            }
        }

        private static async Task<string> UploadFileToServerAsync(ArgsParser ap, string apiKey)
        {
            var filePath = GetFilePath(ap);

            using (var client = new HttpClient())
            using (var postFileRequest = new HttpRequestMessage(HttpMethod.Post, ostFileUri))
            {
                postFileRequest.Headers.Add("apiKey", apiKey);
                postFileRequest.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data");

                using (var content = new MultipartFormDataContent())
                using (var fileStream = File.OpenRead(filePath))
                {
                    content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                    // Make sure to set the Content-Type header to multipart/form-data
                    content.Headers.Remove("Content-Type");
                    content.Headers.Add("Content-Type", "application/octet-stream");

                    postFileRequest.Content = content;

                    Console.WriteLine($"Uploading file: {filePath} to the server");
                    var postFileResponse = await HttpSendReq(client, postFileRequest);

                    var rawContent = postFileResponse.Content.ReadAsStringAsync().Result;
                    var json = JsonDocument.Parse(rawContent);
                    var dataId = json.RootElement.GetProperty("data_id").ToString();

                    return dataId;
                }
            }
        }

        private static async Task<HttpResponseMessage> HttpSendReq(HttpClient client, HttpRequestMessage request, bool consider404success = false)
        {
            var response = await client.SendAsync(request);

            // Check if the request was successful (200 The request has succeeded).
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (!consider404success || response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new HttpResponseException(response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());
                }
            }

            return response;
        }

        private static string GetFilePath(ArgsParser ap)
        {
            if (ap.TryGetValue("File", out var filePath))
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException($"Argument provided is null or empty", filePath);
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File provided by 'File' startup argument was no found", filePath);
                }
            }

            return filePath;
        }

        private static string GetFileHash(ArgsParser ap)
        {
            if (ap.TryGetValue("File", out var filePath))
            {
                // calculate hash by using the file input
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File provided by 'File' startup argument was no found", filePath);
                }

                using (var stream = File.OpenRead(filePath))
                using (var hashInst = SHA256.Create())
                {
                    var cHash = hashInst.ComputeHash(stream);
                    var hash = BitConverter.ToString(cHash).Replace("-", "");
                    return hash;
                }
            }
            else if (ap.TryGetValue("Hash", out var hashValue))
            {
                // get hash by using the arguments
                if (string.IsNullOrEmpty(hashValue))
                {
                    throw new ArgumentException($"Argument provided is null or empty", hashValue);
                }

                return hashValue;
            }
            else
            {
                // hash is missing totally...
                throw new ArgumentException($"Argument(s) provided are null or empty: Hash or File. - one of them must exists at startup.");
            }
        }

        private static string GetApiKey(ArgsParser ap)
        {
            string apiKey;
            if (!ap.TryGetValue("ApiKey", out apiKey))
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("Opswat.Challenge.api.key"))
                using (var reader = new StreamReader(stream))
                {
                    apiKey = reader.ReadToEnd()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .First(l => !l.Trim().StartsWith("#"))
                        .Trim();
                }
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException($"Argument provided is null or empty", apiKey);
            }

            return apiKey;
        }
    }
}
