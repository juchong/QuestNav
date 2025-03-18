using System.IO;
using UnityEngine;

namespace QuestNav.Utils
{
    public class MotionRecorder : MonoBehaviour
    {
        [System.Serializable]
        private struct MotionFrame
        {
            public int frameIndex;
            public float timeStamp;
            public Vector3 position;
            public Quaternion rotation;
        }

        public OVRCameraRig cameraRig;

        private StreamWriter writer;
        private bool firstFrameWritten = false;

        void Start()
        {
            Debug.Log("[MotionRecorder] Logging started.");

            string path = UnityEngine.Application.persistentDataPath + "/motion_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json";

            try
            {
                writer = new StreamWriter(path, false);
                writer.WriteLine("{");
                writer.WriteLine("\"frames\":[");
            }
            catch (IOException e)
            {
                Debug.LogError("[MotionRecorder] ERROR: Failed to create file: " + e.Message);
            }
        }

        void LateUpdate()
        {
            if (writer == null) return;

            MotionFrame frame;
            frame.frameIndex = Time.frameCount;
            frame.timeStamp = Time.time;
            frame.position = cameraRig.centerEyeAnchor.position;
            frame.rotation = cameraRig.centerEyeAnchor.rotation;

            string jsonFrame = JsonUtility.ToJson(frame, true);
            if (firstFrameWritten)
            {
                writer.WriteLine(",");
            }
            writer.Write(jsonFrame);
            writer.Flush(); // Optional: reduce frequency for performance

            firstFrameWritten = true;
        }

        void OnApplicationQuit()
        {
            if (writer != null)
            {
                writer.WriteLine("]");
                writer.WriteLine("}");
                writer.Close();
                Debug.Log("[MotionRecorder] Logging stopped. File closed properly.");
            }
        }
    }
}
