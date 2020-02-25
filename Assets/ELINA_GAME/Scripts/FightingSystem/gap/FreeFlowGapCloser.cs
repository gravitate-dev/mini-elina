using Animancer;
using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

/// <summary>
/// Physics class to move a character towards a target
/// </summary>
public class FreeFlowGapCloser : MonoBehaviour
{
    private string basePath = "Assets\\Resources\\GapClosers\\";
    private HybridAnimancerComponent animancer;
    // Movement speed in units per second.
    public float speed = 7.0F;

    // good for debugging
    private float travelTimeLeft = 0;
    private float totalTravelTime = 0;
    // good for debugging

    // Time when the movement started.
    private float startTime;
    private Vector3 startPosition;

    // Total distance between the markers.
    private float journeyLength;
    private float stopDistance;
    private GameObject target;
    private bool playedAttackAnimationYet;
    private AnimationClipHandler animationClipHandler;

    // this prevents the AI from moving off target when we reach it
    private const float DISABLE_AI_AT_CLOSEST_DISTANCE = 4.0f;
    private class GapCloserStyle
    {
        public bool disabled;

        public string clipName;
        public string gapCloserType; // ground, air

        [DefaultValue(1)]
        public float animationSpeed;
        public float travelTimeRequired;
        [DefaultValue(1)]
        public float playerSpeedMultiplier;

        public bool isPossible(float distanceAvailable, float playerDefaultSpeed)
        {
            // d = rt
            float travelSpeed = Mathf.Max(playerDefaultSpeed, playerSpeedMultiplier);
            float animatedDistanceNeeded = travelTimeRequired * travelSpeed;
            return distanceAvailable >= animatedDistanceNeeded;
        }
    }
    //private GapCloserStyle gapCloserSparta = new GapCloserStyle("GapCloserSpartanKick",/*animationTimeReq= */0.3f);
    
    private List<GapCloserStyle> gapClosers = new List<GapCloserStyle>();
    private GapCloserStyle chosenGapCloserStyle;
    private bool usingGapCloser;

    private const float GAP_CLOSER_COOLDOWN = 20.0f;

    private float gapCloserAllowedTime;
    private FreeFlowGapListener freeFlowGapListener;
    private bool freeFlowIsLeaping;

    void Start()
    {
        animationClipHandler = AnimationClipHandler.INSTANCE; 
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.DefaultValueHandling = DefaultValueHandling.Populate;
        string[] fileNames = Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories);
        foreach (string fname in fileNames)
        {
            string json = File.ReadAllText(fname);
            List<GapCloserStyle> moves = JsonConvert.DeserializeObject<List<GapCloserStyle>>(json, settings);
            foreach (GapCloserStyle move in moves)
            {
                if (move.disabled)
                {
                    continue;
                }
                // debug this later
                gapClosers.Add(move);
            }
        }

        gapClosers.Sort(new SortTimeRequired());
        animancer = GetComponent<HybridAnimancerComponent>();
    }

    private class SortTimeRequired : IComparer<GapCloserStyle>
    {
        public int Compare(GapCloserStyle x, GapCloserStyle y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return 1; // Equal
            if (x != null && y == null) return -1; // Equal

            //todo sort on ideal or nonIdealDistance?
            return x.travelTimeRequired.CompareTo(y.travelTimeRequired);
        }
    }

    // Update is called once per frame
    // note that if player enemy physics allows collision we can only get 0.55 close , safe value 0.6 anything below wont work
    void FixedUpdate()
    {
        if (IsLeaping() && target != null)
        {
            animancer.SetFloat("InputMagnitude", 1.0f);
            float elapsedTime = Time.time - startTime;
            travelTimeLeft = totalTravelTime - elapsedTime;
            // Distance moved equals elapsed time times speed..
            float distCovered = elapsedTime * speed;

            // Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / journeyLength;
            float distanceLeft = Vector3.Distance(transform.position, target.transform.position);
            transform.LookAt(target.transform);
            
            if (chosenGapCloserStyle!=null && !playedAttackAnimationYet)
            {
                if (travelTimeLeft <= chosenGapCloserStyle.travelTimeRequired)
                {
                    gapCloserAllowedTime = Time.time + GAP_CLOSER_COOLDOWN;
                    playedAttackAnimationYet = true;
                    AnimancerState gapAnimancerState = animancer.Play(animationClipHandler.ClipByName(chosenGapCloserStyle.clipName));
                    gapAnimancerState.Time = 0;
                    gapAnimancerState.Speed = chosenGapCloserStyle.animationSpeed;
                    gapAnimancerState.Events.OnEnd = ReturnToNormal;
                    usingGapCloser = true;
                }
            }
            //Debug.Log("DISTANCE LEFT" + distanceLeft);
            if (distanceLeft < stopDistance)
            {
                freeFlowIsLeaping = false;
                animancer.SetFloat("InputMagnitude", 0f);
                ReachedDestination(true);
            }

            transform.position = Vector3.Lerp(startPosition, target.transform.position, fractionOfJourney);
        }
    }

    /// <summary>
    /// Moves to the target then emits an event when the distance was reached
    /// </summary>
    /// <param name="target"></param>
    public void MoveToTargetForAttack(FreeFlowAttackMove attack, FreeFlowGapListener freeFlowGapListener)
    {
        if (IsLeaping() || attack == null)
        {
            ReachedDestination(false);
            return;
        }
        this.freeFlowGapListener = freeFlowGapListener;
        stopDistance = attack.idealDistance;
        startPosition = transform.position;

        target = attack.victim;
        journeyLength = Vector3.Distance(startPosition, target.transform.position);

        // when we are within striking range already, no need to travel
        if (journeyLength <= attack.idealDistance)
        {
            ReachedDestination(true);
            return;
        }

        totalTravelTime = (journeyLength - stopDistance) / speed;
        travelTimeLeft = totalTravelTime;
        startTime = Time.time;
        freeFlowIsLeaping = true;


        if (gapCloserAllowedTime > Time.time)
        {
            //cooldown for next gap closer
            return;
        }
        List<GapCloserStyle> possibleClosers = new List<GapCloserStyle>();
        foreach (GapCloserStyle closer in gapClosers)
        {
            if (closer.isPossible(journeyLength - stopDistance,speed))
            {
                possibleClosers.Add(closer);
            }   
        }

        // if we cant find a gapcloser we just walk there
        if (possibleClosers.Count!=0)
        {
            int randomInt = UnityEngine.Random.Range(0, possibleClosers.Count);
            chosenGapCloserStyle = possibleClosers[randomInt];
            playedAttackAnimationYet = false;
        }
    }

    /// <summary>
    /// Moves to the target then emits an event when the distance was reached
    /// </summary>
    /// <param name="target"></param>
    public void MoveToTargetForSex(GameObject ffTarget, FreeFlowGapListener freeFlowGapListener)
    {
        this.freeFlowGapListener = freeFlowGapListener;
        if (IsLeaping() || ffTarget == null)
        {
            return;
        }
        stopDistance = 1.0f;
        startPosition = transform.position;

        target = ffTarget;
        journeyLength = Vector3.Distance(startPosition, target.transform.position);

        // when we are within striking range already, no need to travel
        if (journeyLength <= stopDistance)
        {
            ReachedDestination(true);
            return;
        }

        totalTravelTime = (journeyLength - stopDistance) / speed;
        travelTimeLeft = totalTravelTime;
        startTime = Time.time;

        List<GapCloserStyle> possibleClosers = new List<GapCloserStyle>();
        foreach (GapCloserStyle closer in gapClosers)
        {
            if (closer.isPossible(journeyLength - stopDistance, speed))
            {
                possibleClosers.Add(closer);
            }
        }

        // if we cant find a gapcloser we just walk there
        if (possibleClosers.Count != 0)
        {
            int randomInt = UnityEngine.Random.Range(0, possibleClosers.Count);
            chosenGapCloserStyle = possibleClosers[randomInt];
            playedAttackAnimationYet = false;
        }
        // Calculate the journey length.
        freeFlowIsLeaping = true;
    }

    private void ReachedDestination(bool success)
    {
        if (freeFlowGapListener == null)
        {
            Debug.LogError("SEVERE: FreeFlowGapListener was null");
        }
        if (success)
        {
            freeFlowGapListener.onReachedDestination();
        } else
        {
            freeFlowGapListener.onReachedDestinationFail();
        }
    }

    
    public bool IsLeaping()
    {
        return freeFlowIsLeaping;
    }

    private void ReturnToNormal()
    {
        usingGapCloser = false;
        animancer.Play(Animator.StringToHash("Free Locomotion"));
        freeFlowIsLeaping = false;
        animancer.SetFloat("InputMagnitude", 0f);
        ReachedDestination(true);
    }
}
