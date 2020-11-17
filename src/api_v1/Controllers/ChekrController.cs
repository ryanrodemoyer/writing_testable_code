using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Primitives;

namespace api_v1.Controllers
{
    public class ApiKey
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public int MaxCallsWithinOneMinute { get; set; }

        public ApiKey(int id, string key, int maxCallsWithinOneMinute)
        {
            Id = id;
            Key = key;
            MaxCallsWithinOneMinute = maxCallsWithinOneMinute;
        }
    }

    public class DomainEntry
    {
        public int Id { get; set; }

        public string Domain { get; set; }

        public DateTime LastScanned { get; set; }

        public ThreatVector ThreatVector { get; set; }

        public DomainEntry(int id, string domain, DateTime lastScanned, ThreatVector threatVector)
        {
            Id = id;
            Domain = domain;
            LastScanned = lastScanned;
            ThreatVector = threatVector;
        }
    }

    public enum ThreatVector
    {
        None = 1,
        Spam = 2,
        Malware = 3,
        Ransomware = 4,
    }

    public class WebCall
    {
        public DateTime When { get; set; }

        public WebCall(DateTime @when)
        {
            When = when;
        }
    }

    public class ChekrResponse
    {
        public int CallsRemaining { get; set; }

        public string ScanStatus { get; set; }

        public string SafetyStatus { get; set; }

        public string Domain { get; set; }

        public string Threat { get; set; }

        public string PreviousThreat { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class CheckrController : ControllerBase
    {
        private readonly ILogger<CheckrController> _logger;

        public CheckrController(ILogger<CheckrController> logger)
        {
            _logger = logger;

            LoadApiKeys().GetAwaiter().GetResult();
        }

        private static readonly Dictionary<string, ApiKey> ApiKeys = new Dictionary<string, ApiKey>
        {
            {"asdf", new ApiKey(1, "adsf", 10)}
            , {"qwer", new ApiKey(2, "qwer", 100)}
            , {"zxcv", new ApiKey(3, "zxcv", 5)}
        };

        private List<ApiKey> _apiKeys;

        private Dictionary<string, DomainEntry> _domains
        {
            get { return GetDomains().GetAwaiter().GetResult(); }
        }

        private static readonly Dictionary<string, DomainEntry> Domains = new Dictionary<string, DomainEntry>
        {
            {"yahoo.com", new DomainEntry(1, "yahoo.com", new DateTime(2020, 11, 6, 0, 0, 5), ThreatVector.None)}
            , {"twitter.com", new DomainEntry(2, "twitter.com", new DateTime(2020, 11, 4, 0, 2, 12), ThreatVector.None)}
            , {"microsoft.com", new DomainEntry(3, "microsoft.com", new DateTime(2020, 11, 16, 0, 5, 12), ThreatVector.None)}
            , {"phishme.net", new DomainEntry(4, "phishme.net", new DateTime(2020, 11, 6, 1, 5, 29), ThreatVector.Spam)}
            , {"clickjack.net", new DomainEntry(5, "clickjack.net", new DateTime(2020, 11, 6, 0, 17, 58), ThreatVector.Ransomware)}
            , {"mlwarebites.com", new DomainEntry(6, "mlwarebites.com", new DateTime(2020, 11, 1, 0, 17, 58), ThreatVector.Malware)}
        };

        private static Dictionary<string, List<WebCall>> RateLimiter = new Dictionary<string, List<WebCall>>();

        private async Task LoadApiKeys()
        {
            using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select Id,ApiKeyName,CallsWithinOneMinute from cfgApiKeys";

            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<ApiKey>();

            int idOrdinal = reader.GetOrdinal("Id");
            int nameOrdinal = reader.GetOrdinal("ApiKeyName");
            int limitOrdinal = reader.GetOrdinal("CallsWithinOneMinute");

            while (reader.Read())
            {
                int id = reader.GetInt32(idOrdinal);
                string name = reader.GetString(nameOrdinal);
                int limit = reader.GetInt32(limitOrdinal);

                var key = new ApiKey(id, name, limit);
                results.Add(key);
            }

            _apiKeys = results;
        }
        
        private async Task<Dictionary<string,DomainEntry>> GetDomains()
        {
            using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select Id,DomainName,LastScannedDate,ThreatVector from cfgdomains";

            using var reader = await cmd.ExecuteReaderAsync();

            var results = new Dictionary<string, DomainEntry>();

            int idOrdinal = reader.GetOrdinal("Id");
            int nameOrdinal = reader.GetOrdinal("DomainName");
            int scannedOrdinal = reader.GetOrdinal("LastScannedDate");
            int threatOrdinal = reader.GetOrdinal("ThreatVector");

            while (reader.Read())
            {
                int id = reader.GetInt32(idOrdinal);
                string domainName = reader.GetString(nameOrdinal);
                DateTime lastScannedDate = reader.GetDateTime(scannedOrdinal);
                ThreatVector threatVector = (ThreatVector) reader.GetInt32(threatOrdinal);

                var key = new DomainEntry(id, domainName, lastScannedDate, threatVector);
                results.Add(domainName, key);
            }

            //_domains = results;

            return results;
        }

        [HttpGet("upload")]
        public async Task<IActionResult> UploadAsync()
        {
            using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            using var deletecmd = sql.CreateCommand();
            deletecmd.CommandText = "delete from cfgDomains";
            deletecmd.ExecuteNonQuery();

            using var cmd = sql.CreateCommand();
            cmd.CommandText = "insert into cfgDomains (Id,DomainName,LastScannedDate,ThreatVector) values (@1,@2,@3,@4)";

            foreach (var (_, value) in Domains)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@1", value.Id);
                cmd.Parameters.AddWithValue("@2", value.Domain);
                cmd.Parameters.AddWithValue("@3", value.LastScanned);
                cmd.Parameters.AddWithValue("@4", value.ThreatVector);

                cmd.ExecuteNonQuery();
            }

            return Ok();
        }

        [HttpGet]
        public IActionResult GetAsync([FromQuery] string domain)
        {
            bool exists = Request.Headers.TryGetValue("x-chekr-api-key", out StringValues value);
            if (exists)
            {
                string apiKey = value.First();

                bool apiKeyExists = ApiKeys.TryGetValue(apiKey, out ApiKey keydef);
                if (apiKeyExists)
                {
                    bool exists2 = RateLimiter.TryGetValue(apiKey, out List<WebCall> calls);
                    if (!exists2)
                    {
                        RateLimiter.Add(apiKey, new List<WebCall>());
                    }

                    var call = new WebCall(DateTime.UtcNow);
                    RateLimiter[apiKey].Add(call);

                    calls = RateLimiter[apiKey];

                    var recent = calls.Where(x => x.When >= DateTime.UtcNow.AddMinutes(-1));
                    if (recent.Count() > keydef.MaxCallsWithinOneMinute)
                    {
                        throw new ArgumentOutOfRangeException("max calls exceeded for api key. wait one minute and try again.");
                    }
                    else
                    {
                        var match = Regex.Match(domain, "[0-9a-zA-z-]+\\.(com|net|org)");
                        if (match.Success == false)
                        {
                            return BadRequest("domain input malformed");
                        }

                        bool domainExists = _domains.TryGetValue(domain, out DomainEntry entry);
                        //bool domainExists = Domains.TryGetValue(domain, out DomainEntry entry);
                        if (domainExists)
                        {
                            string safetyStatus = null;
                            bool recentScan = entry.LastScanned >= DateTime.UtcNow.AddDays(-7);
                            if (recentScan)
                            {
                                if (entry.ThreatVector == ThreatVector.None)
                                {
                                    safetyStatus = "safe";
                                }
                                else
                                {
                                    safetyStatus = entry.ThreatVector.ToString();
                                }
                            }
                            else
                            {
                                safetyStatus = "unknown";
                            }

                            var r = new ChekrResponse
                            {
                                CallsRemaining = keydef.MaxCallsWithinOneMinute - recent.Count(),
                                Domain = domain,
                                ScanStatus = "complete",
                                SafetyStatus = safetyStatus,
                                Threat = safetyStatus == "unknown" ? null : entry.ThreatVector.ToString(),
                                PreviousThreat = safetyStatus == "unknown" ? entry.ThreatVector.ToString() : null
                            };

                            return Ok(r);
                        }
                        else
                        {
                            var r = new ChekrResponse
                            {
                                CallsRemaining = keydef.MaxCallsWithinOneMinute - recent.Count(),
                                Domain = domain,
                                ScanStatus = "in-process",
                                SafetyStatus = "unknown",
                                Threat = null,
                                PreviousThreat = null
                            };

                            return Ok(r);
                        }
                    }
                }
            }

            return Unauthorized();
        }
    }
}