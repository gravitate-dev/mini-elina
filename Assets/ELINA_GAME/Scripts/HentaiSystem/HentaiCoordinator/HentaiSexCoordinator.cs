﻿using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HentaiSexyTimeEventMessenger;
using static HMove;

/// <summary>
/// HentaiSexCoordinator. Every character has one
/// They listen for when the character is sexable
/// While sexable it listens for any requests
/// The director will pick from the attackerList, 
/// </summary>
public class HentaiSexCoordinator : MonoBehaviour
{
    public const string EVENT_STOP_H_MOVE_LOCAL = "EVENT_STOP_H_MOVE:";
    /// <summary>
    /// Change this via the setGenderId
    /// </summary>
    [ValueDropdown("GenderValues")]
    public int genderId;

    private static IEnumerable GenderValues = new ValueDropdownList<int>()
{
    { "Unpicked", 1 },
    { "Male", 2 },
    { "Female", 3 },
    { "Futa", 4 },
};

    public static int GENDER_UNPICKED = 1;
    public static int GENDER_MALE = 2;
    public static int GENDER_FEMALE = 3;
    public static int GENDER_FUTA = 4;

    [InfoBox("Only for devices for the hmoves json files")]
    public string deviceId;

    [HideInEditorMode]
    public string nameOfDeviceOn;

    // do not set this
    private HMove currentMove;
    private static string[] maleParts = new string[] { "ass", "flat", "foot", "hand", "mouth", "penis", "stomach" };
    private static string[] femaleParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "pussy", "stomach" };
    private static string[] futaParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "penis", "pussy", "stomach" };

    /// <summary>
    /// Add penis here if you have the futa spell
    /// </summary>
    private HentaiAnimatorController animatorController;
    private HentaiMoveSystem hentaiMoveSystem;
    private HentaiHeatController hentaiHeatController;
    private bool heartBeatPause;
    private int GO_ID;

    //optional
    private BlendShapeItemCharacterController bsicc;

    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        bsicc = GetComponent<BlendShapeItemCharacterController>();
        hentaiMoveSystem = GameObject.FindObjectOfType<HentaiMoveSystem>();
        hentaiHeatController = GetComponent<HentaiHeatController>();
        animatorController = GetComponent<HentaiAnimatorController>();
        disposables.Add(WickedObserver.AddListener("onStateRegainControl:" + GO_ID, (obj) =>
        {
            StopAllSexIfAny();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            currentMove = new HMove((HMove)obj); // update loop for hentai moves
        }));
        disposables.Add(WickedObserver.AddListener("OnDeath:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("onStateKnockedOut:" + GO_ID);
        }));
        disposables.Add(WickedObserver.AddListener(EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused) =>
         {
             currentMove = null;
         }));

        // every 2 seconds refresh all attackers due to the orgasm % changing
        InvokeRepeating("HentaiHeartBeat", 0, 1f);
    }

    private void HentaiHeartBeat()
    {
        if (currentMove == null)
        {
            if (hentaiHeatController != null && hentaiHeatController.currentLust > 1)
            {
                StartMasterbating();
            }
            return;
        }
        if (!animatorController.CanSkipCurrentSceneWithHeartBeat())
        {
            return;
        }

        // check if victim is gone
        if (currentMove.victim.gameObject == null)
        {
            StopAllSexIfAny();
            return;
        }

        // if i am victim keep going
        if (currentMove.victim.gameObject != null && GO_ID == currentMove.victim.gameObject.GetInstanceID())
        {
            ResynchronizeAllSex();
        }
    }

    private void OnDestroy()
    {
        // free others before i die!
        StopAllSexIfAny();
        WickedObserver.RemoveListener(disposables);
    }

    public void setGender(int gender)
    {
        if (genderId != gender)
        {
            WickedObserver.SendMessage("OnGenderChange:" + GO_ID, gender);
        }
        genderId = gender;

    }

    public class SexyTimeJoinRequest
    {
        public Transform requestor;
        public int senderGO_ID;
        public string[] availableParts;
    }

    /// <summary>
    /// Only victims send this to attackers
    /// </summary>
    public class SexyTimeJoinResponse
    {
        /// <summary>
        /// This is the id of the <see cref="SexyTimeJoinRequest.senderGO_ID"/>
        /// </summary>
        public int requestorGO_ID;

        /// <summary>
        /// This is the victim's ID
        /// </summary>
        public GameObject sender;
        public string nameOfDeviceOn;
        public string[] availableParts;
        public bool isLead;

        public Vector3 sexLocation;
        public Quaternion sexRotation;
    }



    public const int TRY_SEX_RESULT_FAIL = 1;
    public const int TRY_SEX_RESULT_JOINED_IN = 2;
    public const int TRY_SEX_RESULT_PICK_MOVE = 3;
    /// <summary>
    /// Attempts to sex the target
    /// </summary>
    /// <param name="victim">target of the sex</param>
    /// <returns>true if success, false if not</returns>
    public int TrySexTarget(GameObject victim)
    {
        if (victim == null)
        {
            return TRY_SEX_RESULT_FAIL;
        }
        HentaiSexCoordinator victimCoordinator = victim.GetComponent<HentaiSexCoordinator>();
        return victimCoordinator.HandleTrySex(victim);
    }

    private int HandleTrySex(GameObject sender)
    {
        if (sender == null)
        {
            return TRY_SEX_RESULT_FAIL;
        }
        HentaiSexCoordinator senderCoordinator = sender.GetComponent<HentaiSexCoordinator>();

        if (IsSexing() && !currentMove.interruptable)
        {
            if (!AmIVictim())
            {
                // wrong person to ask for sex
                return TRY_SEX_RESULT_FAIL;
            }
            // try to join
            // attempt to join the sex
            int openSpaceIdx = currentMove.GetOpenAttackerIndex(sender, senderCoordinator.getAvailableParts());
            if (openSpaceIdx == -1)
            {
                return TRY_SEX_RESULT_FAIL;
            }
            currentMove.attackers[openSpaceIdx].gameObject = sender;
            ResynchronizeAllSex();
            return TRY_SEX_RESULT_JOINED_IN;
        }
        else
        {
            return TRY_SEX_RESULT_PICK_MOVE;
        }
    }

    public void StartNewSexMove(GameObject leadAttacker, HMove move, bool isPlayground)
    {
        StopAllSexIfAny();
        HMove copy = new HMove(move);
        if (copy.location != null && copy.location.Equals("attacker"))
        {
            // location attacker is used for a device!
            copy.sexLocationPosition = leadAttacker.transform.position;
            copy.sexLocationRotation = leadAttacker.transform.rotation;
        }
        else
        {
            // this needs to be grounded
            
            // method #1
            /*Vector3 basePos = transform.position;
            if (bsicc != null)
            {
                basePos.y -= bsicc.currentHeelHeight;
            }
            copy.sexLocationPosition = basePos;
            */

            // method #2
            Vector3 floorPos = GetFloor();
            if (floorPos == Vector3.zero)
            {
                //dont have sex if theres no floor
                StopAllSexIfAny();
                return;
            }
            copy.sexLocationPosition = floorPos;
            copy.sexLocationRotation = transform.rotation;
        }
        copy.attackers[0].gameObject = leadAttacker;
        copy.victim.gameObject = gameObject;
        copy.playground = isPlayground;
        currentMove = copy;
        ResynchronizeAllSex();
    }

    private Vector3 GetFloor()
    {
        Vector3 center = transform.position;
        center.y += 0.2f; // raise up a little
        Debug.DrawRay(center, -Vector3.up * (2.0f), Color.white);
        RaycastHit[] hits = Physics.RaycastAll(center, -Vector3.up, 2.0f);
        float minDistance = float.MaxValue;
        Vector3 floorPointForSex = Vector3.zero;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject.layer == 0) // 0 is floor
            {
                // floor
                if (hit.distance <= minDistance)
                {
                    minDistance = hit.distance;
                    floorPointForSex = hit.point;
                }
            }
        }
        return floorPointForSex;
    }

    public void StartNewSoloMove(HMove move, bool isPlayground)
    {
        StopAllSexIfAny();
        HMove copy = new HMove(move);
        Vector3 basePos = transform.position;
        copy.sexLocationPosition = basePos;
        copy.sexLocationRotation = transform.rotation;
        copy.victim.gameObject = gameObject;
        copy.playground = isPlayground;
        currentMove = copy;
        ResynchronizeAllSex();
    }

    private void StartMasterbating()
    {
        StopAllSexIfAny();
        HMove move;
        if (genderId == GENDER_FEMALE)
        {
            move = HentaiMoveSystem.INSTANCE.getFemaleMasterbation();
        }
        else
        {
            move = HentaiMoveSystem.INSTANCE.getMaleMasterbation();
        }
        StartNewSoloMove(move, false);
    }

    /// <summary>
    /// This is only called from the victim
    /// </summary>
    public void StopAllSexIfAny()
    {
        if (currentMove == null)
        {
            return;
        }
        HMove temp = new HMove(currentMove);
        if (temp.victim.gameObject != null)
        {
            WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + temp.victim.gameObject.GetInstanceID(), currentMove);
        }
        if (temp.attackers != null)
        {
            int length = temp.attackers.Length;
            for (int i = 0; i < length; i++)
            {
                GameObject attackerObj = temp.attackers[i].gameObject;
                if (attackerObj == null)
                {
                    continue;
                }
                int ID = attackerObj.GetInstanceID();
                WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + ID, temp);
            }
        }
        currentMove = null;
    }

    /// <summary>
    /// Only victim refreshses everyone 
    /// </summary>
    /// <param name="isInitial">Only when its the first move started do we turn isSexingOn</param>
    public void ResynchronizeAllSex()
    {
        
        // am i victim?
        if (currentMove == null ||
            currentMove.victim.gameObject == null ||
            currentMove.victim.gameObject.GetInstanceID() != GO_ID)
        {
            return;
        }
        HMove animatorMove = animatorController.currentHMove;

        // check heat for damage
        float orgasmPercentage = hentaiHeatController.getOrgasmPercentage();
        if (orgasmPercentage > 1)
        {
            // orgasm
            animatorMove.playClimax = true;
            hentaiHeatController.ZeroHeat();
        }
        if (animatorMove != null)
        {
            currentMove.loopCountSync = animatorMove.loopCountSync;
            currentMove.playClimax = animatorMove.playClimax;
            currentMove.sceneIndexSync = animatorMove.sceneIndexSync;
        }
        if (currentMove.attackers != null)
        {
            for (int i = 0; i < currentMove.attackers.Length; i++)
            {
                if (currentMove.attackers[i].gameObject == null)
                {
                    Debug.LogError("DEAD MAN FUCKIN");
                    continue;
                }
                int go_id = currentMove.attackers[i].gameObject.GetInstanceID();
                WickedObserver.SendMessage("onStartHentaiMove:" + go_id, currentMove);
            }
        }
        WickedObserver.SendMessage("onStartHentaiMove:" + GO_ID, currentMove);
    }
    public void sendTieUp(int victimGO_ID)
    {
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_TIE_UP;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
    }

    private HMove getMasterbationMoveForSelf()
    {
        if (genderId == GENDER_FEMALE || (genderId == GENDER_FUTA && UnityEngine.Random.Range(0, 1.0f) > 0.5f))
        {
            // female
            return hentaiMoveSystem.getFemaleMasterbation();
        }
        else
        {
            // male
            return hentaiMoveSystem.getMaleMasterbation();
        }
    }
    /// <summary>
    /// Exposed to <see cref="AiHentai"/>
    /// </summary>
    public string[] getAvailableParts()
    {
        List<string> availableParts = new List<string>();
        if (genderId == 0)
        {
            throw new System.Exception(gameObject.name + "'s GENDER WAS UNSPECIFIED, THIS IS NOT ALLOWED");
        }
        else if (genderId == GENDER_MALE)
        {
            availableParts.AddRange(maleParts);
        }
        else if (genderId == GENDER_FEMALE)
        {
            availableParts.AddRange(femaleParts);
        }
        else if (genderId == GENDER_FUTA)
        {
            availableParts.AddRange(futaParts);
        }
        if (deviceId != null)
        {
            availableParts.Add(deviceId);
        }

        return availableParts.ToArray();
    }

    public static bool isPlayerInvolved(HMove move)
    {
        int ELINA_ID = IAmElina.ELINA.GetInstanceID();
        if (move == null || move.victim.gameObject == null)
        {
            return false;
        }
        if (move.victim.gameObject.GetInstanceID() == ELINA_ID)
        {
            return true;
        }
        if (move.attackers == null || move.attackers.Length == 0)
        {
            return false;
        }

        foreach (Attacker attacker in move.attackers)
        {
            if (attacker.gameObject == null)
            {
                continue;
            }
            if (attacker.gameObject.GetInstanceID() == ELINA_ID)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsSexing()
    {
        return currentMove != null && currentMove.scenes != null && currentMove.scenes.Length != 0;
    }

    public bool AmIVictim()
    {
        if (currentMove == null)
        {
            return false;
        }
        if (currentMove.victim.gameObject.GetInstanceID().Equals(gameObject.GetInstanceID()))
        {
            return true;
        }
        return false;
    }
}
