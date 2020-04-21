using Sirenix.OdinInspector;
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
    public const string EVENT_START_H_MOVE_LOCAL = "onStartHentaiMove:";
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

    // do not set this
    public HMove currentMove;
    private bool stopFlag;
    private static string[] maleParts = new string[] { "ass", "flat", "foot", "hand", "mouth", "penis", "stomach" };
    private static string[] femaleParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "pussy", "stomach" };
    private static string[] futaParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "penis", "pussy", "stomach" };

    /// <summary>
    /// Add penis here if you have the futa spell
    /// </summary>
    private HentaiAnimatorController animatorController;
    private HentaiHeatController hentaiHeatController;
    private HentaiLustManager hentaiLustManager;
    private int GO_ID;

    //optional
    private BlendShapeItemCharacterController bsicc;

    private List<System.Guid> disposables = new List<System.Guid>();

    private const float NEW_SEX_LOCK_TIME = 2.0f;
    private float masterbationLockTime;
    private float masterbationLockTimeTotal;

    private float newSexLockTime;
    private float newSexLockTimeTotal = 1.0f;

    private float hentaiheartBeatTime;
    private float hentaiHeartBeatTimeTotal = 2.0f;

    [HideInEditorMode]
    public List<SexProp> sexProps = new List<SexProp>();
    // victim keeps track of the items spawned
    [System.Serializable]
    public class SexProp
    {
        public GameObject instance;
        public GameObject owner;
        public int actor;
        public string name;
    }

    void Awake()
    {
        hentaiheartBeatTime = hentaiHeartBeatTimeTotal;
        GO_ID = gameObject.GetInstanceID();
        bsicc = GetComponent<BlendShapeItemCharacterController>();
        hentaiHeatController = GetComponent<HentaiHeatController>();
        animatorController = GetComponent<HentaiAnimatorController>();
        hentaiLustManager = GetComponent<HentaiLustManager>();
        if (hentaiLustManager == null)
        {
            Debug.LogError("FATAL MISSING LUST MANAGER ON GAMEOBJECT: " + gameObject.name);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            if (gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                gameObject.layer = LayerMask.NameToLayer("GhostEnemy");
            }
            if (GetComponent<EnemyLogic>() != null)
            {
                GetComponent<EnemyLogic>().enabled = false;
            }
            currentMove = new HMove((HMove)obj); // update loop for hentai moves
        }));
        disposables.Add(WickedObserver.AddListener("OnDeath:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("onStateKnockedOut:" + GO_ID);
        }));
        disposables.Add(WickedObserver.AddListener(EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused) =>
         {
             if (GetComponent<EnemyLogic>() != null)
             {
                 if (!GetComponent<FreeFlowTargetable>().defeated)
                 {
                     GetComponent<EnemyLogic>().enabled = true;
                     GetComponent<EnemyLogic>().DisableForDuration(0);
                 }
             }
             if (gameObject.layer == LayerMask.NameToLayer("GhostEnemy"))
             {
                 gameObject.layer = LayerMask.NameToLayer("Enemy");
             }
             stopFlag = false;
             currentMove = null;
         }));

    }

    private void Update()
    {
        if (masterbationLockTime > 0)
        {
            masterbationLockTime -= Time.deltaTime;
        }
        if (newSexLockTime > 0)
        {
            newSexLockTime -= Time.deltaTime;
        }
        hentaiheartBeatTime -= Time.deltaTime;
        if (hentaiheartBeatTime < 0)
        {
            // to monitor orgasm %
            hentaiheartBeatTime = hentaiHeartBeatTimeTotal;
            HentaiHeartBeat();
        }

    }
    private void HentaiHeartBeat()
    {
        if (IsCurrentMoveNull())
        {
            return;
        }
        if (!animatorController.CanSkipCurrentSceneWithHeartBeat())
        {
            return;
        }

        // check if victim is gone or if bugged
        if (stopFlag || currentMove.victim.gameObject == null || currentMove.victim.gameObject.GetComponent<HentaiSexCoordinator>().IsSexing() == false)
        {
            stopFlag = false;
            StopAllSexIfAny();
            return;
        }
        VictimResynchronizeAllSex();
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
            VictimResynchronizeAllSex();
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
            Vector3 floorPos = GetFloor();
            if (floorPos == Vector3.zero)
            {
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
        if (AmIVictim())
        {
            newSexLockTime = NEW_SEX_LOCK_TIME;
            newSexLockTimeTotal = NEW_SEX_LOCK_TIME;
        }
        VictimResynchronizeAllSex();
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

    // WARNING MUST BE CALLED ON MAIN THREAD
    public void StopAllSexIfAny()
    {
        if (IsCurrentMoveNull())
        {
            return;
        }
        /*if (AmIVictim() && IsMasterbating())
        {
            hentaiLustManager.OnStoppedMasterbation();
        }*/
        
        RemoveAllSexProps();

        List<int> objIds = new List<int>();
        if (currentMove.victim.gameObject != null)
        {
            objIds.Add(currentMove.victim.gameObject.GetInstanceID());
        }
        if (currentMove.attackers != null)
        {
            int length = currentMove.attackers.Length;
            for (int i = 0; i < length; i++)
            {
                GameObject attackerObj = currentMove.attackers[i].gameObject;
                if (attackerObj == null)
                {
                    continue;
                }
                int ID = attackerObj.GetInstanceID();
                objIds.Add(ID);
            }
        }
        HMove temp = new HMove(currentMove);
        foreach (int ID in objIds)
        {
            WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + ID, temp);
        }
        currentMove = null;
    }

    public void FlagStopAllSex()
    {
        stopFlag = true;
    }

    /// <summary>
    /// Only victim refreshses everyone 
    /// </summary>
    /// <param name="isInitial">Only when its the first move started do we turn isSexingOn</param>
    public void VictimResynchronizeAllSex()
    {
        if (IsCurrentMoveNull() || !AmIVictim())
        {
            return;
        }
        HMove animatorMove = animatorController.currentHMove;

        HandleSexProps(currentMove);
        if (animatorMove != null)
        {
            //animatorMove.playClimax = hentaiHeatController.ConsumePendingOrgasm();
            currentMove.loopCountSync = animatorMove.loopCountSync;
            currentMove.playClimax = animatorMove.playClimax;
            currentMove.sceneIndexSync = animatorMove.sceneIndexSync;
        }
        if (currentMove != null && currentMove.attackers != null && currentMove.attackers.Length > 0)
        {
            for (int i = 0; i < currentMove.attackers.Length; i++)
            {
                if (currentMove.attackers[i].gameObject == null)
                {
                    continue;
                }
                int go_id = currentMove.attackers[i].gameObject.GetInstanceID();
                WickedObserver.SendMessage("onStartHentaiMove:" + go_id, currentMove);
            }
        }
        WickedObserver.SendMessage("onStartHentaiMove:" + GO_ID, currentMove);
    }

    private void HandleSexProps(HMove move)
    {
        if (move.props == null || move.props.Length == 0)
        {
            RemoveAllSexProps();
            return;
        }
        for (int i = sexProps.Count-1; i>=0; i--)
        {
            SexProp sexProp = sexProps[i];
            if (sexProp == null || sexProp.owner == null)
            {
                sexProps.RemoveAt(i);
                continue;
            }
            bool keep = false;
            foreach(HMove.Prop prop in move.props)
            {
                if (sexProp.actor == prop.actor && sexProp.name.Equals(prop.name)){
                    keep = true;
                    break;
                }
            }
            if (!keep)
            {
                Destroy(sexProp.instance);
                sexProps.RemoveAt(i);
            }
        }

        // now to populate the missing ones
        /*foreach (HMove.Prop prop in move.props)
        {
            bool found = false;
            for (int i = sexProps.Count - 1; i >= 0; i--)
            {
                SexProp sexProp = sexProps[i];
                if (sexProp.actor == prop.actor && sexProp.name.Equals(prop.name))
                {
                    found = true;
                    break;
                }
            }
            if (found)
                continue;
            // initialize sex toy
            if (prop.actor == 0)
            {
                Transform bone = GetTransformForSexProp(currentMove.victim.gameObject.transform,prop.bone);
                if (bone == null)
                {
                    Debug.LogError("Unable to find the bone");
                }
                GameObject newProp = Instantiate(HentaiPropLibrary.GetProp(prop.name), bone);
                SexProp newSexProp = new SexProp();
                newSexProp.owner = currentMove.victim.gameObject;
                newSexProp.name = prop.name;
                newSexProp.instance = newProp;
                newSexProp.actor = prop.actor;
                sexProps.Add(newSexProp);
            } else
            {
                // attacker index is offset by 1
                if (currentMove.attackers[prop.actor - 1].gameObject == null)
                    continue;
                Transform bone = GetTransformForSexProp(currentMove.attackers[prop.actor - 1].gameObject.transform, prop.bone);
                if (bone == null)
                {
                    Debug.LogError("Unable to find the bone");
                }
                GameObject newProp = Instantiate(HentaiPropLibrary.GetProp(prop.name),bone);
                SexProp newSexProp = new SexProp();
                newSexProp.owner = currentMove.attackers[prop.actor - 1].gameObject;
                newSexProp.name = prop.name;
                newSexProp.instance = newProp;
                newSexProp.actor = prop.actor;
                sexProps.Add(newSexProp);
            }
        }*/
    }

    private Transform GetTransformForSexProp(Transform root, string bone)
    {
        Transform start = null;
        if (root.CompareTag("SexDummy"))
        {
            start = root;
        }
        else
        {
            foreach (Transform child in root.transform)
            {
                if (child.CompareTag("SexDummy"))
                {
                    start = child;
                }
            }
        }
        if (start == null)
        {
            Debug.LogError("Could not find sex dummy for sex prop");
            return null;
        }

        /* BFS implementation */
        Queue<Transform> paths = new Queue<Transform>();
        paths.Enqueue(start);
        while (paths.Count != 0)
        {
            Transform curr = paths.Dequeue();
            foreach (Transform t in curr.transform)
            {
                if (t.name.Equals(bone))
                {
                    return t;
                }
                paths.Enqueue(t);
            }
        }
        return null;
    }

    private void RemoveAllSexProps()
    {
        // clear all sex props
        foreach (SexProp sexProp in sexProps)
        {
            Destroy(sexProp.instance);
        }
        sexProps.Clear();
    }
    public void sendTieUp(int victimGO_ID)
    {
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_TIE_UP;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
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
    #region === Masterbate ===
    public bool TryToMasterbateWithMinimumDuration(float minDuration)
    {
        if (IsSexing())
        {
            return false;
        }
        masterbationLockTime = minDuration;
        masterbationLockTimeTotal = minDuration;
        StartMasterbating(false);
        return true;
    }

    private void StartMasterbating(bool isPlayground)
    {
        HMove move;
        if (genderId == GENDER_FEMALE)
        {
            move = HentaiMoveSystem.INSTANCE.getFemaleMasterbation();
        }
        else
        {
            move = HentaiMoveSystem.INSTANCE.getMaleMasterbation();
        }
        if (move == null)
        {
            Debug.Log("Cant masterbate no animations available for " + gameObject.name);
            return;
        }
        StartNewSoloMove(move, isPlayground);
    }

    public void StartNewSoloMove(HMove move, bool isPlayground)
    {
        HMove copy = new HMove(move);
        Vector3 basePos = transform.position;
        if (bsicc != null)
        {
            basePos.y -= bsicc.currentHeelHeight;
        }
        copy.sexLocationPosition = basePos;
        copy.sexLocationRotation = transform.rotation;
        copy.victim.gameObject = gameObject;
        copy.playground = isPlayground;
        currentMove = copy;
        VictimResynchronizeAllSex();
    }
    public bool IsMasterbating()
    {
        if (!IsSexing())
            return false;
        return currentMove.attackers == null || currentMove.attackers.Length == 0;
    }

    #endregion
    

    
    public bool CanEscapeRightNow()
    {
        if (masterbationLockTime >0 && IsMasterbating())
        {
            return false;
        }
        if (newSexLockTime > 0)
        {
            return false;
        }

        return true;
    }
    public float GetEscapeTimePercentageLeft()
    {
        if (masterbationLockTime > 0 && IsMasterbating())
        {
            return masterbationLockTime / masterbationLockTimeTotal;
        }
        if (newSexLockTime > 0)
        {
            return newSexLockTime / newSexLockTimeTotal;
        }
        return 0f;
    }

    public bool IsSexing()
    {
        return currentMove != null && currentMove.scenes != null && currentMove.scenes.Length != 0;
    }
    public bool AmIVictim()
    {
        return GetCurrentActorId() == 0;
    }
    public int GetCurrentActorId()
    {
        if (!IsSexing())
            return -1;
        if (IsCurrentMoveNull())
            return -1;

        // victim check
        if (currentMove.victim.gameObject.GetInstanceID().Equals(gameObject.GetInstanceID()))
        {
            return 0; // i am victim
        }

        // attacker check
        if (currentMove.attackers == null || currentMove.attackers.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < currentMove.attackers.Length; i++)
        {
            Attacker attacker = currentMove.attackers[i];
            if (attacker.gameObject == null)
            {
                continue;
            }
            if (attacker.gameObject.GetInstanceID() == gameObject.GetInstanceID())
            {
                return i;
            }
        }
        return -1;
    }
   
    public bool IsSexPlaygroundMode()
    {
        if (!IsSexing())
        {
            return false;
        }
        return currentMove.playground;
    }

    public bool IsCurrentMoveNull()
    {
        if (currentMove == null || currentMove.victim == null || currentMove.moveName == null)
            return true;
        if (currentMove.scenes == null || currentMove.scenes.Length == 0)
            return true;
        return false;
    }
}
