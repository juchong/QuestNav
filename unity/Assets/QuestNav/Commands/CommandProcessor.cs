using System;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for command processing.
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Gets or sets a value indicating whether a reset operation is in progress.
        /// </summary>
        bool ResetInProgress { get; set; }

        /// <summary>
        /// Initializes the command processor with required dependencies.
        /// </summary>
        /// <param name="networkConnection">The network connection to use for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        void Initialize(INetworkTableConnection networkConnection, Transform vrCamera, Transform vrCameraRoot, Transform resetTransform);

        /// <summary>
        /// Processes commands received from the robot.
        /// </summary>
        void ProcessCommands();
    }

    /// <summary>
    /// Processes commands received from the robot and performs appropriate actions.
    /// Handles pose tracking, reset operations, and command responses.
    /// </summary>
    public class CommandProcessor : MonoBehaviour, ICommandProcessor
    {
        #region Fields
        /// <summary>
        /// Reference to the network connection
        /// </summary>
        private INetworkTableConnection networkConnection;

        /// <summary>
        /// Reference to the VR camera transform
        /// </summary>
        private Transform vrCamera;

        /// <summary>
        /// Reference to the VR camera root transform
        /// </summary>
        private Transform vrCameraRoot;

        /// <summary>
        /// Reference to the reset position transform
        /// </summary>
        private Transform resetTransform;

        /// <summary>
        /// Current command received from the robot
        /// </summary>
        private long command = 0;

        /// <summary>
        /// Flag indicating if a reset operation is in progress
        /// </summary>
        private bool resetInProgress = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether a reset operation is in progress.
        /// </summary>
        public bool ResetInProgress
        {
            get { return resetInProgress; }
            set { resetInProgress = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the command processor with required dependencies.
        /// </summary>
        /// <param name="networkConnection">The network connection to use for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public void Initialize(INetworkTableConnection networkConnection, Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            this.networkConnection = networkConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
        }

        /// <summary>
        /// Processes commands received from the robot.
        /// </summary>
        public void ProcessCommands()
        {
            command = networkConnection.GetLong(QuestNavConstants.Topics.MOSI);

            if (resetInProgress && command == 0)
            {
                resetInProgress = false;
                QueuedLogger.Log("[QuestNav] Reset operation completed");
                return;
            }

            switch (command)
            {
                case QuestNavConstants.Commands.HEADING_RESET:
                    if (!resetInProgress)
                    {
                        QueuedLogger.Log("[QuestNav] Received heading reset request, initiating recenter...");
                        RecenterPlayer();
                        resetInProgress = true;
                    }
                    break;
                case QuestNavConstants.Commands.POSE_RESET:
                    if (!resetInProgress)
                    {
                        QueuedLogger.Log("[QuestNav] Received pose reset request, initiating reset...");
                        InitiatePoseReset();
                        QueuedLogger.Log("[QuestNav] Processing pose reset request.");
                        resetInProgress = true;
                    }
                    break;
                case QuestNavConstants.Commands.PING:
                    QueuedLogger.Log("[QuestNav] Ping received, responding...");
                    networkConnection.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.Commands.PING_RESPONSE);
                    break;
                default:
                    if (!resetInProgress)
                    {
                        networkConnection.PublishValue(QuestNavConstants.Topics.MISO, 0);
                    }
                    break;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initiates a pose reset based on received NetworkTables data
        /// </summary>
        private void InitiatePoseReset()
        {
            try {
                // Use constants for retry logic and field dimensions from QuestNavConstants

                double[] resetPose = null;
                bool success = false;
                int attemptCount = 0;

                // Attempt to read pose data from NetworkTables with retry logic
                for (int i = 0; i < QuestNavConstants.Field.MAX_POSE_READ_RETRIES && !success; i++)
                {
                    attemptCount++;
                    // Add delay between retries to allow for network latency
                    if (i > 0) {
                        System.Threading.Thread.Sleep((int)QuestNavConstants.Field.POSE_RETRY_DELAY_MS);
                        QueuedLogger.Log($"[QuestNav] Attempt {attemptCount} of {QuestNavConstants.Field.MAX_POSE_READ_RETRIES}...");
                    }

                    QueuedLogger.Log($"[QuestNav] Reading NetworkTables Values (Attempt {attemptCount}):");

                    // Read the pose array from NetworkTables
                    // Format: [X, Y, Rotation] in FRC field coordinates
                    resetPose = networkConnection.GetDoubleArray(QuestNavConstants.Topics.RESET_POSE);

                    // Validate pose data format and field boundaries
                    if (resetPose != null && resetPose.Length == 3) {
                        // Check if pose is within valid field boundaries
                        if (resetPose[0] < 0 || resetPose[0] > QuestNavConstants.Field.FIELD_LENGTH ||
                            resetPose[1] < 0 || resetPose[1] > QuestNavConstants.Field.FIELD_WIDTH) {
                            QueuedLogger.LogWarning($"[QuestNav] Reset pose outside field boundaries: X:{resetPose[0]:F3} Y:{resetPose[1]:F3}");
                            continue;
                        }
                        success = true;
                        QueuedLogger.Log($"[QuestNav] Successfully read reset pose values on attempt {attemptCount}");
                    }

                    QueuedLogger.Log($"[QuestNav] Values (Attempt {attemptCount}): X:{resetPose?[0]:F3} Y:{resetPose?[1]:F3} Rot:{resetPose?[2]:F3}");
                }

                // Exit if we couldn't get valid pose data
                if (!success) {
                    QueuedLogger.LogWarning($"[QuestNav] Failed to read valid reset pose values after {attemptCount} attempts");
                    return;
                }

                // Extract pose components from the array
                double resetX = resetPose[0];          // FRC X coordinate (along length of field)
                double resetY = resetPose[1];          // FRC Y coordinate (along width of field)
                double resetRotation = resetPose[2];   // FRC rotation (CCW positive)

                // Normalize rotation to -180 to 180 degrees range
                while (resetRotation > 180) resetRotation -= 360;
                while (resetRotation < -180) resetRotation += 360;

                QueuedLogger.Log($"[QuestNav] Starting pose reset - Target: FRC X:{resetX:F2} Y:{resetY:F2} Rot:{resetRotation:F2}°");

                // Store current VR camera state for reference
                Vector3 currentCameraPos = vrCamera.position;
                Quaternion currentCameraRot = vrCamera.rotation;

                QueuedLogger.Log($"[QuestNav] Before reset - Camera Pos:{currentCameraPos:F3} Rot:{currentCameraRot.eulerAngles:F3}");

                // Convert FRC coordinates to Unity coordinates:
                // Unity uses a left-handed coordinate system with Y as the vertical axis (aligned with gravity)
                // FRC uses a right-handed coordinate system with Z as the vertical axis (aligned with gravity)
                // See this page for more Unity details: https://docs.unity3d.com/Manual/QuaternionAndEulerRotationsInUnity.html
                // See this page for more FRC details: https://docs.wpilib.org/en/stable/docs/software/basic-programming/coordinate-system.html

                // - Unity X = -FRC Y (right is positive in Unity)
                // - Unity Y = kept unchanged (height)
                // - Unity Z = FRC X (forward is positive in both)
                Vector3 targetUnityPosition = new Vector3(
                    (float)(-resetY),         // Negate Y for Unity's coordinate system
                    currentCameraPos.y,       // Maintain current height
                    (float)resetX            // FRC X maps directly to Unity Z
                );

                // Calculate the position difference we need to move
                Vector3 positionDelta = targetUnityPosition - currentCameraPos;

                // Convert FRC rotation to Unity rotation:
                // - Unity uses clockwise positive rotation around Y
                // - FRC uses counterclockwise positive rotation
                float targetUnityYaw = (float)(-resetRotation);  // Negate for Unity's rotation direction
                float currentYaw = currentCameraRot.eulerAngles.y;

                // Normalize both angles to 0-360 range for proper delta calculation
                while (currentYaw < 0) currentYaw += 360;
                while (targetUnityYaw < 0) targetUnityYaw += 360;

                // Calculate rotation delta and normalize to -180 to 180 range
                float yawDelta = targetUnityYaw - currentYaw;
                if (yawDelta > 180) yawDelta -= 360;
                if (yawDelta < -180) yawDelta += 360;

                QueuedLogger.Log($"[QuestNav] Calculated adjustments - Position delta:{positionDelta:F3} Rotation delta:{yawDelta:F3}°");
                QueuedLogger.Log($"[QuestNav] Target Unity Position: {targetUnityPosition:F3}");

                // Store the original offset between camera and its root
                // This helps maintain proper VR tracking space
                Vector3 cameraOffset = vrCamera.position - vrCameraRoot.position;

                // First rotate the root around the camera position
                // This maintains the camera's position while changing orientation
                vrCameraRoot.RotateAround(vrCamera.position, Vector3.up, yawDelta);

                // Then apply the position adjustment to achieve target position
                vrCameraRoot.position += positionDelta;

                // Log final position and rotation for verification
                QueuedLogger.Log($"[QuestNav] After reset - Camera Pos:{vrCamera.position:F3} Rot:{vrCamera.rotation.eulerAngles:F3}");

                // Calculate and check position error to ensure accuracy
                float posError = Vector3.Distance(vrCamera.position, targetUnityPosition);
                QueuedLogger.Log($"[QuestNav] Position error after reset: {posError:F3}m");

                // Warn if position error is larger than expected threshold
                if (posError > QuestNavConstants.Field.POSITION_ERROR_THRESHOLD) {
                    QueuedLogger.LogWarning($"[QuestNav] Large position error detected!");
                }

                networkConnection.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.Commands.POSE_RESET_SUCCESS);
            }
            catch (Exception e) {
                QueuedLogger.LogError($"[QuestNav] Error during pose reset: {e.Message}");
                QueuedLogger.LogException(e);
                networkConnection.PublishValue(QuestNavConstants.Topics.MISO, 0);
                resetInProgress = false;
            }
        }

        /// <summary>
        /// Recenters the player's view by adjusting the VR camera root transform to match the reset transform.
        /// Similar to the effect of long-pressing the Oculus button.
        /// </summary>
        private void RecenterPlayer()
        {
            try {
                float rotationAngleY = vrCamera.rotation.eulerAngles.y - resetTransform.rotation.eulerAngles.y;

                vrCameraRoot.transform.Rotate(0, -rotationAngleY, 0);

                Vector3 distanceDiff = resetTransform.position - vrCamera.position;
                vrCameraRoot.transform.position += distanceDiff;

                networkConnection.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.Commands.HEADING_RESET_SUCCESS);
            }
            catch (Exception e) {
                QueuedLogger.LogError($"[QuestNav] Error during recenter: {e.Message}");
                QueuedLogger.LogException(e);
                networkConnection.PublishValue(QuestNavConstants.Topics.MISO, 0);
                resetInProgress = false;
            }
        }
        #endregion
    }
}