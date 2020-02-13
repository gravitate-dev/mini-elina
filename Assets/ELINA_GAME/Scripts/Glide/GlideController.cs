using Animancer;
using Invector.vCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlideController : MonoBehaviour
{

    [Header("Targeting")]
    [Tooltip("Max angle of viable target from center of camera")]
    private float targetAngleTolerance = 45f;
    [Tooltip("Max distance of viable target")]
    private float targetDistanceTolerance = 50f;
    [Tooltip("speeds up diving process")]
    public float diveTimeMultiplier;
    [Tooltip("Provide with list of viable targets in scene")]
    public List<Transform> validTargets;
    public Transform targetIndicator; //fx used for highlighting target
    public Transform diveTarget;  //current viable target

    [Header("Initial glide settings")]
    [Tooltip("Amount of force added over the initial upwards boost of the glide")]
    public float glideHeightBoost = 5f;
    private float locationMemoryUpdateRate = 0.05f;    //unused currently
    private float locationMemoryTimer = 0f;    //unused currently
    private List<Vector3> previousPositionList;    //unused currently
                                                   //  [Tooltip("will use the maximum height of the character from this long ago; useful in the case where you fall off a ledge and initiate hover.")]
    public float maxLocationMemoryTime = 1f;     //unused currently

    [Header("Glide Controller Settings")]
    [Tooltip("speed when not holding W")]
    public float minGlideSpeed = 1f;
    [Tooltip("speed when holding W")]
    public float maxGlideSpeed = 3f;
    [Tooltip("rate of speed change forwards/backwards")]
    public float glideAcceleration = 5f;
    [Tooltip("exit state if this close to ground")]
    public float minGlideHeight = 2f;
    [Tooltip("acceleration of height loss")]
    public float glideGravityAccel = 1f;
    [Tooltip("maximum height loss per second")]
    public float glideGravityLimit = -3f;
    [Tooltip("max turn speed per second")]
    public float glideTurnSpeed = 3f;
    [Tooltip("turn speed change rate")]
    public float glideTurnAcceleration = 3f;

    [Header("Glide Camera Settings")]
    public float offsetBehind = 8f;
    public float offsetHeight = 5f;

    [Header("Visual")]
    [Range(0f, 45f)]
    [Tooltip("Maximum angle for sideways turning")]
    public float turnBankAngle = 45f;
    [Range(0f, 45f)]
    [Tooltip("Minimum angle of rotation while holding slow button")]
    public float velocityAlignMin = 10f;
    [Range(0f, 89f)]
    [Tooltip("Maximum angle of rotation while holding speed up button")]
    public float velocityAlignMax = 45f;

    public bool isGliding = false;
    public bool isDiving = false;
    public bool doBoost = false;

    public AnimationClip glidingClip;
    public AnimationClip droppingClip;
    private Rigidbody rB;
    private vThirdPersonController thirdPersonController;
    private HybridAnimancerComponent animancer;
    private float targetUpdateRate = 0.5f; //check rate for targets
    private float targetTimer = 0f;//internal variable
    private float currentTurnSpeed = 0; //internal variable
    private bool endDive = false;

    // Start is called before the first frame update
    private void Start()
    {
        thirdPersonController = GetComponent<vThirdPersonController>();
        rB = GetComponent<Rigidbody>();
        animancer = GetComponent<HybridAnimancerComponent>();

        previousPositionList = new List<Vector3>((int)(maxLocationMemoryTime / locationMemoryUpdateRate));

        GlideExit();
    }

    // Update is called once per frame
    private void Update()
    {

        GlideStartInput();

        if (!isDiving)
        {
            if (isGliding)
            {
                UpdateGlideController();
                
                // TODO SPIDERMAN ADD BACK IN TARGET FINDER
                //TargetUpdate();

                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if (diveTarget != null)
                    {
                        GlideExitDive();
                    }
                }
            } else
            {
                GroundCheckForBoost();
            }
            if (thirdPersonController.isGrounded && isGliding && rB.velocity.y < 0f && isGliding)
            {
                GlideExit();
                Debug.Log("glide exit groudned check");
            }

            if (Physics.Raycast(this.transform.position, Vector3.down, minGlideHeight) && rB.velocity.y < 0f && isGliding)
            {
                GlideExit();
                Debug.Log("glide exit height");
            }
            Debug.DrawRay(this.transform.position + new Vector3(0.15f, 0f, 0f), Vector3.down * minGlideHeight, Color.yellow);
        }
        if (diveTarget != null)
            targetIndicator.transform.gameObject.SetActive(true);
        else
            targetIndicator.transform.gameObject.SetActive(false);
    }

    private void GroundCheckForBoost()
    {
        if (thirdPersonController.isGrounded)
        {
            doBoost = true;
        }
    }
    private void GlideStartInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isGliding)
            {
                if (!thirdPersonController.isGrounded)
                {
                    GlideEnter();
                }
            }
            else
            {
                GlideExit();
            }
        }
    }

    public void GlideEnter()
    {
        animancer.Play(glidingClip);
        rB.angularVelocity = Vector3.zero;
        rB.useGravity = false;
        if (doBoost)
        {
            thirdPersonController.isGrounded = false;
            doBoost = false;
            StartCoroutine(GlideBoost());
        }
        isGliding = true;
    }

    public void UpdateGlideController()
    {
        //store vertical
        float velY = rB.velocity.y;

        Vector3 currVel = new Vector3(rB.velocity.x, 0f, rB.velocity.z);

        //apply speed
        Vector3 flyDir;
        if (currVel.sqrMagnitude > 0f)
            flyDir = Vector3.ProjectOnPlane(currVel, Vector3.up).normalized;    //in planar dir
        else
            flyDir = Vector3.ProjectOnPlane(this.transform.forward, Vector3.up).normalized;    //in planar dir

        float zSpeed = Mathf.Lerp(minGlideSpeed, maxGlideSpeed, (Input.GetAxisRaw("Vertical") + 1f) / 2f);  //input mod
        Vector3 targetVel = flyDir * zSpeed; //target direction and speed
        currVel = Vector3.MoveTowards(currVel, targetVel, glideAcceleration * Time.deltaTime); //smooth

        //apply turn
        float wishTurn = Input.GetAxisRaw("Horizontal");
        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, wishTurn * glideTurnSpeed, glideTurnAcceleration * Time.deltaTime);
        Quaternion rot = Quaternion.Euler(0f, currentTurnSpeed, 0f);

        currVel = rot * currVel;

        velY -= Mathf.Abs(glideGravityAccel) * Time.deltaTime;
        velY = Mathf.Clamp(velY, glideGravityLimit, 500f);
        currVel.y = velY;
        rB.velocity = currVel;
/*
        Quaternion turnRot;
        if (currVel.sqrMagnitude > 0f)
            turnRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(currVel, Vector3.up), Vector3.up);
        else
            turnRot = this.transform.rotation;

        //visual rot
        float rotateX = Remap(currVel.magnitude, minGlideSpeed, maxGlideSpeed, velocityAlignMin, velocityAlignMax, true);
        float rotateZ = Remap(-currentTurnSpeed, -glideTurnSpeed, glideTurnSpeed, -turnBankAngle, turnBankAngle, true);
        // turnRot = Quaternion.Lerp(turnRot, Quaternion.LookRotation(currVel.normalized, Vector3.up), alignFwd);
        Quaternion rot2 = Quaternion.Euler(rotateX, 0f, rotateZ);

        rB.MoveRotation(turnRot * rot2);*/
    }

    public void GlideExit()
    {
        animancer.Play(Animator.StringToHash("Free Locomotion"), 0, 0);
        rB.useGravity = true;
        isGliding = false;
        rB.angularVelocity = Vector3.zero;
    }

    private void GlideExitDive()
    {
        animancer.Play(Animator.StringToHash("Free Locomotion"), 0, 0);
        endDive = false;
        rB.useGravity = true;
        isGliding = false;
        isDiving = true;
        targetIndicator.transform.gameObject.SetActive(false);
        StartCoroutine(DiveEnemy(diveTarget));
        diveTarget = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDiving)
        {
            endDive = true;
        }
    }

    private IEnumerator DiveEnemy(Transform target)
    {
        Vector3 startPos = this.transform.position;
        Quaternion startRot = this.transform.rotation;
        float dist = Vector3.Distance(target.position, this.transform.position) - 1f;
        rB.isKinematic = true;
        rB.velocity = Vector3.zero;
        float diveTime = dist / diveTimeMultiplier;
        float diveTimer = 0f;
        Vector3 targetDir;
        Vector3 targetDirPlanar;
        while (diveTimer < diveTime)
        {
            targetDir = (target.position - this.transform.position).normalized;

            if (endDive)
                diveTimer = diveTime;

            Vector3 pos = Coserp(startPos, target.position + (-targetDir), diveTimer / diveTime);
            rB.MovePosition(pos);

            Quaternion newRot = Quaternion.LookRotation(Vector3.Cross(targetDir, new Vector3(1f, 0f, 0f)), -targetDir);

            Quaternion rot = Quaternion.Lerp(startRot, newRot, Mathf.Clamp01(diveTimer / (diveTime / 2)));
            rB.MoveRotation(rot);

            diveTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        targetDir = (target.position - this.transform.position).normalized;
        targetDirPlanar = Vector3.ProjectOnPlane(targetDir, Vector3.up).normalized;
        Quaternion finalRot = Quaternion.LookRotation(targetDirPlanar, Vector3.up);
        rB.MoveRotation(finalRot);
        endDive = false;
        isDiving = false;
        rB.isKinematic = false;
        rB.velocity = (-targetDirPlanar * 10f) + new Vector3(0f, 6f, 0f);
        rB.angularVelocity = Vector3.zero;
        yield return null;
    }

    //this is needed to be called WHILE ON THE GROUND for a smoother start to the gliding
    private void LocationMemoryUpdate()
    {
        locationMemoryTimer += Time.unscaledDeltaTime;
        if (locationMemoryTimer >= locationMemoryUpdateRate)
        {
            locationMemoryTimer = 0f;
            previousPositionList.Insert(0, this.transform.position);
        }
    }

    public IEnumerator GlideBoost()
    {
        float time = 0.5f;
        float timer = 0f;

        while (timer < time)
        {
            float y = Sinerp(glideHeightBoost, 0f, timer / time);
            rB.velocity = new Vector3(rB.velocity.x, y, rB.velocity.z);
            timer += Time.deltaTime;
            yield return null;
        }
        rB.velocity = new Vector3(rB.velocity.x, 0f, rB.velocity.z);
        yield return null;
    }

    /*private void TempCameraUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.current;
        }
        if (isGliding || isDiving)
        {
            mainCamera.transform.transform.position = this.transform.position + (Vector3.ProjectOnPlane(this.transform.forward, Vector3.up).normalized * offsetBehind) + (Vector3.up * offsetHeight);
            mainCamera.transform.LookAt(this.transform.position, Vector3.up);
        }
        else
        {
            mainCamera.transform.transform.position = this.transform.position + new Vector3(0f, 5f, -10f);
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.transform.Rotate(Vector3.right, 25f);
        }
    }*/

    private void TargetUpdate()
    {
        targetTimer -= Time.unscaledDeltaTime;
        if (targetTimer <= 0f)
        {
            diveTarget = null;
            targetTimer = targetUpdateRate;

            Camera camera = (Camera.current != null) ? Camera.current : Camera.main;
            Vector3 camDir = camera.transform.forward;
            float closestAngle = 1000f;

            for (int i = 0; i < validTargets.Count; i++)
            {
                Vector3 targetDir = validTargets[i].position - camera.transform.position;
                float targetAngle = Vector3.Angle(camDir, targetDir.normalized);

                //range check
                if (Vector3.Distance(validTargets[i].position, this.transform.position) < targetDistanceTolerance)
                {
                    //angle check
                    if (targetAngle < targetAngleTolerance)
                    {
                        //best candidate check
                        if (targetAngle < closestAngle)
                        {
                            diveTarget = validTargets[i];
                            closestAngle = targetAngle;
                            targetIndicator.transform.position = diveTarget.transform.position + new Vector3(0f, 1.5f, 0f);
                        }
                    }
                }
            }
        }
    }

    private float Sinerp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
    }

    private float Remap(float value, float inputMin, float inputMax, float outputMin, float outputMax, bool clamp)
    {
        float f = (outputMin + ((value - inputMin) * (outputMax - outputMin)) / (inputMax - inputMin));
        if (clamp)
            f = Mathf.Clamp(f, outputMin, outputMax);
        return f;
    }

    private Vector3 Sinerp(Vector3 start, Vector3 end, float value)
    {
        return new Vector3(Mathf.Lerp(start.x, end.x, Mathf.Sin(value * Mathf.PI * 0.5f)), Mathf.Lerp(start.y, end.y, Mathf.Sin(value * Mathf.PI * 0.5f)), Mathf.Lerp(start.z, end.z, Mathf.Sin(value * Mathf.PI * 0.5f)));
    }

    private float Coserp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
    }

    private Vector3 Coserp(Vector3 start, Vector3 end, float value)
    {
        return new Vector3(Coserp(start.x, end.x, value), Coserp(start.y, end.y, value), Coserp(start.z, end.z, value));
    }
}