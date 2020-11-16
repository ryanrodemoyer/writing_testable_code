﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
        None=1,Spam=2,Malware=3,Ransomware=4,
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
        }

        private static readonly Dictionary<string, ApiKey> ApiKeys = new Dictionary<string, ApiKey>
        {
            { "asdf", new ApiKey(1, "adsf", 10)}
            , {"qwer", new ApiKey(2, "qwer", 100)}
            , {"zxcv", new ApiKey(3, "zxcv", 5)}
        };

        private static readonly Dictionary<string, DomainEntry> Domains = new Dictionary<string, DomainEntry>
        {
            { "yahoo.com", new DomainEntry(1,"yahoo.com", new DateTime(2020,11,6, 0, 0, 5), ThreatVector.None) }
            , { "twitter.com", new DomainEntry(1,"twitter.com", new DateTime(2020,11,4, 0, 2, 12), ThreatVector.None) }
            , { "microsoft.com", new DomainEntry(1,"microsoft.com", new DateTime(2020,11,16, 0, 5, 12), ThreatVector.None) }
            , { "phishme.net", new DomainEntry(1,"phishme.net", new DateTime(2020,11,6, 1, 5, 29), ThreatVector.Spam) }
            , { "clickjack.net", new DomainEntry(1,"clickjack.net", new DateTime(2020,11,6, 0, 17, 58), ThreatVector.Ransomware) }
            , { "mlwarebites.com", new DomainEntry(1,"mlwarebites.com", new DateTime(2020,11,1, 0, 17, 58), ThreatVector.Malware) }
            
        };

        private static Dictionary<string, List<WebCall>> RateLimiter = new Dictionary<string, List<WebCall>>();

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

                        bool domainExists = Domains.TryGetValue(domain, out DomainEntry entry);
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
                                CallsRemaining = keydef.MaxCallsWithinOneMinute - recent.Count()
                                , Domain = domain
                                , SafetyStatus = safetyStatus
                                , Threat = safetyStatus == "unknown" ? null : entry.ThreatVector.ToString()
                                , PreviousThreat = safetyStatus == "unknown" ? entry.ThreatVector.ToString() : null
                            };

                            return Ok(r);
                        }
                        else
                        {
                            var r = new ChekrResponse
                            {
                                CallsRemaining = keydef.MaxCallsWithinOneMinute - recent.Count()
                                , Domain = domain
                                , SafetyStatus = "not-tracked"
                                , Threat = null
                                , PreviousThreat = null
                            };

                            return Ok(r);
                        }
                    }
                }
            }
            
            return Unauthorized();
        }
    } 
    
    //[ApiController]
    //[Route("[controller]")]
    //public class WeatherForecastController : ControllerBase
    //{
    //    private static readonly string[] Summaries = new[]
    //    {
    //        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    //    };

    //    private readonly ILogger<WeatherForecastController> _logger;

    //    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    //    {
    //        _logger = logger;
    //    }

    //    [HttpGet]
    //    public IEnumerable<WeatherForecast> Get()
    //    {
    //        var rng = new Random();
    //        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //        {
    //            Date = DateTime.Now.AddDays(index),
    //            TemperatureC = rng.Next(-20, 55),
    //            Summary = Summaries[rng.Next(Summaries.Length)]
    //        })
    //        .ToArray();
    //    }
    //}
}