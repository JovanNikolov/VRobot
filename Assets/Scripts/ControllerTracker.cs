using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Valve.VR;

public class ControllerTracker : MonoBehaviour
{
    [Header("VR Components")]
    public SteamVR_Behaviour_Pose rightController; // Assign in Inspector
    public SteamVR_Action_Single gripAction; // Assign in Inspector

    [Header("Robot Dimensions")]
    public float upperArmLength = 0.08f;  // meters
    public float forearmLength = 0.08f;   // meters
    public float servoOffset = 0.05f;

    [Header("Network Settings")]
    public string raspberryPiIP = "192.168.0.210"; // your Pi's IP
    public int port = 6000;

    [Header("Performance Settings")]
    public float sendInterval = 0.05f; // 20Hz update rate

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool enableGizmos = true;
    public bool enableNetworking = true;

    [Header("Transforms")]
    public Transform shoulderTransform;
    public Transform elbowTransform;
    public Transform wristTransform;

    [Header("Wrist Settings")]
    public Vector3 controllerToWristOffset = new Vector3(0f, -0.08f, 0.02f); // Adjustable in Inspector
    public float wristSmoothingFactor = 0.15f;

    private float previousWristAngle = 0f;
    private float smoothedWristAngle = 0f;
    private bool wristInitialized = false;
    // Private fields
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private float lastSendTime = 0f;

    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("ControllerTracker Start called on: " + gameObject.name);

        // Initialize UDP client only if networking is enabled
        if (enableNetworking)
        {
            try
            {
                udpClient = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(raspberryPiIP), port);

                // Send test message
                byte[] test = Encoding.ASCII.GetBytes("90,150,90,90,90,150");
                udpClient.Send(test, test.Length, remoteEndPoint);

                if (enableDebugLogs)
                    Debug.Log("Test UDP sent successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP initialization error: " + ex.Message);
                enableNetworking = false; // Disable networking if it fails
            }
        }

        // Initialize transforms
        InitializeTransforms();
    }

    void InitializeTransforms()
    {
        if (shoulderTransform == null)
        {
            GameObject shoulder = new GameObject("ShoulderSocket");
            shoulder.transform.position = new Vector3(0.20f, 1.3f, 0f);
            shoulderTransform = shoulder.transform;
        }
        if (elbowTransform == null)
        {
            GameObject elbow = new GameObject("Elbow");
            elbow.transform.parent = shoulderTransform;
            elbowTransform = elbow.transform;
        }
        if (wristTransform == null)
        {
            GameObject wrist = new GameObject("Wrist");
            wrist.transform.parent = elbowTransform;
            wristTransform = wrist.transform;
        }
    }

    void Update()
    {
        if (rightController == null) return;

        // Send at controlled rate
        if (Time.time - lastSendTime >= sendInterval)
        {
            CalculateAndSendServoPositions();
            lastSendTime = Time.time;
        }
    }

    void CalculateAndSendServoPositions()
    {
        // --- GET CONTROLLER WRIST POSITION ---
        // Valve Index controller offset - adjust the controller position to actual wrist
        // The controller transform is at the grip, we need to offset to get the wrist center
        Vector3 controllerOffset = rightController.transform.rotation * new Vector3(0f, -0.08f, 0.02f);
        // Adjust these values based on your hand size:
        Vector3 targetWristPos = rightController.transform.position + controllerOffset;

        // --- BASE POSITIONS ---
        Vector3 shoulderPos = new Vector3(0.20f, 1.3f, 0f);

        // Pitch servo rotates at shoulder position (1 DOF: up/down)
        Vector3 pitchAxis = Vector3.right;
        Vector3 toTargetFromShoulder = targetWristPos - shoulderPos;
        float shoulderYawDeg = Mathf.Atan2(toTargetFromShoulder.y, toTargetFromShoulder.z) * Mathf.Rad2Deg;

        // Roll servo rotates at 3cm right from shoulder (1 DOF: roll)
        Vector3 rollPos = shoulderPos + new Vector3(0.03f, 0f, 0f);
        Vector3 toTargetFromRoll = targetWristPos - rollPos;
        float shoulderRollDeg = Mathf.Atan2(toTargetFromRoll.x, toTargetFromRoll.z) * Mathf.Rad2Deg;

        // Yaw servo starts 4cm forward from roll (1 DOF: yaw)
        Vector3 yawPos = rollPos + new Vector3(0f, 0f, 0.04f);
        Vector3 toTargetFromYaw = targetWristPos - yawPos;
        float shoulderPitchDeg = Mathf.Atan2(toTargetFromYaw.x, toTargetFromYaw.z) * Mathf.Rad2Deg;

        // Elbow is 3cm straight from yaw, rotates hand forward (1 DOF: hinge)
        Vector3 elbowDir = toTargetFromYaw.normalized;
        Vector3 elbowPos = yawPos + elbowDir * 0.03f;
        float reach = Mathf.Min((targetWristPos - elbowPos).magnitude, forearmLength - 0.001f);
        float elbowRad = Mathf.Acos(
            Mathf.Clamp(
                (forearmLength * forearmLength + 0.03f * 0.03f - reach * reach) /
                (2 * forearmLength * 0.03f),
                -1f, 1f
            )
        );
        float elbowDeg = elbowRad * Mathf.Rad2Deg;

        // Wrist is at end of forearm (rotation only, 1 DOF)
        Vector3 wristDir = (targetWristPos - elbowPos).normalized;
        Vector3 wristPos = elbowPos + wristDir * forearmLength;

        // --- IMPROVED WRIST ROTATION ---
        // Use quaternion-based approach to avoid gimbal lock and jumps
        float wristRotationDeg = CalculateSmoothWristRotation();

        // Grip
        float gripServo = Map(gripAction.GetAxis(SteamVR_Input_Sources.RightHand), 0, 1, 0, 180);

        // --- MAP ANGLES TO SERVO RANGE ---
        float servoShoulderYaw = Map(shoulderYawDeg, -90, 90, 0, 180);
        float servoShoulderRoll = Map(shoulderRollDeg, -90, 90, 180, 0);
        float servoShoulderPitch = Map(shoulderPitchDeg, -90, 90, 0, 180);
        float servoElbow = Map(elbowDeg, 0, 135, 0, 180);
        float servoWrist = Map(wristRotationDeg, -90, 90, 0, 180);

        // --- SEND TO ROBOT ---
        SendServoPositions(
            servoShoulderPitch,
            servoShoulderRoll,
            servoShoulderYaw,
            servoElbow,
            servoWrist,
            gripServo
        );

        // --- UPDATE VISUALIZATION ---
        UpdateVisualization(shoulderPos, elbowPos, wristPos);
    }

    void UpdateVisualization(Vector3 shoulderPos, Vector3 elbowPos, Vector3 wristPos)
    {
        if (shoulderTransform != null) shoulderTransform.position = shoulderPos;
        if (elbowTransform != null) elbowTransform.position = elbowPos;
        if (wristTransform != null) wristTransform.position = wristPos;
    }

    void SendServoPositions(float shoulderYaw, float shoulderPitch, float shoulderRoll,
                        float elbow, float wristRotation, float gripServo)
    {
        // Debug output (controlled by enableDebugLogs)
        if (enableDebugLogs)
        {
            Debug.Log($"Shoulder Yaw: {shoulderYaw:F1}");
            Debug.Log($"Shoulder Pitch: {shoulderPitch:F1}");
            Debug.Log($"Shoulder Roll: {shoulderRoll:F1}");
            Debug.Log($"Elbow: {elbow:F1}");
            Debug.Log($"Wrist Rotation: {wristRotation:F1}");
            Debug.Log($"Grip: {gripServo:F1}");
        }

        // Apply calibrations
        var calibratedShoulderPitch = Mathf.Clamp(shoulderPitch - 10, 0, 180);
        var calibratedShoulderRoll = Mathf.Clamp(shoulderRoll + 10, 0, 180);
        var calibratedElbow = Mathf.Clamp(elbow, 90, 180);
        var calibratedGrip = Mathf.Clamp(gripServo + 20, 110, 180);

        // Send to communication manager instead of directly over network
        if (RobotCommunicationManager.Instance != null)
        {
            RobotCommunicationManager.Instance.UpdateArmServos(
                calibratedShoulderPitch,
                calibratedShoulderRoll,
                shoulderYaw,
                calibratedElbow,
                wristRotation,
                calibratedGrip
            );
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("RobotCommunicationManager not found!");
        }
    }

    float CalculateSmoothWristRotation()
    {
        // Calculate wrist rotation as twist around the forearm axis only
        Vector3 controllerOffset = rightController.transform.rotation * controllerToWristOffset;
        Vector3 targetWristPos = rightController.transform.position + controllerOffset;
        Vector3 shoulderPos = new Vector3(0.20f, 1.3f, 0f);
        Vector3 rollPos = shoulderPos + new Vector3(0.03f, 0f, 0f);
        Vector3 yawPos = rollPos + new Vector3(0f, 0f, 0.04f);
        Vector3 toTargetFromYaw = targetWristPos - yawPos;
        Vector3 elbowPos = yawPos + toTargetFromYaw.normalized * 0.03f;

        // Forearm direction
        Vector3 forearmDir = (targetWristPos - elbowPos).normalized;

        // Get the controller's up vector
        Vector3 controllerUp = rightController.transform.up;

        // Remove the component along the forearm to get pure twist
        Vector3 twistUp = controllerUp - Vector3.Project(controllerUp, forearmDir);
        twistUp.Normalize();

        Vector3 referenceUp = Vector3.up - Vector3.Project(Vector3.up, forearmDir);
        if (referenceUp.magnitude < 0.1f) // If forearm is nearly vertical
        {
            referenceUp = Vector3.forward - Vector3.Project(Vector3.forward, forearmDir);
        }
        referenceUp.Normalize();

        // Calculate twist angle between reference and actual controller up
        float angle = -Vector3.SignedAngle(referenceUp, twistUp, forearmDir);

        //sensitivity adjustment
        angle *= 0.75f;

        if (!wristInitialized)
        {
            smoothedWristAngle = angle;
            previousWristAngle = angle;
            wristInitialized = true;
            return angle;
        }

        float deltaAngle = Mathf.DeltaAngle(previousWristAngle, angle);
        float targetAngle = previousWristAngle + deltaAngle;

        smoothedWristAngle = Mathf.Lerp(smoothedWristAngle, targetAngle, 1f - wristSmoothingFactor);
        previousWristAngle = smoothedWristAngle;

        return smoothedWristAngle;
    }

    float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return Mathf.Clamp(outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin), outMin, outMax);
    }

    void OnDrawGizmos()
    {
        if (!enableGizmos || rightController == null) return;

        Vector3 controllerOffset = rightController.transform.rotation * controllerToWristOffset;
        Vector3 targetWristPos = rightController.transform.position + controllerOffset;

        Vector3 shoulderPos = new Vector3(0.20f, 1.3f, 0f);

        Vector3 rollPos = shoulderPos + new Vector3(0.03f, 0f, 0f);
        Vector3 yawPos = rollPos + new Vector3(0f, 0f, 0.04f);
        Vector3 toTargetFromYaw = targetWristPos - yawPos;
        Vector3 elbowDir = toTargetFromYaw.normalized;
        Vector3 elbowPos = yawPos + elbowDir * 0.03f;
        Vector3 wristDir = (targetWristPos - elbowPos).normalized;
        Vector3 wristPos = elbowPos + wristDir * forearmLength;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(shoulderPos, 0.02f); // Pitch

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(rollPos, 0.02f);   // Roll

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(yawPos, 0.02f);     // Yaw

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(elbowPos, 0.02f);// Elbow

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(wristPos, 0.02f); // Wrist

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(targetWristPos, 0.02f); // Actual target (with offset)

        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawWireSphere(rightController.transform.position, 0.015f); // Raw controller position

        Gizmos.color = Color.white;
        Gizmos.DrawLine(shoulderPos, rollPos);
        Gizmos.DrawLine(rollPos, yawPos);
        Gizmos.DrawLine(yawPos, elbowPos);
        Gizmos.DrawLine(elbowPos, wristPos);
        Gizmos.DrawLine(wristPos, targetWristPos);

        // Draw reach sphere
        Gizmos.color = new Color(1, 0, 0, 0.15f);
        Gizmos.DrawWireSphere(shoulderPos, upperArmLength + forearmLength);
    }

    void OnDestroy()
    {
        // Clean up UDP client
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient.Dispose();
        }
    }
}
