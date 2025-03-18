namespace QuestNav.Core
{
    /// <summary>
    /// Contains all constants used by the QuestNav application.
    /// Organized by functional category for easier maintenance.
    /// </summary>
    public static class QuestNavConstants
    {
        /// <summary>
        /// Constants related to network configuration and communication.
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// Application name for NetworkTables connection
            /// </summary>
            public const string APP_NAME = "QuestNav";

            /// <summary>
            /// Server address format for NetworkTables connection
            /// Pattern replaces TE with team number prefix and AM with team number suffix
            /// </summary>
            public const string SERVER_ADDRESS_FORMAT = "10.TE.AM.2";

            /// <summary>
            /// Alternate roboRIO network address that may be available
            /// </summary>
            public const string ALTERNATE_ADDRESS = "172.22.11.2";

            /// <summary>
            /// NetworkTables server port
            /// </summary>
            public const int SERVER_PORT = 5810;

            /// <summary>
            /// Default reconnect delay for failed connection attempts (seconds)
            /// </summary>
            public const float DEFAULT_RECONNECT_DELAY = 3.0f;

            /// <summary>
            /// Maximum reconnection delay (seconds)
            /// </summary>
            public const float MAX_RECONNECT_DELAY = 10.0f;

            /// <summary>
            /// Cooldown before trying candidates that have failed previously (seconds)
            /// </summary>
            public const float CANDIDATE_FAILURE_COOLDOWN = 10.0f;

            /// <summary>
            /// Delay before retrying when network is unreachable (seconds)
            /// </summary>
            public const int UNREACHABLE_NETWORK_DELAY = 5;

            /// <summary>
            /// Maximum time to wait for a connection attempt before resetting state (seconds)
            /// </summary>
            public const float CONNECTION_ATTEMPT_TIMEOUT = 10.0f;

            /// <summary>
            /// Default team number when none is provided
            /// </summary>
            public const string DEFAULT_TEAM_NUMBER = "9999";
        }

        /// <summary>
        /// Constants related to the heartbeat system for connection monitoring.
        /// </summary>
        public static class Heartbeat
        {
            /// <summary>
            /// Maximum number of consecutive heartbeat failures before forcing reconnection
            /// </summary>
            public const int MAX_FAILED_HEARTBEATS = 3;

            /// <summary>
            /// Time interval between heartbeat checks (seconds)
            /// </summary>
            public const float HEARTBEAT_INTERVAL = 1.0f;

            /// <summary>
            /// Maximum time to wait for a heartbeat response before considering it failed (seconds)
            /// </summary>
            public const float HEARTBEAT_TIMEOUT = 3.0f;
        }

        /// <summary>
        /// Constants related to NetworkTables topics and paths.
        /// </summary>
        public static class Topics
        {
            /// <summary>
            /// Base path for all QuestNav topics
            /// </summary>
            private const string BASE_PATH = "/questnav";

            /// <summary>
            /// Command response topic (Quest to robot)
            /// </summary>
            public const string MISO = BASE_PATH + "/miso";

            /// <summary>
            /// Command request topic (robot to Quest)
            /// </summary>
            public const string MOSI = BASE_PATH + "/mosi";

            /// <summary>
            /// Frame count topic
            /// </summary>
            public const string FRAME_COUNT = BASE_PATH + "/frameCount";

            /// <summary>
            /// Timestamp topic
            /// </summary>
            public const string TIMESTAMP = BASE_PATH + "/timestamp";

            /// <summary>
            /// Position topic
            /// </summary>
            public const string POSITION = BASE_PATH + "/position";

            /// <summary>
            /// Quaternion rotation topic
            /// </summary>
            public const string QUATERNION = BASE_PATH + "/quaternion";

            /// <summary>
            /// Euler angles topic
            /// </summary>
            public const string EULER_ANGLES = BASE_PATH + "/eulerAngles";

            /// <summary>
            /// Battery percentage topic
            /// </summary>
            public const string BATTERY_PERCENT = BASE_PATH + "/batteryPercent";

            /// <summary>
            /// Initial position topic
            /// </summary>
            public const string INIT_POSITION = BASE_PATH + "/init/position";

            /// <summary>
            /// Initial euler angles topic
            /// </summary>
            public const string INIT_EULER_ANGLES = BASE_PATH + "/init/eulerAngles";

            /// <summary>
            /// Reset pose topic
            /// </summary>
            public const string RESET_POSE = BASE_PATH + "/resetpose";

            /// <summary>
            /// Heartbeat topic (Quest to robot)
            /// </summary>
            public const string HEARTBEAT_TO_ROBOT = BASE_PATH + "/heartbeat/quest_to_robot";

            /// <summary>
            /// Heartbeat topic (robot to Quest)
            /// </summary>
            public const string HEARTBEAT_FROM_ROBOT = BASE_PATH + "/heartbeat/robot_to_quest";
        }

        /// <summary>
        /// Constants related to command processing.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Command code for heading reset request
            /// </summary>
            public const int HEADING_RESET = 1;

            /// <summary>
            /// Command code for pose reset request
            /// </summary>
            public const int POSE_RESET = 2;

            /// <summary>
            /// Command code for ping request
            /// </summary>
            public const int PING = 3;

            /// <summary>
            /// Response code for ping
            /// </summary>
            public const int PING_RESPONSE = 97;

            /// <summary>
            /// Response code for successful pose reset
            /// </summary>
            public const int POSE_RESET_SUCCESS = 98;

            /// <summary>
            /// Response code for successful heading reset
            /// </summary>
            public const int HEADING_RESET_SUCCESS = 99;
        }

        /// <summary>
        /// Constants related to the display and update frequency.
        /// </summary>
        public static class Display
        {
            /// <summary>
            /// Quest display frequency (in Hz)
            /// </summary>
            public const float DISPLAY_FREQUENCY = 120.0f;
        }

        /// <summary>
        /// Constants related to FRC field dimensions and pose resets.
        /// </summary>
        public static class Field
        {
            /// <summary>
            /// FRC field length in meters
            /// </summary>
            public const float FIELD_LENGTH = 16.54f;

            /// <summary>
            /// FRC field width in meters
            /// </summary>
            public const float FIELD_WIDTH = 8.02f;

            /// <summary>
            /// Maximum number of attempts to read pose data
            /// </summary>
            public const int MAX_POSE_READ_RETRIES = 3;

            /// <summary>
            /// Delay between retry attempts (ms)
            /// </summary>
            public const float POSE_RETRY_DELAY_MS = 50f;

            /// <summary>
            /// Position error threshold for warning (meters)
            /// </summary>
            public const float POSITION_ERROR_THRESHOLD = 0.01f; // 1cm
        }
    }
}