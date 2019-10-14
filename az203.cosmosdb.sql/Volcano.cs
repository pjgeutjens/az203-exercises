using System;
using Newtonsoft.Json;

namespace az203.cosmosdb.sql
{
    public class Volcano  
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        [JsonProperty(PropertyName = "Volcano Name")] 
        public string Name { get; set; }
        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

    }
}