using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessCentralPlugin.Helper
{
    public class HttpClientWithLogging
    {
        private readonly HttpClient _httpClient;

        public HttpClientWithLogging(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            HttpResponseMessage response = null;
            var sw = Stopwatch.StartNew();

            try
            {
                response = await _httpClient.SendAsync(request);
                sw.Stop();

                // Buffer the content so it can be read multiple times
                await response.Content.LoadIntoBufferAsync();

                await LogRequestAsync(request, response, sw.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"Error in SendAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            HttpResponseMessage response = null;
            var sw = Stopwatch.StartNew();

            try
            {
                response = await _httpClient.SendAsync(request);
                sw.Stop();

                response.EnsureSuccessStatusCode();

                // Buffer the content so it can be read multiple times
                await response.Content.LoadIntoBufferAsync();

                await LogRequestAsync(request, response, sw.ElapsedMilliseconds);

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, JsonHelper.DeserializeOptions);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"Error in SendAsync<T>: {ex.Message}");
                throw;
            }
        }

        private async Task LogRequestAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs)
        {
            if (Console.IsOutputRedirected || response == null)
                return;

            // Always log at least the basic info
            lock (Timer.ConsoleLock)
            {
                Console.WriteLine($"{request.Method} {request.RequestUri} - {response.StatusCode} in {durationMs} ms");
            }

            // Only do detailed logging if startup check is enabled
            if (!Configuration.EnableStartupCheck)
                return;

            try
            {
                // Read as bytes to avoid any encoding issues
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var content = Encoding.UTF8.GetString(bytes);

                if (!string.IsNullOrEmpty(content))
                {
                    // Pretty print JSON for logging
                    var jsonDoc = JsonDocument.Parse(content);
                    var payload = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });

                    lock (Timer.ConsoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Response payload:\n{payload}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging response: {ex.Message}");
            }
        }
    }
}
