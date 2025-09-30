//using UnityEngine;

//public class RobotArmTracking : MonoBehaviour
//{
//    public Transform vrCamera;
//    public Transform vrLeftHand;
//    public Transform vrRightHand;

//    public Transform head;
//    public Transform leftUpperArm;
//    public Transform leftForeArm;
//    public Transform leftHand;
//    public Transform rightUpperArm;
//    public Transform rightForeArm;
//    public Transform rightHand;

//    public float servoStep = 15f; // Servo angle resolution
//    public Vector3 leftShoulderOffset = new Vector3(-0.2f, 1.5f, 0);
//    public Vector3 rightShoulderOffset = new Vector3(0.2f, 1.5f, 0);
//    public float upperArmLength = 0.3f;
//    public float foreArmLength = 0.3f;

//    void Update()
//    {
//        // Update head (position and snapped rotation)
//        if (vrCamera != null && head != null)
//        {
//            head.position = vrCamera.position;
//            head.rotation = SnapRotation(vrCamera.rotation);
//        }

//        // Update left arm
//        if (vrLeftHand != null)
//        {
//            UpdateRobotArm(vrLeftHand, leftUpperArm, leftForeArm, leftHand, leftShoulderOffset, true);
//        }

//        // Update right arm
//        if (vrRightHand != null)
//        {
//            UpdateRobotArm(vrRightHand, rightUpperArm, rightForeArm, rightHand, rightShoulderOffset, false);
//        }
//    }

//    void UpdateRobotArm(Transform handTarget, Transform upperArm, Transform foreArm, Transform hand, Vector3 shoulderOffset, bool isLeftArm)
//    {
//        // Shoulder position (fixed relative to body)
//        Vector3 shoulderPos = transform.position + shoulderOffset;
//        Vector3 handPos = handTarget.position;
//        Vector3 shoulderToHand = handPos - shoulderPos;

//        // Total distance to hand
//        float distance = shoulderToHand.magnitude;
//        float maxReach = upperArmLength + foreArmLength;
//        if (distance > maxReach)
//        {
//            shoulderToHand = shoulderToHand.normalized * maxReach; // Limit to reachable range
//            handPos = shoulderPos + shoulderToHand;
//        }

//        // Coordinate system: Unity (Y-up) -> Robot (Z-up convention, adjust for left/right)
//        float x = shoulderToHand.x; // Left-right
//        float y = shoulderToHand.y; // Up-down
//        float z = shoulderToHand.z; // Forward-back

//        // Shoulder angles (Pitch, Roll, Yaw order)
//        // 1. Pitch (around X-axis, up/down in YZ plane)
//        float pitch = Mathf.Atan2(y, z); // Angle in YZ plane
//        pitch = SnapAngle(pitch * Mathf.Rad2Deg);

//        // Project onto XZ plane after pitch for roll and yaw
//        Vector3 pitchAdjusted = Quaternion.AngleAxis(pitch, Vector3.right) * shoulderToHand;
//        float roll = Mathf.Atan2(-pitchAdjusted.x, pitchAdjusted.z); // Roll around Z after pitch
//        roll = SnapAngle(roll * Mathf.Rad2Deg);

//        // Yaw (around Y-axis) after pitch and roll
//        Vector3 rollAdjusted = Quaternion.AngleAxis(roll, Vector3.forward) * pitchAdjusted;
//        float yaw = Mathf.Atan2(rollAdjusted.x, rollAdjusted.z);
//        yaw = SnapAngle(yaw * Mathf.Rad2Deg);

//        // Mirror for left arm if needed (adjust signs based on arm side)
//        if (isLeftArm)
//        {
//            roll = -roll; // Mirror roll for left arm
//            yaw = -yaw;   // Mirror yaw for left arm
//        }

//        // Apply shoulder rotation (Pitch -> Roll -> Yaw)
//        Quaternion shoulderRotation = Quaternion.Euler(pitch, 0, 0) * Quaternion.Euler(0, 0, roll) * Quaternion.Euler(0, yaw, 0);
//        upperArm.position = shoulderPos;
//        upperArm.rotation = shoulderRotation;

//        // Elbow angle (1 DoF, flexion in plane)
//        float r = distance;
//        float cosElbow = (r * r - upperArmLength * upperArmLength - foreArmLength * foreArmLength) / (2 * upperArmLength * foreArmLength);
//        cosElbow = Mathf.Clamp(cosElbow, -1f, 1f); // Avoid NaN
//        float elbowAngle = Mathf.Acos(cosElbow) * Mathf.Rad2Deg;
//        elbowAngle = SnapAngle(elbowAngle); // Snap to servo steps

//        // Elbow position
//        Vector3 elbowPos = shoulderPos + (shoulderRotation * Vector3.forward * upperArmLength);
//        foreArm.position = elbowPos;

//        // Apply elbow rotation (relative to shoulder, flexion along local X-axis)
//        Quaternion elbowRotation = shoulderRotation * Quaternion.Euler(elbowAngle, 0, 0);
//        foreArm.rotation = elbowRotation;

//        // Wrist (1 DoF rotation)
//        Vector3 wristPos = elbowPos + (elbowRotation * Vector3.forward * foreArmLength);
//        hand.position = wristPos;

//        // Wrist rotation (align with target rotation, snapped)
//        float wristAngle = handTarget.rotation.eulerAngles.z; // Use Z-rotation (roll) from VR hand
//        wristAngle = SnapAngle(wristAngle);
//        hand.rotation = elbowRotation * Quaternion.Euler(0, 0, wristAngle);

//        // Ensure hand position matches target (adjust if needed)
//        hand.position = handPos;
//    }

//    float SnapAngle(float angle)
//    {
//        // Normalize angle to [-180, 180] range, then snap to servo steps
//        angle = ((angle + 180f) % 360f) - 180f;
//        return Mathf.Round(angle / servoStep) * servoStep;
//    }

//    Quaternion SnapRotation(Quaternion rotation)
//    {
//        Vector3 euler = rotation.eulerAngles;
//        euler.x = SnapAngle(euler.x);
//        euler.y = SnapAngle(euler.y);
//        euler.z = SnapAngle(euler.z);
//        return Quaternion.Euler(euler);
//    }
//}
