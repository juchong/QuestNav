using System;
using System.Collections.Generic;
using System.Linq;

namespace QuestNav.Network.NetworkTables_CSharp
{
    public class Nt4Source
    {
        public readonly Nt4Client Client;

        private readonly Dictionary<string, TopicValue<string>> _stringValues = new();
        private readonly Dictionary<string, TopicValue<long>> _longValues = new();
        private readonly Dictionary<string, TopicValue<double>> _doubleValues = new();
        private readonly Dictionary<string, TopicValue<double[]>> _doubleArrayValues = new();

        
        private readonly Dictionary<string, string> _queuedPublishes = new();
        private readonly Dictionary<string, Nt4SubscriptionOptions> _queuedSubscribes = new();

        /// <summary>
        /// Create a new NT4Source which automatically creates a client and connects to the server.
        /// </summary>
        public Nt4Source(string appName = "Nt4Unity", string serverAddress = "127.0.0.1", int port = 5810)
        {
            Client = new Nt4Client(appName, serverAddress, port, OnOpen, OnNewTopicData);
            Client.Connect();
        }
        
        public void PublishTopic(string topic, string type)
        {
            if (Client.Connected())
            {
                Client.PublishTopic(topic, type);
            }
            _queuedPublishes.TryAdd(topic, type);
        }
        
        public void PublishValue(string topic, object value)
        {
            if (Client.Connected())
            {
                Client.PublishValue(topic, value);
            }
        }
        
        public void Subscribe(string topic, double period = 0.1, bool all = false, bool topicsOnly = false, bool prefix = false)
        {
            if (Client.Connected())
            {
                Client.Subscribe(topic, period, all, topicsOnly, prefix);
            }
            _queuedSubscribes.TryAdd(topic, new Nt4SubscriptionOptions(period, all, topicsOnly, prefix));
        }

        private void OnOpen(object sender, EventArgs e)
        {
            foreach(string topic in _queuedPublishes.Keys)
            {
                Client.PublishTopic(topic, _queuedPublishes[topic]);
            }
            foreach(string topic in _queuedSubscribes.Keys)
            {
                Client.Subscribe(topic, _queuedSubscribes[topic]);
            }
        }
        private void OnNewTopicData(Nt4Topic topic, long timestamp, object value)
        {
            switch (topic.Type)
            {
                case "string":
                    AddString(topic.Name, timestamp, value);
                    break;
                case "int":
                    AddInteger(topic.Name, timestamp, value);
                    break;
                case "double":
                    AddDouble(topic.Name, timestamp, value);
                    break;
                case "double[]":
                    AddDoubleArray(topic.Name, timestamp, value);
                    break;
            }
        }

        /// <summary>
        /// Get the latest integer value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest string value of the topic, or 0 if it doesn't exist</returns>
        public string GetString(string key)
        {
            if (_stringValues.TryGetValue(key, out var value))
            {
                return value.GetValue();
            }

            return "";
        }

        /// <summary>
        /// Get the latest integer value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest integer value of the topic, or 0 if it doesn't exist</returns>
        public long GetLong(string key)
        {
            if (_longValues.TryGetValue(key, out var value))
            {
                return value.GetValue();
            }

            return 0;
        }
        
        /// <summary>
        /// Get the latest double value of a topic
        /// </summary>
        /// <param name="key">The topic to get the value of</param>
        /// <returns>The latest double value of the topic, or 0 if it doesn't exist</returns>
        public double GetDouble(string key)
        {
            if (_doubleValues.TryGetValue(key, out var value))
            {
                return value.GetValue();
            }

            return 0;
        }

        public double[] GetDoubleArray(string key)
        {
            if (_doubleArrayValues.TryGetValue(key, out var value))
            {
                return value.GetValue();
            }

            return new double[0];
        }

        private void AddString(string key, long timestamp, object value)
        {
            _stringValues.TryAdd(key, new TopicValue<string>());
            _stringValues[key].AddValue(timestamp, Convert.ToString(value));
        }

        private void AddInteger(string key, long timestamp, object value)
        {
            _longValues.TryAdd(key, new TopicValue<long>());
            _longValues[key].AddValue(timestamp, Convert.ToInt64(value));
        }
        
        private void AddDouble(string key, long timestamp, object value)
        {
            _doubleValues.TryAdd(key, new TopicValue<double>());
            _doubleValues[key].AddValue(timestamp, Convert.ToDouble(value));
        }
        
        private void AddDoubleArray(string key, long timestamp, object value)
        {
            _doubleArrayValues.TryAdd(key, new TopicValue<double[]>());
            if (value is object[] arr)
            {
                _doubleArrayValues[key].AddValue(timestamp, arr.Cast<double>().ToArray());
            }
            
        }
    }
}