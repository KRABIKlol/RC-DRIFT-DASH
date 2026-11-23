using UnityEngine;

namespace RCDriftDash
{
    public sealed class RaceGate : MonoBehaviour
    {
        public RaceSession session;
        public int index;
        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == session.car.body)
                session.Cross(index, Vector3.Dot(session.car.body.linearVelocity, transform.forward));
        }
    }
}
