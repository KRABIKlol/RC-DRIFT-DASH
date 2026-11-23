using UnityEngine;
using UnityEngine.InputSystem;

namespace RCDriftDash
{
    public sealed class HandControls : MonoBehaviour
    {
        public OVRCameraRig rig;
        public Transform wheel, transmitter, wheelDock, remoteDock;
        public bool leftHandThrottle;
        [Range(30,100)] public float wheelTravel = 60f;
        public bool Held => wheelHeld && remoteHeld;
        public float Steer { get; private set; }
        public float Gas { get; private set; }
        private bool wheelHeld, remoteHeld;
        private Quaternion previous;
        private float twist;
        public bool Desktop { get; set; }
        public Transform Head => rig.centerEyeAnchor;
        public Ray Pointer => new Ray(rig.rightHandAnchor.position, rig.rightHandAnchor.forward);
        public bool Tracked => Desktop || (OVRManager.hasInputFocus && OVRManager.tracker.isPositionTracked &&
            OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) &&
            OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch));
        public bool Select => Desktop ? Keyboard.current?.enterKey.wasPressedThisFrame == true :
            OVRInput.GetDown(OVRInput.RawButton.A);
        public bool Alternate => Desktop ? Keyboard.current?.pKey.wasPressedThisFrame == true :
            OVRInput.GetDown(OVRInput.RawButton.B);

        public void Dock(Transform viewer)
        {
            var yaw = Quaternion.Euler(0,viewer.eulerAngles.y,0);
            var side = leftHandThrottle ? -1 : 1;
            wheelDock.SetPositionAndRotation(viewer.position+yaw*new Vector3(-.18f*side,-.28f,.36f),yaw);
            remoteDock.SetPositionAndRotation(viewer.position+yaw*new Vector3(.18f*side,-.28f,.36f),yaw);
            Drop();
        }

        public void Drop()
        {
            wheelHeld = remoteHeld = false;
            Steer = Gas = twist = 0;
            wheel.SetPositionAndRotation(wheelDock.position,wheelDock.rotation);
            transmitter.SetPositionAndRotation(remoteDock.position,remoteDock.rotation);
        }

        public void Sample()
        {
            if (Desktop)
            {
                var k = Keyboard.current;
                if (k == null) return;
                if (k.gKey.wasPressedThisFrame) wheelHeld = remoteHeld = !Held;
                Gas = Held && k.wKey.isPressed ? 1 : 0;
                Steer = Held ? (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0) : 0;
                return;
            }
            var gasHand = leftHandThrottle ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            var steerHand = leftHandThrottle ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
            Transform throttlePose = leftHandThrottle ? rig.leftHandAnchor : rig.rightHandAnchor;
            Transform steerPose = leftHandThrottle ? rig.rightHandAnchor : rig.leftHandAnchor;
            bool wasHeld = wheelHeld;
            wheelHeld = Grab(wheel, wheelDock, steerPose, steerHand, wheelHeld);
            remoteHeld = Grab(transmitter, remoteDock, throttlePose, gasHand, remoteHeld);
            if (wheelHeld)
            {
                if (!wasHeld) { previous = steerPose.rotation; twist = 0; }
                var delta = Quaternion.Inverse(previous) * steerPose.rotation;
                twist = Mathf.Clamp(twist - Mathf.DeltaAngle(0, 2*Mathf.Atan2(delta.z,delta.w)*Mathf.Rad2Deg), -wheelTravel, wheelTravel);
                previous = steerPose.rotation;
            }
            Steer = Held ? Mathf.Sign(twist)*Mathf.InverseLerp(3,wheelTravel,Mathf.Abs(twist)) : 0;
            Gas = Held ? OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger,gasHand) : 0;
        }

        private static bool Grab(Transform item, Transform dock, Transform hand, OVRInput.Controller side, bool held)
        {
            held = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger,side) &&
                   (held || Vector3.Distance(hand.position, item.position)<.105f);
            var pose = held ? hand : dock;
            item.SetPositionAndRotation(pose.position,pose.rotation);
            return held;
        }
    }
}
