using UnityEngine;

namespace QuestNav.Utils
{
    public class LoggerFlusher : MonoBehaviour
    {
        void Update()
        {
            QueuedLogger.Flush();
        }
    }
}