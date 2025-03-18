using System;
using QuestNav.Core;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Network
{
    /// <summary>
    /// Interface for heartbeat management.
    /// </summary>
    public interface IHeartbeatManager
    {
        /// <summary>
        /// Initializes the heartbeat manager with a connection.
        /// </summary>
        /// <param name="networkConnection">The network connection to use for heartbeat messages</param>
        void Initialize(INetworkTableConnection networkConnection);

        /// <summary>
        /// Manages the heartbeat system by sending and checking for responses.
        /// </summary>
        void ManageHeartbeat();

        /// <summary>
        /// Resets the heartbeat system state.
        /// </summary>
        void ResetHeartbeatState();
    }

    /// <summary>
    /// Manages the heartbeat system for detecting and handling connection issues.
    /// Implements a ping/pong mechanism to detect zombie connections with the robot.
    /// </summary>
    public class HeartbeatManager : MonoBehaviour, IHeartbeatManager
    {
        #region Fields
        /// <summary>
        /// Reference to the network connection
        /// </summary>
        private INetworkTableConnection networkConnection;

        /// <summary>
        /// Heartbeat counter value that increments with each successful exchange
        /// </summary>
        private int heartbeatCounter = 1;

        /// <summary>
        /// Time when the last heartbeat was sent to the robot
        /// </summary>
        private float lastHeartbeatSentTime = 0;

        /// <summary>
        /// Time when the last successful heartbeat response was received
        /// </summary>
        private float lastHeartbeatResponseTime = 0;

        /// <summary>
        /// Flag indicating whether we're waiting for a heartbeat response
        /// </summary>
        private bool heartbeatResponsePending = false;

        /// <summary>
        /// Counter for consecutive failed heartbeats
        /// </summary>
        private int consecutiveFailedHeartbeats = 0;

        // Constants are now defined in QuestNavConstants.Heartbeat
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the heartbeat manager with a connection.
        /// </summary>
        /// <param name="networkConnection">The network connection to use for heartbeat messages</param>
        public void Initialize(INetworkTableConnection networkConnection)
        {
            this.networkConnection = networkConnection;
            ResetHeartbeatState();
        }

        /// <summary>
        /// Manages the heartbeat system to detect zombie connections.
        /// Called in LateUpdate when connection is established.
        /// </summary>
        public void ManageHeartbeat()
        {
            // Only run if connected
            if (networkConnection == null || !networkConnection.IsConnected) return;
            
            float currentTime = Time.time;
            
            // Check if previous heartbeat timed out
            if (heartbeatResponsePending)
            {
                if (currentTime - lastHeartbeatSentTime > QuestNavConstants.Heartbeat.HEARTBEAT_TIMEOUT)
                {
                    // Heartbeat timed out - increment failure counter
                    consecutiveFailedHeartbeats++;
                    heartbeatResponsePending = false;
                    QueuedLogger.LogWarning($"[QuestNav] Heartbeat #{heartbeatCounter} timed out. Failed count: {consecutiveFailedHeartbeats}");
                    
                    // Force reconnection if too many consecutive failures
                    if (consecutiveFailedHeartbeats >= QuestNavConstants.Heartbeat.MAX_FAILED_HEARTBEATS)
                    {
                        QueuedLogger.LogWarning("[QuestNav] Too many failed heartbeats, forcing reconnection");
                        networkConnection.ForceReconnection();
                        return;
                    }
                }
                else
                {
                    // Check for heartbeat response if still waiting
                    CheckHeartbeatResponse();
                }
            }
            
            // Send new heartbeat if interval elapsed and not waiting for response
            if (!heartbeatResponsePending && currentTime - lastHeartbeatSentTime > QuestNavConstants.Heartbeat.HEARTBEAT_INTERVAL)
            {
                SendHeartbeat();
            }
        }

        /// <summary>
        /// Resets the heartbeat system state.
        /// </summary>
        public void ResetHeartbeatState()
        {
            heartbeatCounter = 1;
            heartbeatResponsePending = false;
            consecutiveFailedHeartbeats = 0;
            lastHeartbeatSentTime = Time.time;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sends a heartbeat value to the robot and starts waiting for response
        /// </summary>
        private void SendHeartbeat()
        {
            try
            {
                networkConnection.PublishValue(QuestNavConstants.Topics.HEARTBEAT_TO_ROBOT, (double)heartbeatCounter);
                lastHeartbeatSentTime = Time.time;
                heartbeatResponsePending = true;
                QueuedLogger.Log($"[QuestNav] Sent heartbeat #{heartbeatCounter}");
            }
            catch (Exception ex)
            {
                QueuedLogger.LogWarning($"[QuestNav] Error sending heartbeat: {ex.Message}");
                // Don't set heartbeatResponsePending to true if we couldn't send
            }
        }

        /// <summary>
        /// Checks for a heartbeat response from the robot
        /// </summary>
        private void CheckHeartbeatResponse()
        {
            try
            {
                double response = networkConnection.GetDouble(QuestNavConstants.Topics.HEARTBEAT_FROM_ROBOT);
                
                if ((int)response == heartbeatCounter)
                {
                    // Valid response received
                    heartbeatResponsePending = false;
                    lastHeartbeatResponseTime = Time.time;
                    consecutiveFailedHeartbeats = 0;
                    QueuedLogger.Log($"[QuestNav] Received heartbeat response #{heartbeatCounter}");
                    
                    // Increment heartbeat counter for next round (with overflow protection)
                    heartbeatCounter++;
                    if (heartbeatCounter > 1000000) heartbeatCounter = 1;
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogWarning($"[QuestNav] Error checking heartbeat response: {ex.Message}");
                // Don't reset heartbeatResponsePending here - let the timeout handle it
            }
        }
        #endregion
    }
}