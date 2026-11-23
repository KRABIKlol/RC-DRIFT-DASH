using UnityEngine;

namespace RCDriftDash
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DriftCar : MonoBehaviour
    {
        public Rigidbody body;
        public Transform[] wheels;
        public float acceleration = 1.45f, maxSpeed = .70f, steeringRate = 285f;
        public AudioSource motor, skid, impact;
        public bool driving;
        public float throttle, steering;
        private float lastImpact;

        public void Park(Transform start)
        {
            SetMotion(false);
            body.position = start.position;
            body.rotation = start.rotation;
            transform.SetPositionAndRotation(start.position, start.rotation);
        }

        public void SetMotion(bool enabled)
        {
            driving = enabled;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = !enabled;
            throttle = steering = 0;
        }

        private void FixedUpdate()
        {
            if (!driving) return;
            Vector3 v = body.linearVelocity;
            float forward = Vector3.Dot(v, transform.forward);
            float lateral = Vector3.Dot(v, transform.right);
            float grip = Mathf.Lerp(9f, 4f, Mathf.Abs(steering) * Mathf.Clamp01(v.magnitude / maxSpeed));
            float thrust = throttle * acceleration - forward * (.95f + (1 - throttle) * 2f);
            body.AddForce(transform.forward * thrust - transform.right * lateral * grip, ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(v, maxSpeed);
            body.MoveRotation(body.rotation * Quaternion.Euler(0, steering * steeringRate *
                Mathf.Clamp(forward / .34f, -.6f, 1f) * Time.fixedDeltaTime, 0));
            foreach (var wheel in wheels) wheel.Rotate(Vector3.up, forward * 4300f * Time.fixedDeltaTime);
        }

        private void Update()
        {
            motor.volume = driving ? .10f + throttle * .16f : 0;
            motor.pitch = .7f + throttle * .4f + body.linearVelocity.magnitude;
            skid.volume = driving ? Mathf.Clamp01(Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.right)) * 4 - .12f) * .32f : 0;
        }

        private void OnCollisionEnter(Collision hit)
        {
            if (driving && Time.time > lastImpact + .16f && hit.relativeVelocity.magnitude > .09f)
            {
                lastImpact = Time.time;
                impact.Play();
            }
        }
    }
}
