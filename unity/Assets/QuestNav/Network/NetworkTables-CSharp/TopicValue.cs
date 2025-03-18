using System.Collections.Generic;

namespace QuestNav.Network.NetworkTables_CSharp
{
    public class TopicValue<T>
    {
        private Dictionary<long, T> _timestampedValues = new Dictionary<long, T>();
        private T _latestValue = default(T);
        
        public void AddValue(long timestamp, T value)
        {
            _timestampedValues.Add(timestamp, value);
            _latestValue = value;
        }
        
        public T GetValue()
        {
            return _latestValue;
        }
        
        public T GetValue(long timestamp)
        {
            if (_timestampedValues.ContainsKey(timestamp))
            {
                return _timestampedValues[timestamp];
            }

            return default(T);
        }
    }
}