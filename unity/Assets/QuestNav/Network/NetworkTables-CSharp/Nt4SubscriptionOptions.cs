using System.Collections.Generic;

namespace QuestNav.Network.NetworkTables_CSharp
{
    public class Nt4SubscriptionOptions
    {
        private double _periodic;
        private bool _all;
        private bool _topicsOnly;
        private bool _prefix;
        
        public Nt4SubscriptionOptions(double periodic = 0.1, bool all = false, bool topicsOnly = false, bool prefix = false)
        {
            _periodic = periodic;
            _all = all;
            _topicsOnly = topicsOnly;
            _prefix = prefix;
        }

        public Dictionary<string, object> ToObj()
        {
            return new Dictionary<string, object>
            {
                { "periodic", _periodic },
                { "all", _all },
                { "topicsonly", _topicsOnly },
                { "prefix", _prefix }
            };
        }
    }
}