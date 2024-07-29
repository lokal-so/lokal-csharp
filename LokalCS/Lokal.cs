using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using SemanticVersioning;

namespace LokalCS
{
    public class Lokal
    {
        private string _baseUrl;
        private HttpClient _restClient;
        private const string ServerMinVersion = "1.0.0"; // Update this as needed

        public Lokal()
        {
            _baseUrl = "http://127.0.0.1:6174";
            _restClient = new HttpClient();
            _restClient.DefaultRequestHeaders.Add("User-Agent", "Lokal .NET - github.com/lokal-so/lokal-dotnet");
        }

        public static async Task<Lokal> NewDefaultAsync()
        {
            var lokal = new Lokal();
            await lokal.ValidateServerVersionAsync();
            return lokal;
        }

        public Lokal SetBaseUrl(string url)
        {
            _baseUrl = url;
            _restClient.BaseAddress = new Uri(_baseUrl);
            return this;
        }

        public Lokal SetBasicAuth(string username, string password)
        {
            var authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            _restClient.DefaultRequestHeaders.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
            return this;
        }

        public Lokal SetApiToken(string token)
        {
            _restClient.DefaultRequestHeaders.Add("X-Auth-Token", token);
            return this;
        }

        private async Task ValidateServerVersionAsync()
        {
            var content = new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
            var response = await GetRestClient().PostAsync("/api/tunnel/start", content);

            if (response.IsSuccessStatusCode && response.Headers != null)
            {
                var serverVersionHeader = response.Headers.FirstOrDefault(h => h.Key == "Lokal-Server-Version");
                {
                    var serverVersion = new SemanticVersioning.Version(serverVersionHeader.Value.ToString());
                    var minVersion = new SemanticVersioning.Version(ServerMinVersion);

                    if (serverVersion < minVersion)
                    {
                        throw new Exception($"Your local client is outdated, please update to minimum version {ServerMinVersion}");
                    }
                }
            }
            else
            {
                throw new Exception("Failed to validate server version");
            }
        }

        public Tunnel NewTunnel()
        {
            return new Tunnel(this);
        }

        internal HttpClient GetRestClient()
        {
            return _restClient;
        }
    }
}