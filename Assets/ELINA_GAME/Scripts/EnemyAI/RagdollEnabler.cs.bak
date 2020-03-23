using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollEnabler : MonoBehaviour
{
    HybridAnimancerComponent hybridAnimancerComponent;
    CharacterController cc;
    List<LimbInformation> Limbs;

    public bool targetable;
    private float fixedHeight;
    private float floorBodyHeightGap = 0.025f;

    public float DurationToBlend;
    public float TimeUntilStandingUp;
    public LayerMask LayerForLimbs;
    public bool ModelIsJanky;
    [HideInInspector] public CurrentState state = CurrentState.Enabled;
    [HideInInspector] public bool animRagdollFlag = false;

    float blendStartTime = -100;
    float timeSpendOnGround;
    public bool characterControllerDisabled = false;

    Vector3 hipPosition;
    Vector3 headPosition;
    Vector3 feetPosition;
    Vector3 originalRoot;

    private void Awake()
    {
        targetable = true;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Vector3 dir = transform.position - IAmElina.ELINA.transform.position;
            AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Head), dir, 25, 0.1f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Chest), -transform.forward, 30, 0);
            AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips), -transform.forward, 30, 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Vector3 dir = transform.position - IAmElina.ELINA.transform.position;
            AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Chest), dir + Vector3.up, 50, 0);
            AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips), dir + Vector3.up, 40, 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            BeginRagdoll();
        }

        GetUpFromRagdoll();
    }

    public void DebugShowOff()
    {
        Vector3 dir = transform.position - IAmElina.ELINA.transform.position;
        AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Chest), dir + Vector3.up, 50, 0);
        AddForceToLimb(hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips), dir + Vector3.up, 40, 0);
    }
    void Start()
    {
        // Get the animator and the character controller
        hybridAnimancerComponent = GetComponent<HybridAnimancerComponent>();
        if (hybridAnimancerComponent == null)
        {
            hybridAnimancerComponent = GetComponentInChildren<HybridAnimancerComponent>();
        }
        cc = GetComponent<CharacterController>();

        // Store Limb information for the character
        Limbs = new List<LimbInformation>();
        Component[] components = GetComponentsInChildren(typeof(Transform));
        foreach (Component c in components)
        {
            if (c.GetComponent<Rigidbody>() != null)
            {
                LimbInformation bodyPart = new LimbInformation();
                bodyPart.Transform = c as Transform;
                Limbs.Add(bodyPart);
            }
        }
    }

    void LateUpdate()
    {
        if (state == CurrentState.Blending)
        {
            UpdateRootPosition();
            UpdateRootRotation();

            float blendAmount = 1.0f - (Time.time - blendStartTime) / DurationToBlend;
            blendAmount = Mathf.Clamp01(blendAmount);

            // Blend back to the original rotation / positions of the bones
            foreach (LimbInformation limb in Limbs)
            {
                if (limb.Transform != transform)
                {
                    // Only adjust the position of the Hip bone, and do not adjust its rotation
                    if (limb.Transform == hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips))
                    {
                        limb.Transform.position = Vector3.Lerp(limb.Transform.position, limb.OriginalPosition, blendAmount);
                        continue;
                    }

                    // Adjust the rotation of the bone
                    limb.Transform.rotation = Quaternion.Slerp(limb.Transform.rotation, limb.OriginalRotation, blendAmount);
                }
            }

            // Blending is complete and we can renable the animator
            if (blendAmount <= 0)
            {
                state = CurrentState.Enabled;
        }
        }
        else if (state == CurrentState.Enabled)
        {
            if (hybridAnimancerComponent.GetCurrentAnimatorStateInfo(0).IsName("Idle") && hybridAnimancerComponent.GetBool("Ragdolled"))
            {
                targetable = true;
                hybridAnimancerComponent.SetBool("Ragdolled", false);
                animRagdollFlag = false;
                timeSpendOnGround = 0;
            }
        }
    }

    #region == Exposed Ragdolling functions ==
    /// <summary>
    /// Puts the character into a ragdoll state
    /// </summary>
    public void BeginRagdoll()
    {
        EnableRagdoll();
        state = CurrentState.Disabled;
    }
    
    /// <summary>
    /// Blends from the ragdoll into the animation state provided
    /// </summary>
    /// <param name="AnimationToBlendInto"></param>
    public void BlendFromRagdoll(string AnimationToBlendInto)
    {
        if (state == CurrentState.Enabled)
        {
            timeSpendOnGround = 0;
            return;
        }

        DisableRagdoll();
        timeSpendOnGround = 0;
        blendStartTime = Time.time;
        state = CurrentState.Blending;
        originalRoot = transform.position;

        foreach (LimbInformation limb in Limbs)
        {
            limb.OriginalRotation = limb.Transform.rotation;
            limb.OriginalPosition = limb.Transform.position;
        }

        feetPosition = 0.5f * (hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.LeftToes).position + hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.RightToes).position);
        headPosition = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Head).position;
        hipPosition = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).position;
        hybridAnimancerComponent.SetBool("Ragdolled", true);
        animRagdollFlag = true;
        if (AnimationToBlendInto != null)
        {
            AnimancerState state = hybridAnimancerComponent.Play(AnimationToBlendInto);
            fixedHeight = transform.position.y + floorBodyHeightGap;
        }
        else
        {
            targetable = true;
            hybridAnimancerComponent.PlayController();
        }
    }

    /// <summary>
    /// Attempts to get back up after being ragdolled if the body is on the ground
    /// </summary>
    public void GetUpFromRagdoll()
    {
        if (state == CurrentState.Enabled)
        {
            timeSpendOnGround = 0;
            return;
        }

        // Get the heights of the hip and head bones so we can compare the distance between them
        float hipHeight = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
        float headHeight = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Head).position.y;
        
        // If the difference between the heights of the head and hips is less than 20% of the character controllers height
        // and the body has been on the ground longer than the required duration, stand the character up
        if (Mathf.Abs(hipHeight - headHeight) <= cc.height * 0.2f && timeSpendOnGround > TimeUntilStandingUp)
        {
            timeSpendOnGround = 0;
            if (ModelIsJanky)
            {
                if (-hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).forward.y > 0)
                    BlendFromRagdoll("Get Up From Back");
                else
                    BlendFromRagdoll("Get Up From Chest");
            }
            else
            {
                if (hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).forward.y > 0)
                    BlendFromRagdoll("Get Up From Back");
                else
                    BlendFromRagdoll("Get Up From Chest");
            }
        }
        else if (state == CurrentState.Disabled)
        {
            // If the body is barely moving, meaning its probably laying somewhere. Increase the timer til standing up
            if (hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).GetComponent<Rigidbody>().velocity.magnitude < 0.5f)
                timeSpendOnGround += Time.deltaTime;

            if (timeSpendOnGround > TimeUntilStandingUp * 2)
            {
                BlendFromRagdoll(null);
                timeSpendOnGround = 0;
            }
        }
    }

    /// <summary>
    /// Enables Ragdolling and adds a force to the provided limb, if provided time is greater 
    /// than 0 the ragdoll will blend back into the animation at the end of the duration, otherwise the ragdoll
    /// will behave normally and pick itself up off the floor when it is able to.
    /// </summary>
    /// <param name="Limb"></param>
    /// <param name="ForceDirection"></param>
    /// <param name="Force"></param>
    /// <param name="TimeTilEnd"></param>
    public void AddForceToLimb(Transform Limb, Vector3 ForceDirection, float Force, float TimeTilEnd)
    {
        BeginRagdoll();
        Limb.GetComponent<Rigidbody>().AddForce(ForceDirection.normalized * Force, ForceMode.Impulse);
        if (TimeTilEnd > 0)
        {
            StopAllCoroutines();
            StartCoroutine(DisableRagdollAfterTime(TimeTilEnd));
        }
    }
    IEnumerator DisableRagdollAfterTime(float TimeTilEnd)
    {
        // Move to the specified object
        float Duration = TimeTilEnd;
        while (Duration > 0)
        {
            Duration -= Time.deltaTime;
            yield return null;
        }
        BlendFromRagdoll(null);
    }
    #endregion

    #region == Internal Enabling / Disabling Ragdoll == 
    void EnableRagdoll()
    {
        hybridAnimancerComponent.enabled = false;
        Collider[] colliders = transform.GetComponentsInChildren<Collider>();
        targetable = false;
        timeSpendOnGround = 0;

        foreach (Collider col in colliders)
        {
            if (col.gameObject.layer == ToLayerNumber(LayerForLimbs))
            {
                col.isTrigger = false;
                col.attachedRigidbody.isKinematic = false;
            }
        }
    }
    void DisableRagdoll()
    {
        hybridAnimancerComponent.enabled = true;
        Collider[] colliders = transform.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col.gameObject.layer == ToLayerNumber(LayerForLimbs))
            {
                col.isTrigger = true;
                col.attachedRigidbody.isKinematic = true;
            }
        }
    }
    #endregion

    #region == Helpers ==
    /// <summary>
    /// Calculates and moves the root to its new position based off the ragdolls current position
    /// </summary>
    void UpdateRootPosition()
    {
        // Find where we need to move the root of the character to
        Vector3 animatedToRagdolled = hipPosition - hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 calculatedRootPosition = transform.position + animatedToRagdolled;

        // Find the Height of the ground
        RaycastHit[] hits = Physics.RaycastAll(new Ray(calculatedRootPosition, Vector3.down));
        calculatedRootPosition.y = 0;
        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(transform))
                calculatedRootPosition.y = Mathf.Max(calculatedRootPosition.y, hit.point.y);
        }
        float diff = Mathf.Abs(fixedHeight - calculatedRootPosition.y);
        if (diff > 5.0f)
        {
            Debug.Log("Check yo self!" + diff);
        }
        // Only move the root if the distance is significant, this way we dont produce needless jitter
        if (Vector3.Distance(transform.position, calculatedRootPosition) > cc.height * 0.2f)
        {
            // method 1 it will be neat allows falling gravity
            //calculatedRootPosition.y = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).position.y;

            // for when im on the floor this is accurate
            calculatedRootPosition.y = fixedHeight;
            cc.enabled = false;
            cc.transform.position = calculatedRootPosition;
            cc.enabled = true;
            transform.position = calculatedRootPosition;
    }
    }

    /// <summary>
    /// Calculates and rotates the root while blending to maintain stability
    /// </summary>
    void UpdateRootRotation()
    {
        // Find the heights of the hips and head which will be used to calculate how much the body is leaning / laying
        float hipHeight = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
        float headHeight = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Head).position.y;

        Vector3 directionBeforeBlending = headPosition - feetPosition;
        directionBeforeBlending.y = 0;
        Vector3 betweenFeetPosition = 0.5f * (hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
        Vector3 currentDirection = hybridAnimancerComponent.Animator.GetBoneTransform(HumanBodyBones.Head).position - betweenFeetPosition;
        currentDirection.y = 0;

        if (Mathf.Abs(hipHeight - headHeight) <= cc.height * 0.2f || Vector3.Distance(transform.position, originalRoot) > cc.height * 0.5)
            transform.rotation *= Quaternion.FromToRotation(currentDirection, directionBeforeBlending);
    }

    /// <summary>
    /// Gets the layer number from the layer mask provided
    /// </summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    public static int ToLayerNumber(LayerMask mask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((1 << i) == mask.value)
                return i;
        }
        return -1;
    }

    public class LimbInformation
    {
        public Transform Transform;
        public Vector3 OriginalPosition;
        public Quaternion OriginalRotation;
    }

    public enum CurrentState
    {
        Enabled,
        Disabled,
        Blending
    }
    #endregion
}
