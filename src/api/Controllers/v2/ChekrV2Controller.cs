using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Primitives;

namespace api.Controllers.v2
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
    [Route("v2/chekr")]
    public class ChekrV2Controller : ControllerBase
    {
        private readonly ILogger<ChekrV2Controller> _logger;
        private readonly DomainChekr _domainChekr;

        public ChekrV2Controller(
            ILogger<ChekrV2Controller> logger
            , DomainChekr domainChekr
            )
        {
            _logger = logger;
            _domainChekr = domainChekr;
        }

        //private static readonly Dictionary<string, ApiKey> ApiKeys = new Dictionary<string, ApiKey>
        //{
        //    {"asdf", new ApiKey(1, "adsf", 10)}
        //    , {"qwer", new ApiKey(2, "qwer", 100)}
        //    , {"zxcv", new ApiKey(3, "zxcv", 5)}
        //};

        //private static readonly Dictionary<string, DomainEntry> Domains = new Dictionary<string, DomainEntry>
        //{
        //    {"yahoo.com", new DomainEntry(1, "yahoo.com", new DateTime(2020, 11, 6, 0, 0, 5), ThreatVector.None)}
        //    , {"twitter.com", new DomainEntry(2, "twitter.com", new DateTime(2020, 11, 4, 0, 2, 12), ThreatVector.None)}
        //    , {"microsoft.com", new DomainEntry(3, "microsoft.com", new DateTime(2020, 11, 16, 0, 5, 12), ThreatVector.None)}
        //    , {"phishme.net", new DomainEntry(4, "phishme.net", new DateTime(2020, 11, 6, 1, 5, 29), ThreatVector.Spam)}
        //    , {"clickjack.net", new DomainEntry(5, "clickjack.net", new DateTime(2020, 11, 6, 0, 17, 58), ThreatVector.Ransomware)}
        //    , {"mlwarebites.com", new DomainEntry(6, "mlwarebites.com", new DateTime(2020, 11, 1, 0, 17, 58), ThreatVector.Malware)}
        //};
        
        [HttpGet("upload")]
        public async Task<IActionResult> UploadAsync()
        {
            await _domainChekr.AddDomains();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery] string domain)
        {
            try
            {
                ChekrResponse chekrResponse = await _domainChekr.GetReportAsync(domain);

                return Ok(chekrResponse);
            }
            catch (ApiKeyMissingException e)
            {
                return Unauthorized(e.Message);
            }
            catch (RateLimitException e)
            {
                return StatusCode((int)HttpStatusCode.TooManyRequests);
            }
            catch (DomainValidationException e)
            {
                return StatusCode((int) HttpStatusCode.NotAcceptable);
            }
        }
    }

    public interface IRateLimit
    {
        bool IsCallWithinLimits(ApiKey keydef, out int callsRemaining);
    }

    public class OneMinuteRateLimit : IRateLimit
    {
        private static Dictionary<string, List<WebCall>> RateLimiter = new Dictionary<string, List<WebCall>>();

        public OneMinuteRateLimit()
        {

        }

        public bool IsCallWithinLimits(ApiKey keydef, out int callsRemaining)
        {
            var call = new WebCall(DateTime.UtcNow);

            bool exists = RateLimiter.TryGetValue(keydef.Key, out List<WebCall> calls);
            if (exists)
            {
                RateLimiter[keydef.Key].Add(call);
            }
            else
            {
                RateLimiter.Add(keydef.Key, new List<WebCall> {call});
            }

            if (calls == null)
            {
                calls = RateLimiter[keydef.Key];
            }
            
            var recent = calls.Where(x => x.When >= DateTime.UtcNow.AddMinutes(-1));
            if (recent.Count() > keydef.MaxCallsWithinOneMinute)
            {
                callsRemaining = 0;
                return false;
            }

            callsRemaining = keydef.MaxCallsWithinOneMinute - recent.Count();
            return true;
        }
    }

    public interface IApiKeyRetriever
    {
        bool TryGetApiKey(out string value);
    }

    public class HttpHeaderApiKeyRetriever : IApiKeyRetriever
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpHeaderApiKeyRetriever(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public bool TryGetApiKey(out string value)
        {
            bool exists = _accessor.HttpContext.Request.Headers.TryGetValue("x-chekr-api-key", out StringValues val);
            if (exists)
            {
                value = val.First();
                return true;
            }

            value = null;
            return false;
        }
    }

    public class ApiKeyMissingException : Exception
    {
        public ApiKeyMissingException(string message) : base(message)
        {
            
        }
    }

    public class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message)
        {

        }
    }

    public class DomainValidationException : Exception
    {
        public DomainValidationException(string message) : base(message)
        {

        }
    }

    public class DomainChekr
    {
        private readonly IDataAccess _dataAccess;
        private readonly IApiKeyRetriever _apiKeyRetriever;
        private readonly IRateLimit _rateLimit;

        public DomainChekr(IDataAccess dataAccess, IApiKeyRetriever apiKeyRetriever, IRateLimit rateLimit)
        {
            _dataAccess = dataAccess;
            _apiKeyRetriever = apiKeyRetriever;
            _rateLimit = rateLimit;
        }

        public static string ExtractBareDomain(string domain)
        {
            var match = Regex.Match(domain, "[0-9a-zA-z-]+\\.(com|net|org)");
            if (match.Success == false)
            {
                return null;
            }

            return match.Value;
        }

        public async Task AddDomains()
        {
            Dictionary<string, DomainEntry> domains = new Dictionary<string, DomainEntry>
            {
                { "yahoo.com", new DomainEntry(1, "yahoo.com", new DateTime(2020, 11, 6, 0, 0, 5), ThreatVector.None)}
                , { "twitter.com", new DomainEntry(2, "twitter.com", new DateTime(2020, 11, 4, 0, 2, 12), ThreatVector.None)}
                , { "microsoft.com", new DomainEntry(3, "microsoft.com", new DateTime(2020, 11, 16, 0, 5, 12), ThreatVector.None)}
                , { "phishme.net", new DomainEntry(4, "phishme.net", new DateTime(2020, 11, 6, 1, 5, 29), ThreatVector.Spam)}
                , { "clickjack.net", new DomainEntry(5, "clickjack.net", new DateTime(2020, 11, 6, 0, 17, 58), ThreatVector.Ransomware)}
                , { "mlwarebites.com", new DomainEntry(6, "mlwarebites.com", new DateTime(2020, 11, 1, 0, 17, 58), ThreatVector.Malware) }
            };

            await _dataAccess.AddDomainsAsync(domains);
        }

        public async Task<ChekrResponse> GetReportAsync(string domain)
        {
            bool apiKeyRetrieved = _apiKeyRetriever.TryGetApiKey(out string apiKey);
            if (apiKeyRetrieved == false)
            {
                throw new ApiKeyMissingException("Cannot locate the API key.");
            }

            Dictionary<string, ApiKey> apiKeys = await _dataAccess.LoadApiKeysAsync();

            bool apiKeyExists = apiKeys.TryGetValue(apiKey, out ApiKey keydef);
            if (apiKeyExists)
            {
                bool rl = _rateLimit.IsCallWithinLimits(keydef, out int callsRemaining);
                if (rl == false)
                {
                    throw new RateLimitException("max calls exceeded for api key. wait one minute and try again.");
                }

                string bare = ExtractBareDomain(domain);
                if (string.IsNullOrWhiteSpace(bare))
                {
                    throw new DomainValidationException($"Unable to validate input of {domain}");
                }

                Dictionary<string, DomainEntry> domains = await _dataAccess.GetDomainsAsync();

                var response = ChekrResponseFactory.Create(keydef, callsRemaining, bare, domains);
                return response;
            }

            return null;
        }
    }

    public static class ChekrResponseFactory
    {
        public static ChekrResponse Create(ApiKey keydef, int callsRemaining, string bare, Dictionary<string, DomainEntry> domains)
        {
            ChekrResponse response = null;

            bool domainExists = domains.TryGetValue(bare, out DomainEntry entry);
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

                response = new ChekrResponse
                {
                    CallsRemaining = callsRemaining
                    , Domain = bare
                    , ScanStatus = "complete"
                    , SafetyStatus = safetyStatus
                    , Threat = safetyStatus == "unknown" ? null : entry.ThreatVector.ToString()
                    , PreviousThreat = safetyStatus == "unknown" ? entry.ThreatVector.ToString() : null
                };
            }
            else
            {
                response = new ChekrResponse
                {
                    CallsRemaining = callsRemaining
                    , Domain = bare
                    , ScanStatus = "in-process"
                    , SafetyStatus = "unknown"
                    , Threat = null
                    , PreviousThreat = null
                };
            }

            return response;
        }
    }

    public interface IDataAccess
    {
        Task AddDomainsAsync(Dictionary<string, DomainEntry> domains);

        Task<Dictionary<string, DomainEntry>> GetDomainsAsync();

        Task<Dictionary<string, ApiKey>> LoadApiKeysAsync();
    }

    public class DataAccess : IDataAccess
    {
        public async Task AddDomainsAsync(Dictionary<string, DomainEntry> domains)
        {
            await using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            await using var deletecmd = sql.CreateCommand();
            deletecmd.CommandText = "delete from cfgDomains";
            deletecmd.ExecuteNonQuery();

            await using var cmd = sql.CreateCommand();
            cmd.CommandText = "insert into cfgDomains (Id,DomainName,LastScannedDate,ThreatVector) values (@1,@2,@3,@4)";

            foreach (var (_, value) in domains)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@1", value.Id);
                cmd.Parameters.AddWithValue("@2", value.Domain);
                cmd.Parameters.AddWithValue("@3", value.LastScanned);
                cmd.Parameters.AddWithValue("@4", value.ThreatVector);

                cmd.ExecuteNonQuery();
            }
        }

        public async Task<Dictionary<string, ApiKey>> LoadApiKeysAsync()
        {
            await using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            await using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select Id,ApiKeyName,CallsWithinOneMinute from cfgApiKeys";

            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new Dictionary<string, ApiKey>();

            int idOrdinal = reader.GetOrdinal("Id");
            int nameOrdinal = reader.GetOrdinal("ApiKeyName");
            int limitOrdinal = reader.GetOrdinal("CallsWithinOneMinute");

            while (reader.Read())
            {
                int id = reader.GetInt32(idOrdinal);
                string name = reader.GetString(nameOrdinal);
                int limit = reader.GetInt32(limitOrdinal);

                var key = new ApiKey(id, name, limit);
                results.Add(name, key);
            }

            return results;
        }

        public async Task<Dictionary<string, DomainEntry>> GetDomainsAsync()
        {
            await using var sql = new SqliteConnection("Data Source=data.db");
            await sql.OpenAsync();

            await using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "select Id,DomainName,LastScannedDate,ThreatVector from cfgdomains";

            await using var reader = await cmd.ExecuteReaderAsync();

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
                ThreatVector threatVector = (ThreatVector)reader.GetInt32(threatOrdinal);

                var key = new DomainEntry(id, domainName, lastScannedDate, threatVector);
                results.Add(domainName, key);
            }

            return results;
        }
    }
}