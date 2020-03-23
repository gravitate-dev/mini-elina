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
    [HideInEditorMode]
    public string leadAttackerGO_ID;
    [HideInEditorMode]
    public int currentAttackerCount;
    [HideInEditorMode]
    public int sexLeaderGO_ID;

    private HMove currentMove;
    private static string[] maleParts = new string[] { "ass","flat","foot","hand","mouth","penis","stomach" };
    private static string[] femaleParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "pussy", "stomach" };
    private static string[] futaParts = new string[] { "ass", "boobs", "foot", "hand", "mouth", "penis", "pussy", "stomach" };
    
    /// <summary>
    /// Add penis here if you have the futa spell
    /// </summary>
    private HentaiAnimatorController animatorController;
    private HentaiMoveSystem hentaiMoveSystem;
    private bool knockedDown;
    private int GO_ID;

    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        hentaiMoveSystem = GameObject.FindObjectOfType<HentaiMoveSystem>();
        animatorController = GetComponent<HentaiAnimatorController>();
        disposables.Add(WickedObserver.AddListener("onSexyTimeJoinRequest:" + GO_ID, onSexyTimeJoinRequest));
        disposables.Add(WickedObserver.AddListener("onSexyTimeEventMessage:" + GO_ID, onSexyTimeEventMessage));
        disposables.Add(WickedObserver.AddListener("onStateKnockedOut:" + GO_ID, (obj)=> { knockedDown = true;}));
        disposables.Add(WickedObserver.AddListener("onStateRegainControl:" + GO_ID, (obj)=>
        {
            knockedDown = false;
            stopAllSex();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj)=>
        {
            currentMove = new HMove((HMove)obj); // update loop for hentai moves
        }));
        disposables.Add(WickedObserver.AddListener("StartMasterbating:" + GO_ID, StartMasterbating));
        disposables.Add(WickedObserver.AddListener("OnDeath:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("onStateKnockedOut:" + GO_ID);
        }));

        // every 2 seconds refresh all attackers due to the orgasm % changing
        InvokeRepeating("resynchronizeAllAttackers", 0, 1f);
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    public void setGender(int gender)
    {
        genderId = gender;
        WickedObserver.SendMessage("OnGenderChange:" + GO_ID, gender);
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
        public int senderGO_ID;
        public string nameOfDeviceOn;
        public string[] availableParts;
        public bool isLead;

        public Vector3 sexLocation;
        public Quaternion sexRotation;
    }


    /// <summary>
    /// Documentation of mechanism
    /// When player is knockedDown, all enemies send a SexyTimeJoinReq,
    /// The victim then replies to the first one it hears as isLead=true;
    /// The lead attacker then will reply with a sexMove,
    /// </summary>

    // called by the invector AI
    public void sendSexyTimeRequest(int victimGO_ID)
    {
        SexyTimeJoinRequest request = new SexyTimeJoinRequest();
        request.requestor = transform;
        request.senderGO_ID = GO_ID;
        request.availableParts = getAvailableParts();
        WickedObserver.SendMessage("onSexyTimeJoinRequest:" + victimGO_ID, request);
    }

    private void onSexyTimeJoinRequest(object message)
    {
        SexyTimeJoinRequest request = (SexyTimeJoinRequest)message;
        // only first time leader gets to decide what to do with me
        if (sexLeaderGO_ID == 0 || (currentMove != null && currentMove.interruptable))
        {
            sexLeaderGO_ID = request.senderGO_ID;
            Debug.Log("Victim : " + GO_ID + " is now " + request.senderGO_ID + "'s bitch!");
            SexyTimeJoinResponse response = new SexyTimeJoinResponse();
            if (currentMove!=null && currentMove.location != null && currentMove.location.Equals("attacker"))
            {
                response.sexLocation = request.requestor.transform.position;
                response.sexRotation = request.requestor.transform.rotation;
            }
            else
            {
                response.sexLocation = transform.position;
                response.sexRotation = transform.rotation;
            }
            response.requestorGO_ID = request.senderGO_ID;
            response.senderGO_ID = GO_ID; // i am sending a message
            response.nameOfDeviceOn = nameOfDeviceOn;
            response.availableParts = getAvailableParts(); // get victims available fuck parts
            response.isLead = true;
            WickedObserver.SendMessage("onSexyTimeJoinResponse:"+request.senderGO_ID, response);
            return;
        }

        if (sexLeaderGO_ID != request.senderGO_ID && currentMove != null)
        {
            Debug.Log("I WANNA JOIN!");
            // if the requestor can join then we notify
            int attackerIdx = canJoinInTheSex(request.senderGO_ID, request.availableParts);
            if (attackerIdx == -1)
            {
                Debug.Log("No room for you");
                return;
            }
            //since a new attacker joined everyone should sync on the same HMove
            currentMove.attackers[attackerIdx].GO_ID = request.senderGO_ID;
            if (GO_ID == currentMove.victim.GO_ID)
            {
                resynchronizeAllAttackers();
            } else
            {
                Debug.LogError("I am not a victim i can not synchronize everyone");
            }
        }
    }

    /// <summary>
    /// This is only called from the victim
    /// </summary>
    public void stopAllSex()
    {
        sexLeaderGO_ID = 0;
        if (currentMove != null)
        {
            WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + currentMove.victim.GO_ID);
            if (currentMove.attackers != null)
            {
                for (int i = 0; i < currentMove.attackers.Length; i++)
                {
                    WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + currentMove.attackers[i].GO_ID);
                }
            }
        } else
        {
            Debug.LogError("Critical, there is no h move going on we need to evacuate all players");
            WickedObserver.SendMessage(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + IAmElina.ELINA.GetInstanceID());
        }
        currentMove = null;
        
    }
    // only the victim does this
    public void resynchronizeAllAttackers()
    {
        if (currentMove == null)
        {
            return;
        }
        if (currentMove.victim.GO_ID != GO_ID)
        {
            return;
        }
        HMove animatorMove = animatorController.currentHMove;
        if (animatorMove != null)
        {
            currentMove.loopCountSync = animatorMove.loopCountSync;
            currentMove.playClimaxSync = animatorMove.playClimaxSync;
            currentMove.sceneIndexSync = animatorMove.sceneIndexSync;
        }
        if (currentMove.attackers != null)
        {
            for (int i = 0; i < currentMove.attackers.Length; i++)
            {
                int go_id = currentMove.attackers[i].GO_ID;
                WickedObserver.SendMessage("onStartHentaiMove:" + go_id, currentMove);
            }
        }
        // send one for the victim itself
        WickedObserver.SendMessage("onStartHentaiMove:" + GO_ID, currentMove);
    }
    private void onSexyTimeEventMessage(object message)
    {
        SexyTimeEventMessage data = (SexyTimeEventMessage)message;
        if (data.eventId == SexyTimeEventMessage.EVENT_START_H_MOVE)
        {
            currentMove = new HMove(data.move);
            /**
             * If location needs to be set for attacker, do so in the <see cref="HentaiDialogOptionEvent"/>
             **/
            if (currentMove.location == null || !currentMove.location.Equals("attacker"))
            {
                currentMove.sexLocationPosition = transform.position;
                currentMove.sexLocationRotation = transform.rotation;
            }
            resynchronizeAllAttackers();
        } else if (data.eventId == SexyTimeEventMessage.EVENT_FREE_FROM_LEAD)
        {
            stopAllSex();
        }
    }

    public void sendSTEM_stopHentaiMove()
    {
        if (currentMove == null)
        {
            return;
        }
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_FREE_FROM_LEAD;
        message.senderGO_ID = GO_ID;
        string topic = "onSexyTimeEventMessage:" + currentMove.victim.GO_ID;
        WickedObserver.SendMessage(topic, message);
    }

    // send the move you are a leader
    // if you are not a leader, you can send the move as a direct call from player!
    public void sendSTEM_sendHMove(HMove move)
    {
        // only lead attacker sends this move
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_START_H_MOVE;
        message.move = move;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + move.victim.GO_ID, message);
    }

    public void sendTieUp(int victimGO_ID)
    {
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_TIE_UP;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
    }

    /// <summary>
    /// Useful for gallery mode, and solo masterbation moves
    /// </summary>
    /// <param name="soloMove"></param>
    public void sendSelfHMove(HMove hMove)
    {

    }




    /// <summary>
    /// While this target is being sexed, this will check if another enemy can join in
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="avaibleParts"></param>
    /// <returns>Returns -1 if they can not participate otherwise it will return  the id of the attacker</returns>
    public int canJoinInTheSex(int AttackerGO_ID, string[] avaibleParts)
    {
        for (int i = 0; i < currentMove.attackers.Length; i++)
        {
            Attacker possibility = currentMove.attackers[i];
            if (possibility.GO_ID != 0)
            {
                continue;
            }
            foreach (string part in avaibleParts)
            {
                if (part.Equals(possibility.usingPart))
                {
                    possibility.GO_ID = AttackerGO_ID;
                    currentMove.attackers[i].GO_ID = AttackerGO_ID;
                    return i;
                }
            }

        }
        return -1;
    }

    private void StartMasterbating(object message)
    {
        float duration = (float)message;
        if (currentMove == null)
        {
            WickedObserver.SendMessage("OnPreventEscapeByTime:" + GO_ID, duration);
            WickedObserver.SendMessage("OnStartMasterbationCallback", true);
            HMove masterbate = getMasterbationMoveForSelf();
            masterbate.victim.GO_ID = GO_ID;
            sendSTEM_sendHMove(masterbate);
        } else
        {
            // fail!
            WickedObserver.SendMessage("OnStartMasterbationCallback", false);
        }
    }

    private HMove getMasterbationMoveForSelf()
    {
        if (genderId == GENDER_FEMALE || (genderId == GENDER_FUTA && UnityEngine.Random.Range(0, 1.0f) > 0.5f))
        {
            // female
            return hentaiMoveSystem.getFemaleMasterbation();
        } else
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
        if (move == null)
        {
            return false;
        }
        if (move.victim.GO_ID == ELINA_ID)
        {
            return true;
        }
        if (move.attackers == null || move.attackers.Length == 0)
        {
            return false;
        }

        foreach ( Attacker attacker in move.attackers)
        {
            if (attacker.GO_ID == ELINA_ID)
            {
                return true;
            }
        }
        return false;
    }

}
