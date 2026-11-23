using UnityEngine;

namespace RCDriftDash
{
    public sealed class TrackBatch : MonoBehaviour
    {
        private void Start()
        {
            StaticBatchingUtility.Combine(gameObject);
        }
    }
}
