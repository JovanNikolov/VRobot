using UnityEngine;
using Valve.VR;

public class HeadTracker : MonoBehaviour
{
    [Header("VR Components")]
    public Transform headset; 
    public SteamVR_Behaviour_Pose headPose; 

    [Header("Head Servo Settings")]
    [Range(-90, 90)]
    public float headPanRange = 60f; // Max degrees left/right
    [Range(-45, 45)]
    public float headTiltRange = 30f; // Max degrees up/down

    [Header("Tracking Settings")]
    public float panSensitivity = 1.0f;
    public float tiltSensitivity = 0.8f;
    public float smoothingFactor = 0.15f;
    public bool invertPan = true;
    public bool invertTilt = false;

    [Header("Calibration")]
    public bool useRelativeTracking = true; // Track relative to start position
    public KeyCode recalibrateCenterKey = KeyCode.R; 

    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool enableGizmos = true;
    public bool showCurrentAngles = true;

    private Vector3 initialHeadPosition;
    private Quaternion initialHeadRotation;
    private float smoothedPan = 90f; 
    private float smoothedTilt = 90f; 
    private bool initialized = false;

    private float currentPanAngle = 0f;
    private float currentTiltAngle = 0f;

    void Start()
    {
        FindHeadset();

        if (headset == null && headPose == null)
        {
            Debug.LogError("HeadTracker: No headset transform found! Please assign manually.");
            enabled = false;
            return;
        }

        // Store initial position/rotation for relative tracking
        if (headset != null)
        {
            initialHeadPosition = headset.position;
            initialHeadRotation = headset.rotation;
        }
        else if (headPose != null)
        {
            initialHeadPosition = headPose.transform.position;
            initialHeadRotation = headPose.transform.rotation;
        }

        initialized = true;

        if (enableDebugLogs)
            Debug.Log("HeadTracker initialized on: " + gameObject.name);
    }

    void FindHeadset()
    {

        if (headset == null && GetComponent<Camera>() != null)
        {
            headset = transform;
            if (enableDebugLogs) Debug.Log("Found headset via Camera component");
            return;
        }

        if (headPose == null)
        {
            headPose = GetComponent<SteamVR_Behaviour_Pose>();
            if (headPose != null && enableDebugLogs)
                Debug.Log("Found headset via SteamVR_Behaviour_Pose");
        }

        if (headset == null)
        {
            GameObject cameraRig = GameObject.Find("[CameraRig]");
            if (cameraRig != null)
            {
                Transform cameraTransform = cameraRig.transform.Find("Camera");
                if (cameraTransform == null)
                    cameraTransform = cameraRig.transform.Find("Head");

                if (cameraTransform != null)
                {
                    headset = cameraTransform;
                    if (enableDebugLogs)
                        Debug.Log("Found headset via [CameraRig]/Camera");

                    if (headPose == null)
                        headPose = cameraTransform.GetComponent<SteamVR_Behaviour_Pose>();
                }
            }
        }

        if (headset == null && headPose == null)
        {
            SteamVR_Behaviour_Pose[] poses = FindObjectsOfType<SteamVR_Behaviour_Pose>();
            foreach (var pose in poses)
            {
                if (pose.inputSource == SteamVR_Input_Sources.Head)
                {
                    headPose = pose;
                    headset = pose.transform;
                    if (enableDebugLogs)
                        Debug.Log("Found headset via SteamVR Head input source");
                    break;
                }
            }
        }

        if (headset == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                headset = mainCam.transform;
                if (enableDebugLogs)
                    Debug.Log("Found headset via Camera.main");
            }
        }
    }

    void Update()
    {
        if (!initialized) return;

        CalculateAndUpdateHeadServos();
    }

    void CalculateAndUpdateHeadServos()
    {
        Transform currentTransform = headPose != null ? headPose.transform : headset;
        if (currentTransform == null) return;

        float panAngle, tiltAngle;

        if (useRelativeTracking)
        {
            // Calculate relative rotation from initial position
            Quaternion relativeRotation = Quaternion.Inverse(initialHeadRotation) * currentTransform.rotation;
            Vector3 eulerAngles = relativeRotation.eulerAngles;

            panAngle = eulerAngles.y;
            if (panAngle > 180) panAngle -= 360;

            tiltAngle = eulerAngles.x;
            if (tiltAngle > 180) tiltAngle -= 360;
            tiltAngle = -tiltAngle;
        }
        else
        {
            Vector3 forward = currentTransform.forward;

            // Pan (yaw) - rotation around Y axis
            panAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

            // Tilt (pitch) - rotation around X axis
            tiltAngle = Mathf.Asin(forward.y) * Mathf.Rad2Deg;
        }

        // Store for debug display
        currentPanAngle = panAngle;
        currentTiltAngle = tiltAngle;

        panAngle *= panSensitivity;
        tiltAngle *= tiltSensitivity;

        if (invertPan) panAngle = -panAngle;
        if (invertTilt) tiltAngle = -tiltAngle;

        panAngle = Mathf.Clamp(panAngle, -headPanRange, headPanRange);
        tiltAngle = Mathf.Clamp(tiltAngle, -headTiltRange, headTiltRange);

        float targetPan = Map(panAngle, -headPanRange, headPanRange, 0, 180);
        float targetTilt = Map(tiltAngle, -headTiltRange, headTiltRange, 0, 180);

        smoothedPan = Mathf.Lerp(smoothedPan, targetPan, 1f - smoothingFactor);
        smoothedTilt = Mathf.Lerp(smoothedTilt, targetTilt, 1f - smoothingFactor);

        if (RobotCommunicationManager.Instance != null)
        {
            RobotCommunicationManager.Instance.UpdateHeadServos(smoothedPan, smoothedTilt);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Head - Pan: {smoothedPan:F1}° ({currentPanAngle:F1}°), Tilt: {smoothedTilt:F1}° ({currentTiltAngle:F1}°)");
        }
    }

    float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return Mathf.Clamp(outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin), outMin, outMax);
    }

    void OnDrawGizmos()
    {
        if (!enableGizmos) return;

        Transform currentTransform = headPose != null ? headPose.transform : headset;
        if (currentTransform == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentTransform.position, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(currentTransform.position, currentTransform.forward * 0.5f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(currentTransform.position, currentTransform.up * 0.3f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(currentTransform.position, currentTransform.right * 0.3f);

        if (useRelativeTracking && Application.isPlaying && initialized)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(initialHeadPosition, 0.08f);
            Gizmos.DrawRay(initialHeadPosition, initialHeadRotation * Vector3.forward * 0.4f);
        }
    }

    void OnGUI()
    {
        if (showCurrentAngles && Application.isPlaying && initialized)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Head Pan: {currentPanAngle:F1}° → Servo: {smoothedPan:F1}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Head Tilt: {currentTiltAngle:F1}° → Servo: {smoothedTilt:F1}");
            if (useRelativeTracking)
            {
                GUI.Label(new Rect(10, 50, 300, 20), "Mode: Relative (Press R to recenter)");
            }
            else
            {
                GUI.Label(new Rect(10, 50, 300, 20), "Mode: Absolute");
            }
        }
    }

    public void RecalibrateCenter()
    {
        Transform currentTransform = headPose != null ? headPose.transform : headset;
        if (currentTransform != null)
        {
            initialHeadPosition = currentTransform.position;
            initialHeadRotation = currentTransform.rotation;
            smoothedPan = 90f;
            smoothedTilt = 90f;

            Debug.Log("Head tracking recalibrated to current position");
        }
    }
}