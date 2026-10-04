using UnityEngine;
using UnityEngine.EventSystems;

public class carCam : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Isometric View Settings")]
    public float height = 15f;
    public float verticalAngle = 45f;
    public float sideTiltAngle = 15f;
    public float followSpeed = 5f;

    [Header("Orbit Settings")]
    public float rotationSpeed = 5f;
    public float snapAngle = 90f;
    public float rotationLerpSpeed = 5f;
    public float angleTolerance = 5f;
    public float uiClearanceTime = 0.15f; 

    [Header("Zoom Settings")]
    public float minDistance = 5f;
    public float maxDistance = 30f;
    public float defaultDistance = 15f; // ✅ Added this variable
    public float zoomSpeed = 10f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;
    public float collisionOffset = 0.3f;

    private float currentDistance;
    private float targetDistance;
    private float manualYaw;
    private float snapYaw;
    private bool isManual;

    private Quaternion smoothRotation;
    private Vector3 smoothVelocity;

    private float lastUIHoverTime;

    void Start()
    {
        // ✅ Changed these two lines to use defaultDistance instead of maxDistance
        currentDistance = defaultDistance; 
        targetDistance = defaultDistance;

        snapYaw = Mathf.Round(target.eulerAngles.y / snapAngle) * snapAngle;
        manualYaw = snapYaw;
        smoothRotation = Quaternion.Euler(verticalAngle, manualYaw + sideTiltAngle, 0f);
    }

    void LateUpdate()
    {
        if (!target) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (overUI)
            lastUIHoverTime = Time.time;

        bool uiBlocking = (Time.time - lastUIHoverTime) < uiClearanceTime;

        HandleZoom(!uiBlocking);
        HandleRotation(!uiBlocking);

        UpdateCameraPosition();
    }

    void HandleZoom(bool allowInput)
    {
        if (allowInput)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                targetDistance -= scrollInput * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSpeed);
    }

    void HandleRotation(bool allowInput)
    {
        if (allowInput && Input.GetMouseButton(1)) // right mouse drag
        {
            isManual = true;
            manualYaw += Input.GetAxis("Mouse X") * rotationSpeed;
        }
        else
        {
            if (isManual) isManual = false;

            float targetYaw = target.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(snapYaw, targetYaw);

            if (Mathf.Abs(yawDelta) >= snapAngle - angleTolerance)
                snapYaw = Mathf.Round(targetYaw / snapAngle) * snapAngle;

            manualYaw = Mathf.LerpAngle(manualYaw, snapYaw, Time.deltaTime * rotationLerpSpeed);
        }

        float chosenYaw = manualYaw;
        Quaternion desiredRotation = Quaternion.Euler(verticalAngle, chosenYaw + sideTiltAngle, 0f);
        smoothRotation = Quaternion.Slerp(smoothRotation, desiredRotation, Time.deltaTime * rotationLerpSpeed);
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPos = target.position + Vector3.up * height;
        Vector3 direction = smoothRotation * Vector3.back;
        Vector3 desiredCamPos = targetPos + direction * currentDistance;

        if (Physics.Raycast(targetPos, direction, out RaycastHit hit, currentDistance, collisionLayers))
        {
            float adjustedDist = Vector3.Distance(targetPos, hit.point) - collisionOffset;
            
            // ✅ Ensure we don't snap closer than minDistance even during collision
            currentDistance = Mathf.Clamp(adjustedDist, minDistance, maxDistance); 
            desiredCamPos = targetPos + direction * currentDistance;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredCamPos, ref smoothVelocity, 1f / followSpeed
        );

        transform.LookAt(target.position + Vector3.up * 2f);
    }
}