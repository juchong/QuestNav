using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuestNav.Network.NetworkTables_CSharp
{
    public class Nt4Topic
    {
        public readonly int Uid;
        public readonly string Name;
        public readonly string Type;
        private readonly Dictionary<string, object> _properties;
        
        public Nt4Topic(int uid, string name, string type, Dictionary<string, object> properties)
        {
            Uid = uid;
            Name = name;
            Type = type;
            _properties = properties;
        }
        
        public Nt4Topic(Dictionary<string, object> obj)
        {
            Uid = Convert.ToInt32(obj["id"]);
            Name = Convert.ToString(obj["name"]);
            Type = Convert.ToString(obj["type"]);
            _properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(Convert.ToString(obj["properties"]));
        }

        public Dictionary<string, object> ToPublishObj()
        {
            return new Dictionary<string, object>
            {
                { "name", Name },
                { "type", Type },
                { "pubuid", Uid },
                { "properties", _properties }
            };
        }

        public Dictionary<string, object> ToUnpublishObj()
        {
            return new Dictionary<string, object>
            {
                { "pubuid", Uid }
            };
        }

        public int GetTypeIdx()
        {
            if (Nt4Client.TypeStrIdxLookup.ContainsKey(Type))
            {
                return Nt4Client.TypeStrIdxLookup[Type];
            }
        
            return 5; // Default to binary
        }
    }
}