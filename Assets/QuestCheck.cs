using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(Camera))]
public sealed class QuestCheck : MonoBehaviour
{
    private Transform diorama;
    private bool placedFromHeadset;

    private void Start()
    {
        diorama = new GameObject("Scale check (1 metre)").transform;
        AddBlock("Table", new Vector3(0, -0.025f, 0), new Vector3(1, 0.05f, 1), Color.gray);
        AddBlock("Left barrier", new Vector3(-0.48f, 0.02f, 0), new Vector3(0.04f, 0.04f, 1), Color.blue);
        AddBlock("Right barrier", new Vector3(0.48f, 0.02f, 0), new Vector3(0.04f, 0.04f, 1), Color.blue);
        AddBlock("Car scale reference", new Vector3(0, 0.025f, 0), new Vector3(0.08f, 0.05f, 0.16f), Color.red);
        diorama.position = new Vector3(0, -0.35f, 1.1f);
        ApplyHeadPose();
    }

    private void OnEnable() => Application.onBeforeRender += ApplyHeadPose;
    private void OnDisable() => Application.onBeforeRender -= ApplyHeadPose;
    private void Update() => ApplyHeadPose();

    private void ApplyHeadPose()
    {
        var head = InputSystemHead();
        if (head == null || !head.isTracked.isPressed)
            return;

        var position = head.centerEyePosition.ReadValue();
        var rotation = head.centerEyeRotation.ReadValue();
        transform.SetPositionAndRotation(position, rotation);
        if (placedFromHeadset || diorama == null)
            return;

        var yaw = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
        diorama.SetPositionAndRotation(position + yaw * new Vector3(0, -0.35f, 1.1f), yaw);
        placedFromHeadset = true;
    }

    private static XRHMD InputSystemHead() => UnityEngine.InputSystem.InputSystem.GetDevice<XRHMD>();

    private void AddBlock(string label, Vector3 position, Vector3 size, Color color)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = label;
        block.transform.SetParent(diorama, false);
        block.transform.localPosition = position;
        block.transform.localScale = size;
        block.GetComponent<Renderer>().material.color = color;
    }
}
