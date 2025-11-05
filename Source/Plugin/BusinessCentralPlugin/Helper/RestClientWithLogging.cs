using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace BusinessCentralPlugin.Helper
{
    public class RestClientWithLogging
    {
        readonly RestClient _restClient;

        public RestClientWithLogging(RestClient restClient)
        {
            _restClient = restClient;
        }

        public RestResponse Execute(RestRequest request)
        {
            RestResponse response = null;
            var sw = new Stopwatch();

            try
            {
                sw.Start();
                response = _restClient.Execute(request);
                sw.Stop();
            }
            finally
            {
                LogRequest(request, response, sw.ElapsedMilliseconds);
            }
            return response;
        }

        public Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            Task<RestResponse> response = null;
            var sw = new Stopwatch();

            try
            {
                sw.Start();
                response = _restClient.ExecuteAsync(request);
            }
            finally
            {
                response?.ContinueWith(t =>
                {
                    sw.Stop();
                    LogRequest(request, t.Result, sw.ElapsedMilliseconds);
                });
            }
            return response;
        }

        public RestResponse<T> Execute<T>(RestRequest request) where T : new()
        {
            RestResponse<T> response = null;
            var sw = new Stopwatch();

            try
            {
                sw.Start();
                response = _restClient.Execute<T>(request);
                sw.Stop();
            }
            finally
            {
                LogRequest(request, response, sw.ElapsedMilliseconds);
            }
            return response;
        }

        public Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request) where T : new()
        {
            Task<RestResponse<T>> response = null;
            var sw = new Stopwatch();

            try
            {
                sw.Start();
                response = _restClient.ExecuteAsync<T>(request);
            }
            finally
            {
                response?.ContinueWith(t =>
                {
                    sw.Stop();
                    LogRequest(request, t.Result, sw.ElapsedMilliseconds);
                });
            }
            return response;
        }

        private void LogRequest(RestRequest request, RestResponse response, long durationMs)
        {
            if (Console.IsOutputRedirected)
                return;

            if (!Configuration.EnableStartupCheck)
            {
                lock (Timer.ConsoleLock)
                {
                    Console.WriteLine($"{request.Method.ToString().ToUpper()} {request.Resource} - {response.StatusCode} in {durationMs} ms");
                }
                    
                return;
            }

            try
            {
                if (response.Content != null)
                {
                    var payload = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response.Content), Formatting.Indented);
                    lock(Timer.ConsoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{request.Method.ToString().ToUpper()} {_restClient.BuildUri(request)}\n{payload}\n{response.StatusCode} in {durationMs} ms");
                        Console.ResetColor();
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}