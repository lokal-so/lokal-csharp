using System;
using System.Text;
using System.Threading.Tasks;
using Lokal;
using Newtonsoft.Json;
using RestSharp;

namespace LokalCS
{
    public class Tunnel
    {
        private Lokal _lokal;
        private const string ServerMinVersion = "v0.6.0";

        public string Id { get; set; }
        public string Name { get; set; }
        public TunnelType TunnelType { get; set; }
        public string LocalAddress { get; set; }
        public string ServerId { get; set; }
        public string AddressTunnel { get; set; }
        public long AddressTunnelPort { get; set; }
        public string AddressPublic { get; set; }
        public string AddressMdns { get; set; }
        public bool Inspect { get; set; }
        public Options Options { get; set; }

        private bool _ignoreDuplicate;
        private bool _startupBanner;

        public Tunnel(Lokal lokal)
        {
            _lokal = lokal;
            Options = new Options();
        }

        public Tunnel SetLocalAddress(string localAddress)
        {
            LocalAddress = localAddress;
            return this;
        }

        public Tunnel SetTunnelType(TunnelType tunnelType)
        {
            TunnelType = tunnelType;
            return this;
        }

        public Tunnel SetInspection(bool inspect)
        {
            Inspect = inspect;
            return this;
        }

        public Tunnel SetLANAddress(string lanAddress)
        {
            int index = lanAddress.LastIndexOf(".local", StringComparison.Ordinal);
            AddressMdns = index >= 0 ? lanAddress.Substring(0, index) : lanAddress;
            return this;
        }

        public Tunnel SetPublicAddress(string publicAddress)
        {
            AddressPublic = publicAddress;
            return this;
        }

        public Tunnel SetName(string name)
        {
            Name = name;
            return this;
        }

        public Tunnel IgnoreDuplicate()
        {
            _ignoreDuplicate = true;
            return this;
        }

        public Tunnel ShowStartupBanner()
        {
            _startupBanner = true;
            return this;
        }

        public async Task<Tunnel> CreateAsync()
        {
            if (string.IsNullOrEmpty(AddressMdns) && string.IsNullOrEmpty(AddressPublic))
            {
                throw new Exception("Please enable either LAN address or random/custom public URL");
            }

            var client = _lokal.GetRestClient();
            var content = new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/tunnel/start", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            var tunnelResponse = JsonConvert.DeserializeObject<TunnelResponse>(responseContent);

            if (response.IsSuccessStatusCode && tunnelResponse?.Success == true && tunnelResponse.Tunnel.Count > 0)
            {
                var createdTunnel = tunnelResponse.Tunnel[0];
                AddressPublic = createdTunnel.AddressPublic;
                AddressMdns = createdTunnel.AddressMdns;
                Id = createdTunnel.Id;

                if (_startupBanner)
                {
                    ShowStartupBanner();
                }

                return this;
            }
            else if (_ignoreDuplicate && tunnelResponse?.Message.EndsWith("address is already being used") == true)
            {
                var existingTunnel = tunnelResponse.Tunnel[0];
                AddressPublic = existingTunnel.AddressPublic;
                AddressMdns = existingTunnel.AddressMdns;
                Id = existingTunnel.Id;

                if (_startupBanner)
                {
                    ShowStartupBanner();
                }

                return this;
            }
            else
            {
                throw new Exception(tunnelResponse?.Message ?? "Tunnel creation failed");
            }
        }

        public string GetLANAddress()
        {
            if (string.IsNullOrEmpty(AddressMdns))
            {
                throw new Exception("LAN address is not being set");
            }

            return AddressMdns.EndsWith(".local") ? AddressMdns : $"{AddressMdns}.local";
        }

        public async Task<string> GetPublicAddressAsync()
        {
            if (string.IsNullOrEmpty(AddressPublic))
            {
                throw new Exception("Public address is not requested by client");
            }

            if (TunnelType != TunnelType.HTTP && !AddressPublic.Contains(":"))
            {
                await UpdatePublicUrlPortAsync();
                throw new Exception("Tunnel is using a random port, but it has not been assigned yet. Please try again later");
            }

            return AddressPublic;
        }

        private async Task UpdatePublicUrlPortAsync()
        {
            var client = _lokal.GetRestClient();
            var response = await client.GetAsync($"/api/tunnel/info/{Id}");

            var responseContent = await response.Content.ReadAsStringAsync();
            var tunnelResponse = JsonConvert.DeserializeObject<TunnelResponse>(responseContent);

            if (response.IsSuccessStatusCode && tunnelResponse?.Success == true && tunnelResponse.Tunnel.Count > 0)
            {
                var updatedTunnel = tunnelResponse.Tunnel[0];
                if (!updatedTunnel.AddressPublic.Contains(":"))
                {
                    throw new Exception("Could not get assigned port");
                }

                AddressPublic = updatedTunnel.AddressPublic;
            }
            else
            {
                throw new Exception("Could not get tunnel info");
            }
        }

        private void PrintStartupBanner()
        {
            string banner = @"
    __       _         _             
   / /  ___ | | ____ _| |  ___  ___  
  / /  / _ \| |/ / _  | | / __|/ _ \ 
 / /__| (_) |   < (_| | |_\__ \ (_) |
 \____/\___/|_|\_\__,_|_(_)___/\___/ ";

            Console.WriteLine(banner);
            Console.WriteLine();
            Console.WriteLine($"Minimum Lokal Client\t{ServerMinVersion}");
            Console.WriteLine($"Public Address\t\thttps://{AddressPublic}");
            Console.WriteLine($"LAN Address\t\thttps://{GetLANAddress()}");
            Console.WriteLine();
        }
    }

    internal class TunnelResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Tunnel> Tunnel { get; set; }
    }
}