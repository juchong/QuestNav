using QuestNav.Commands;
using QuestNav.Network;
using QuestNav.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestNav.Core
{
    /// <summary>
    /// Main controller class for QuestNav application.
    /// Manages streaming of VR motion data to a FRC robot through NetworkTables.
    /// Orchestrates pose tracking, reset operations, and network communication.
    /// </summary>
    public class QuestNav : MonoBehaviour
    {
        #region Fields
        /// <summary>
        /// Current frame index from Unity's Time.frameCount
        /// </summary>
        public int frameIndex;

        /// <summary>
        /// Current timestamp from Unity's Time.time
        /// </summary>
        public double timeStamp;

        /// <summary>
        /// Current position of the VR headset
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Current rotation of the VR headset as a Quaternion
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// Current rotation of the VR headset in Euler angles
        /// </summary>
        public Vector3 eulerAngles;

        /// <summary>
        /// Reference to the OVR Camera Rig for tracking
        /// </summary>
        public OVRCameraRig cameraRig;

        /// <summary>
        /// Input field for team number entry
        /// </summary>
        public TMP_InputField teamInput;

        /// <summary>
        /// IP address text
        /// </summary>
        public TMP_Text ipAddressText;

        /// <summary>
        /// ConState text
        /// </summary>
        public TMP_Text conStateText;

        /// <summary>
        /// Button to update team number
        /// </summary>
        public Button teamUpdateButton;

        /// <summary>
        /// Reference to the VR camera transform
        /// </summary>
        [SerializeField] 
        public Transform vrCamera;

        /// <summary>
        /// Reference to the VR camera root transform
        /// </summary>
        [SerializeField] 
        public Transform vrCameraRoot;

        /// <summary>
        /// Reference to the reset position transform
        /// </summary>
        [SerializeField] 
        public Transform resetTransform;

        /// <summary>
        /// Current battery percentage of the device
        /// </summary>
        private float batteryPercent = 0;

        /// <summary>
        /// Counter for display update delay
        /// </summary>
        private int delayCounter = 0;

        // Using display frequency constant from QuestNavConstants

        #region Component References
        /// <summary>
        /// Reference to the network table connection component
        /// </summary>
        [SerializeField]
        private NetworkTableConnection networkConnection;

        /// <summary>
        /// Reference to the heartbeat manager component
        /// </summary>
        [SerializeField]
        private HeartbeatManager heartbeatManager;

        /// <summary>
        /// Reference to the command processor component
        /// </summary>
        [SerializeField]
        private CommandProcessor commandProcessor;

        /// <summary>
        /// Reference to the UI manager component
        /// </summary>
        [SerializeField]
        private UIManager uiManager;
        #endregion
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the connection and UI components
        /// </summary>
        void Start()
        {
            // Set Oculus display frequency
            OVRPlugin.systemDisplayFrequency = QuestNavConstants.Display.DISPLAY_FREQUENCY;
            
            // Initialize UI manager
            uiManager.Initialize(teamInput, ipAddressText, conStateText, teamUpdateButton, networkConnection);
            
            // Initialize command processor
            commandProcessor.Initialize(networkConnection, vrCamera, vrCameraRoot, resetTransform);
            
            // Initialize heartbeat manager
            heartbeatManager.Initialize(networkConnection);
            
            // Start connection to robot
            networkConnection.ConnectToRobot();
        }

        /// <summary>
        /// Handles frame updates for data publishing and command processing
        /// </summary>
        void LateUpdate()
        {
            // Check for connection attempt timeout to prevent zombie state
            if (!networkConnection.IsConnected)
            {
                // Only start a new connection attempt if not already trying
                if (!networkConnection.IsConnectionAttemptInProgress)
                {
                    networkConnection.HandleDisconnectedState();
                }
                return;
            }

            // Connected - manage heartbeat, publish data, and process commands
            if (networkConnection.IsConnected)
            {
                // Manage heartbeat to detect zombie connections
                heartbeatManager.ManageHeartbeat();
                
                // Collect and publish current frame data
                UpdateFrameData();
                networkConnection.PublishFrameData(frameIndex, timeStamp, position, rotation, eulerAngles, batteryPercent);
                
                // Process robot commands
                commandProcessor.ProcessCommands();
            }

            // Update UI periodically
            if (delayCounter >= (int)QuestNavConstants.Display.DISPLAY_FREQUENCY)
            {
                uiManager.UpdateIPAddressText();
                uiManager.UpdateConStateText();
                delayCounter = 0;
            }
            else
            {
                delayCounter++;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the current frame data from the VR headset
        /// </summary>
        private void UpdateFrameData()
        {
            frameIndex = Time.frameCount;
            timeStamp = Time.time;
            position = cameraRig.centerEyeAnchor.position;
            rotation = cameraRig.centerEyeAnchor.rotation;
            eulerAngles = cameraRig.centerEyeAnchor.eulerAngles;
            batteryPercent = SystemInfo.batteryLevel * 100;
        }
        #endregion
    }
}