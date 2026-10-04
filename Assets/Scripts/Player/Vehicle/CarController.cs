using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class ThreeWheelerController : MonoBehaviour
{
    [Header("Wheel Colliders (1 Front, 2 Rear)")]
    public WheelCollider frontWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;

    [Header("Vehicle Specifications")]
    public float motorForce = 1200f;
    public float maxSteerAngle = 35f;
    public float brakeForce = 2500f;

    [Header("Handling & Physics")]
    [Tooltip("Keep slightly high to maintain top-heavy momentum for cornering risk.")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.2f, 0); 
    public float leanForce = 1500f; 

    [Header("Engine Audio Settings")]
    public AudioSource engineAudioSource;
    [Tooltip("Pitch when resting at idle speed.")]
    public float minPitch = 0.75f;
    [Tooltip("Pitch when driving at top speed.")]
    public float maxPitch = 2.4f;
    [Tooltip("Top speed (in km/h) where engine pitch reaches its peak.")]
    public float maxSpeedKmh = 60f;
    [Tooltip("How fast the engine pitch revs up or down.")]
    public float revSmoothness = 6f;
    [Tooltip("Audio volume when idling.")]
    public float idleVolume = 0.35f;
    [Tooltip("Audio volume at max speed / acceleration.")]
    public float maxVolume = 0.9f;

    private float horizontalInput;
    private float verticalInput;
    private float leanInput;
    private bool isHandbraking;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;

        // Auto-assign AudioSource if not manually referenced
        if (engineAudioSource == null)
        {
            engineAudioSource = GetComponent<AudioSource>();
        }

        // Initialize engine sound loop
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
        }
    }

    void Update()
    {
        GetInput();
        HandleEngineAudio();

        UpdateWheelVisuals(frontWheel, frontWheelMesh);
        UpdateWheelVisuals(rearLeftWheel, rearLeftWheelMesh);
        UpdateWheelVisuals(rearRightWheel, rearRightWheelMesh);
    }

    void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        HandleCounterWeight();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        
        // Dedicated handbrake key
        isHandbraking = Input.GetKey(KeyCode.Space); 

        // Lean / Counter-Weight logic (Shift + A/D)
        leanInput = 0f;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            leanInput = Input.GetAxis("Horizontal"); 
        }
    }

    private void HandleMotor()
    {
        // Local forward speed (+ is forward, - is reverse)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;

        float currentTorque = 0f;
        float currentBrake = 0f;

        if (isHandbraking)
        {
            currentBrake = brakeForce;
        }
        else if (verticalInput > 0) // Accelerating Forward (W / Up)
        {
            if (forwardSpeed < -0.5f) // If rolling backwards, apply brakes first
            {
                currentBrake = brakeForce;
            }
            else // Otherwise, accelerate forward
            {
                currentTorque = verticalInput * motorForce;
            }
        }
        else if (verticalInput < 0) // Reversing / Braking (S / Down)
        {
            if (forwardSpeed > 0.5f) // If moving forward, apply brakes to slow down
            {
                currentBrake = brakeForce;
            }
            else // Once stopped or rolling backward, apply reverse drive
            {
                currentTorque = verticalInput * motorForce;
            }
        }

        // Apply torque to rear drive wheels
        rearLeftWheel.motorTorque = currentTorque;
        rearRightWheel.motorTorque = currentTorque;

        // Apply brakes across all wheels
        frontWheel.brakeTorque = currentBrake;
        rearLeftWheel.brakeTorque = currentBrake;
        rearRightWheel.brakeTorque = currentBrake;
    }

    private void HandleSteering()
    {
        frontWheel.steerAngle = maxSteerAngle * horizontalInput;
    }

    private void HandleCounterWeight()
    {
        if (Mathf.Abs(leanInput) > 0.1f)
        {
            rb.AddRelativeTorque(Vector3.forward * leanInput * leanForce * -1f);
        }
    }

    private void HandleEngineAudio()
    {
        if (engineAudioSource == null || engineAudioSource.clip == null) return;

        // Convert current velocity magnitude to km/h
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        // Speed load vs throttle load blend
        float speedFactor = Mathf.Clamp01(speedKmh / maxSpeedKmh);
        float throttleFactor = Mathf.Abs(verticalInput);

        // Blended engine load: 70% actual wheel speed, 30% instant throttle input
        float engineLoad = Mathf.Clamp01(speedFactor * 0.7f + throttleFactor * 0.3f);

        // Calculate pitch and volume targets
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, engineLoad);
        float targetVolume = Mathf.Lerp(idleVolume, maxVolume, engineLoad);

        // Smooth pitch and volume changes to avoid abrupt audio pops
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * revSmoothness);
        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, targetVolume, Time.deltaTime * revSmoothness);
    }

    private void UpdateWheelVisuals(WheelCollider col, Transform mesh)
    {
        if (mesh == null) return;
        
        col.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.position = position;
        mesh.rotation = rotation;
    }
}