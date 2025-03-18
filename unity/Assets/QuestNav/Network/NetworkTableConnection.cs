using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Network
{
    /// <summary>
    /// Interface for NetworkTables connection management.
    /// </summary>
    public interface INetworkTableConnection
    {
        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether a connection attempt is currently in progress.
        /// </summary>
        bool IsConnectionAttemptInProgress { get; }

        /// <summary>
        /// Gets the current connection state message.
        /// </summary>
        string ConnectionStateMessage { get; }

        /// <summary>
        /// Gets the current IP address.
        /// </summary>
        string IPAddress { get; }

        /// <summary>
        /// Gets the NetworkTables client instance.
        /// </summary>
        Nt4Source DataSink { get; }

        /// <summary>
        /// Initiates a connection to the robot.
        /// </summary>
        void ConnectToRobot();

        /// <summary>
        /// Publishes frame data to NetworkTables.
        /// </summary>
        /// <param name="frameIndex">Current frame index</param>
        /// <param name="timeStamp">Current timestamp</param>
        /// <param name="position">Current position</param>
        /// <param name="rotation">Current rotation</param>
        /// <param name="eulerAngles">Current euler angles</param>
        /// <param name="batteryPercent">Current battery percentage</param>
        void PublishFrameData(int frameIndex, double timeStamp, Vector3 position, Quaternion rotation, Vector3 eulerAngles, float batteryPercent);

        /// <summary>
        /// Publishes a value to NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to publish to</param>
        /// <param name="value">The value to publish</param>
        void PublishValue(string topic, object value);

        /// <summary>
        /// Gets a double value from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The double value</returns>
        double GetDouble(string topic);

        /// <summary>
        /// Gets a long value from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The long value</returns>
        long GetLong(string topic);

        /// <summary>
        /// Gets a double array from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The double array</returns>
        double[] GetDoubleArray(string topic);

        /// <summary>
        /// Forces a reconnection.
        /// </summary>
        void ForceReconnection();

        /// <summary>
        /// Updates the team number.
        /// </summary>
        /// <param name="teamNumber">The team number</param>
        void UpdateTeamNumber(string teamNumber);

        /// <summary>
        /// Handles the disconnected state.
        /// </summary>
        void HandleDisconnectedState();

        /// <summary>
        /// Publishes the required topics.
        /// </summary>
        void PublishTopics();
    }

    /// <summary>
    /// Manages NetworkTables connections for communication with an FRC robot.
    /// </summary>
    public class NetworkTableConnection : MonoBehaviour, INetworkTableConnection
    {
        #region Fields
        /// <summary>
        /// NetworkTables connection for FRC data communication
        /// </summary>
        private Nt4Source frcDataSink = null;

        /// <summary>
        /// Current team number
        /// </summary>
        private string teamNumber = "";

        /// <summary>
        /// Current log message
        /// </summary>
        private string conStateMessage = "No Message";

        #region NetworkTables Configuration
        /// <summary>
        /// Current IP address for connection
        /// </summary>
        private string ipAddress = "";

        /// <summary>
        /// Counter for connection retry delay
        /// </summary>
        private int delayCounter = 0;

        /// <summary>
        /// Reconnection delay variable for delaying failed connection attempts.
        /// This value changes dynamically based on the number of failed attempts.
        /// </summary>
        private float reconnectDelay = QuestNavConstants.Network.DEFAULT_RECONNECT_DELAY;

        /// <summary>
        /// List of failed connection attempt candidates (prevents excessive retries on failed addresses)
        /// </summary>
        private Dictionary<string, float> failedCandidates = new Dictionary<string, float>();

        /// <summary>
        /// Coroutine definition to enable controlling the connection process
        /// </summary>
        private Coroutine _connectionCoroutine;

        /// <summary>
        /// used to only set up the coroutine once in async
        /// </summary>
        private bool connectionAttempt = false;

        /// <summary>
        /// used to make sure connection completed on coroutine
        /// </summary>
        private bool connectionAttemptCompleted = true;

        /// <summary>
        /// Timestamp when the connection attempt started for timeout calculation
        /// </summary>
        private float connectionAttemptStartTime = 0;
        #endregion
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        public bool IsConnected => frcDataSink != null && frcDataSink.Client.Connected();

        /// <summary>
        /// Gets whether a connection attempt is currently in progress.
        /// </summary>
        public bool IsConnectionAttemptInProgress => connectionAttempt;

        /// <summary>
        /// Gets the current connection state message.
        /// </summary>
        public string ConnectionStateMessage => conStateMessage;

        /// <summary>
        /// Gets the current IP address.
        /// </summary>
        public string IPAddress => ipAddress;

        /// <summary>
        /// Gets the NetworkTables client instance.
        /// </summary>
        public Nt4Source DataSink => frcDataSink;
        #endregion

        #region Network Connection Methods
        /// <summary>
        /// Called to start the connection process.
        /// Manages connection flags and starts the coroutine for connection.
        /// </summary>
        public void ConnectToRobot()
        {
            // Don't start multiple connection attempts
            if (connectionAttempt)
            {
                QueuedLogger.LogWarning("[QuestNav] Connection attempt already in progress, skipping new request");
                return;
            }
            
            // Set connection flags and timestamp for timeout tracking
            connectionAttempt = true;
            connectionAttemptCompleted = false;
            connectionAttemptStartTime = Time.time;
            
            // Cancel any existing connection coroutine
            if (_connectionCoroutine != null)
            {
                StopCoroutine(_connectionCoroutine);
            }
            
            // Start a new connection attempt
            _connectionCoroutine = StartCoroutine(ConnectionCoroutineWrapper());
        }

        /// <summary>
        /// A coroutine wrapper that waits until the asynchronous connection method completes.
        /// This wrapper enables setting timeout guardbands for the connection process.
        /// </summary>
        private IEnumerator ConnectionCoroutineWrapper()
        {
            var connectionTask = AttemptConnectionAsync();
            
            while (!connectionTask.IsCompleted)
            {
                yield return null;
            }
            
            // Check if the task completed successfully
            if (connectionTask.IsFaulted)
            {
                QueuedLogger.LogError($"[QuestNav] Connection attempt failed with exception: {connectionTask.Exception}");
                connectionAttempt = false;  // Reset flag to allow new attempts
            }
        }

        /// <summary>
        /// Asynchronously attempts to connect to one of the candidate addresses.
        /// Uses async DNS resolution and wraps blocking calls in Task.Run to avoid blocking the main Unity thread.
        /// Will try candidates until successful or all fail, then retry with exponential backoff.
        /// </summary>
        private async Task AttemptConnectionAsync()
        {
            bool connectionEstablished = false;
            List<string> candidateAddresses = new List<string>()
            {
                generateIP(),
                "172.22.11.2",
                $"roboRIO-{teamNumber}-FRC.local",
                $"roboRIO-{teamNumber}-FRC.lan",
                $"roboRIO-{teamNumber}-FRC.frc-field.local"
            };

            while (!connectionEstablished)
            {
                // Check if the network is reachable.
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    QueuedLogger.LogWarning($"[QuestNav] Network not reachable. Waiting {QuestNavConstants.Network.UNREACHABLE_NETWORK_DELAY} seconds before reattempting.");
                    conStateMessage = "net no reach waiting";
                    await Task.Delay(QuestNavConstants.Network.UNREACHABLE_NETWORK_DELAY * 1000);
                    continue;
                }

                StringBuilder cycleLog = new StringBuilder();
                cycleLog.AppendLine("[QuestNav] Connection attempt cycle:");
                conStateMessage = "connection attempt cycle";

                foreach (string candidate in candidateAddresses)
                {
                    // Skip a candidate if it failed recently.
                    if (failedCandidates.ContainsKey(candidate) && (Time.time - failedCandidates[candidate] < QuestNavConstants.Network.CANDIDATE_FAILURE_COOLDOWN))
                    {
                        cycleLog.AppendLine($"Skipping candidate {candidate} (failed less than {QuestNavConstants.Network.CANDIDATE_FAILURE_COOLDOWN} seconds ago).");
                        conStateMessage = "skipping " + candidate + " cool " + QuestNavConstants.Network.CANDIDATE_FAILURE_COOLDOWN;
                        continue;
                    }
                    else
                    {
                        failedCandidates.Remove(candidate);
                        conStateMessage = "removed candidate " + candidate;
                    }

                    string resolvedAddress = candidate;
                    try
                    {
                        // Use asynchronous DNS resolution.
                        IPHostEntry hostEntry = await Dns.GetHostEntryAsync(candidate);
                        IPAddress[] ipv4Addresses = hostEntry.AddressList
                            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                            .ToArray();
                        if (ipv4Addresses.Length > 0)
                        {
                            resolvedAddress = ipv4Addresses[0].ToString();
                        }
                        else
                        {
                            cycleLog.AppendLine($"DNS lookup returned no IPv4 for candidate '{candidate}'.");
                            conStateMessage = "no ipv4 for " + candidate;
                            failedCandidates[candidate] = Time.time;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        cycleLog.AppendLine($"DNS resolution failed for candidate '{candidate}': {ex.Message}");
                        conStateMessage = "dns failed for " + candidate;
                        failedCandidates[candidate] = Time.time;
                        continue;
                    }

                    cycleLog.AppendLine($"Attempting connection to {resolvedAddress}...");
                    conStateMessage = "attempting " + resolvedAddress;

                    try
                    {
                        // Wrap the potentially blocking connection call in Task.Run.
                        var sink = await Task.Run(() =>
                        {
                            return new Nt4Source(QuestNavConstants.Network.APP_NAME, resolvedAddress, QuestNavConstants.Network.SERVER_PORT);
                        });

                        if (sink.Client.Connected())
                        {
                            ipAddress = resolvedAddress; // Cache the working address.
                            frcDataSink = sink;
                            cycleLog.AppendLine($"Connected successfully to {resolvedAddress}.");
                            conStateMessage = "connected to " + resolvedAddress;
                            connectionEstablished = true;
                            connectionAttempt = false;
                            connectionAttemptCompleted = true;
                            break;
                        }
                        else
                        {
                            cycleLog.AppendLine($"Connection attempt to {resolvedAddress} did not succeed.");
                            conStateMessage = "conn failed to " + resolvedAddress;
                        }
                    }
                    catch (Exception ex)
                    {
                        cycleLog.AppendLine($"Connection attempt failed for {resolvedAddress}: {ex.Message}");
                        conStateMessage = "conn failed " + resolvedAddress + " " + ex.Message;
                    }
                }

                // Handle a failed connection
                if (!connectionEstablished)
                {
                    cycleLog.AppendLine($"Could not establish a connection with any candidate addresses. Reattempting in {reconnectDelay} second(s)...");
                    conStateMessage = "no conn to any candidates trying in " + reconnectDelay;
                    QueuedLogger.Log(cycleLog.ToString(), QueuedLogger.LogLevel.Warning);
                    await Task.Delay((int)(reconnectDelay * 1000));
                    reconnectDelay = Mathf.Min(reconnectDelay * 2, QuestNavConstants.Network.MAX_RECONNECT_DELAY);
                }
            }

            // Reset delay on success.
            reconnectDelay = QuestNavConstants.Network.DEFAULT_RECONNECT_DELAY;
            QueuedLogger.Log("[QuestNav] Connection established. Publishing topics.");
            conStateMessage = "connected - publishing";
            PublishTopics();
        }

        /// <summary>
        /// Handles reconnection when connection is lost.
        /// Centralizes disconnection and reconnection logic.
        /// </summary>
        public void HandleDisconnectedState()
        {
            // Prevent multiple reconnection attempts
            if (connectionAttempt)
            {
                return;
            }
            
            QueuedLogger.Log("[QuestNav] Robot disconnected. Resetting connection and attempting to reconnect...");
            conStateMessage = "robot disconnected - retrying";
            
            // Safely disconnect if we have a connection
            if (frcDataSink != null)
            {
                try
                {
                    frcDataSink.Client.Disconnect();
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogWarning($"[QuestNav] Error disconnecting: {ex.Message}");
                }
                frcDataSink = null;
            }
            
            // Start a fresh connection attempt
            ConnectToRobot();
        }

        /// <summary>
        /// Publishes and subscribes to required NetworkTables topics
        /// Includes heartbeat topics for connection monitoring
        /// </summary>
        public void PublishTopics()
        {
            // Standard QuestNav topics
            frcDataSink.PublishTopic(QuestNavConstants.Topics.MISO, "int");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.FRAME_COUNT, "int");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.TIMESTAMP, "double");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.POSITION, "float[]");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.QUATERNION, "float[]");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.EULER_ANGLES, "float[]");
            frcDataSink.PublishTopic(QuestNavConstants.Topics.BATTERY_PERCENT, "double");
            frcDataSink.Subscribe(QuestNavConstants.Topics.MOSI, 0.1, false, false, false);
            frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_POSITION, 0.1, false, false, false);
            frcDataSink.Subscribe(QuestNavConstants.Topics.INIT_EULER_ANGLES, 0.1, false, false, false);
            frcDataSink.Subscribe(QuestNavConstants.Topics.RESET_POSE, 0.1, false, false, false);
            
            // Heartbeat system topics
            frcDataSink.PublishTopic(QuestNavConstants.Topics.HEARTBEAT_TO_ROBOT, "double");
            frcDataSink.Subscribe(QuestNavConstants.Topics.HEARTBEAT_FROM_ROBOT, 0.1, false, false, false);
        }

        /// <summary>
        /// Forces a reconnection due to heartbeat failure
        /// Resets connection state and begins a new connection attempt
        /// </summary>
        public void ForceReconnection()
        {
            QueuedLogger.Log("[QuestNav] Forcing reconnection due to heartbeat failure");
            
            // Disconnect existing connection
            if (frcDataSink != null)
            {
                try
                {
                    frcDataSink.Client.Disconnect();
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogWarning($"[QuestNav] Error disconnecting: {ex.Message}");
                }
                frcDataSink = null;
            }
            
            // Reset connection state flags to enable reconnection
            connectionAttempt = false;
            connectionAttemptCompleted = true;
            
            // Initiate reconnection
            ConnectToRobot();
        }

        /// <summary>
        /// Updates the team number and restarts the connection.
        /// </summary>
        /// <param name="teamNumber">The new team number</param>
        public void UpdateTeamNumber(string teamNumber)
        {
            QueuedLogger.Log("[QuestNav] Updating Team Number");
            this.teamNumber = teamNumber;
            PlayerPrefs.SetString("TeamNumber", teamNumber);
            PlayerPrefs.Save();

            // Clear cached IP and candidate failure data.
            ipAddress = "";
            failedCandidates.Clear();

            // If a connection coroutine is already running, stop it.
            if (_connectionCoroutine != null)
            {
                StopCoroutine(_connectionCoroutine);
                _connectionCoroutine = null;
            }

            // Disconnect existing connection, if any.
            if (frcDataSink != null)
            {
                if (frcDataSink.Client.Connected())
                {
                    frcDataSink.Client.Disconnect();
                }
                frcDataSink = null;
            }

            // Reset connection state and restart connection process
            connectionAttempt = false;
            connectionAttemptCompleted = true;
            
            // Restart the asynchronous connection process.
            ConnectToRobot();
        }
        #endregion

        #region Data Publishing Methods
        /// <summary>
        /// Publishes current frame data to NetworkTables
        /// </summary>
        public void PublishFrameData(int frameIndex, double timeStamp, Vector3 position, Quaternion rotation, Vector3 eulerAngles, float batteryPercent)
        {
            // Check if connection is established before publishing data
            if (frcDataSink == null || !frcDataSink.Client.Connected())
            {
                return; // Exit early if connection isn't established
            }
            
            frcDataSink.PublishValue(QuestNavConstants.Topics.FRAME_COUNT, frameIndex);
            frcDataSink.PublishValue(QuestNavConstants.Topics.TIMESTAMP, timeStamp);
            frcDataSink.PublishValue(QuestNavConstants.Topics.POSITION, position.ToArray());
            frcDataSink.PublishValue(QuestNavConstants.Topics.QUATERNION, rotation.ToArray());
            frcDataSink.PublishValue(QuestNavConstants.Topics.EULER_ANGLES, eulerAngles.ToArray());
            frcDataSink.PublishValue(QuestNavConstants.Topics.BATTERY_PERCENT, batteryPercent);
        }

        /// <summary>
        /// Publishes a value to NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to publish to</param>
        /// <param name="value">The value to publish</param>
        public void PublishValue(string topic, object value)
        {
            // Check if connection is established before publishing data
            if (frcDataSink == null || !frcDataSink.Client.Connected())
            {
                return; // Exit early if connection isn't established
            }
            
            frcDataSink.PublishValue(topic, value);
        }

        /// <summary>
        /// Gets a double value from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The double value</returns>
        public double GetDouble(string topic)
        {
            // Check if connection is established before getting data
            if (frcDataSink == null || !frcDataSink.Client.Connected())
            {
                return 0.0; // Return default value if connection isn't established
            }
            
            return frcDataSink.GetDouble(topic);
        }

        /// <summary>
        /// Gets a long value from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The long value</returns>
        public long GetLong(string topic)
        {
            // Check if connection is established before getting data
            if (frcDataSink == null || !frcDataSink.Client.Connected())
            {
                return 0; // Return default value if connection isn't established
            }
            
            return frcDataSink.GetLong(topic);
        }

        /// <summary>
        /// Gets a double array from NetworkTables.
        /// </summary>
        /// <param name="topic">The topic to read from</param>
        /// <returns>The double array</returns>
        public double[] GetDoubleArray(string topic)
        {
            // Check if connection is established before getting data
            if (frcDataSink == null || !frcDataSink.Client.Connected())
            {
                return new double[0]; // Return empty array if connection isn't established
            }
            
            return frcDataSink.GetDoubleArray(topic);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Generates the IP address for the roboRIO based on team number
        /// </summary>
        /// <returns>The formatted IP address string</returns>
        private string generateIP()
        {
            if (teamNumber.Length >= 1)
            {
                string tePart = teamNumber.Length > 2 ? teamNumber.Substring(0, teamNumber.Length - 2) : "0";
                string amPart = teamNumber.Length > 2 ? teamNumber.Substring(teamNumber.Length - 2) : teamNumber;
                return QuestNavConstants.Network.SERVER_ADDRESS_FORMAT.Replace("TE", tePart).Replace("AM", amPart);
            }
            else
            {
                return QuestNavConstants.Network.ALTERNATE_ADDRESS;
            }
        }
        #endregion
    }
}