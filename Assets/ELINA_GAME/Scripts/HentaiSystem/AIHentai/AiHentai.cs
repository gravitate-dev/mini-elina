using System.Collections.Generic;
using UnityEngine;
using static HentaiSexCoordinator;

public class AiHentai : MonoBehaviour
{
    private HentaiSexCoordinator hentaiSexCoordinator;
    private HentaiSexyTimeEventMessenger hentaiSexyTimeEventMessenger;
    private List<System.Guid> disposables = new List<System.Guid>();

    void Awake()
    {
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        if (hentaiSexCoordinator == null)
        {
            throw new System.Exception("Missing hentai sex coordinator");
        }
        int GO_ID = gameObject.GetInstanceID();
        hentaiSexyTimeEventMessenger = new HentaiSexyTimeEventMessenger(GO_ID);
        disposables.Add(WickedObserver.AddListener("onSexyTimeJoinResponse:" + GO_ID, onSexyTimeJoinResponse));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    public void FuckTarget(int target_GO_ID)
    {
        // TODO add debouncer
        hentaiSexCoordinator.sendSexyTimeRequest(target_GO_ID);
    }

    // only attackers get this callback
    private void onSexyTimeJoinResponse(object message)
    {
        //TODO decide what the AI likes doing here
        // like branch based on the likes
        // I am attacker talking to victim in this whole method
        SexyTimeJoinResponse response = (SexyTimeJoinResponse)message;
        if (response.isLead)
        {
            HMove move = HentaiMoveSystem.INSTANCE.GetRandomHMoveForParts(hentaiSexCoordinator.getAvailableParts(), response.availableParts);
            if (move == null)
            {
                //hentaiSexyTimeEventMessenger.sendEvent_freeVictim(response.senderGO_ID);
                //return;
            }
            else
            {
                move.sexLocationPosition = response.sexLocation;
                move.sexLocationRotation = response.sexRotation;
                Debug.Log(gameObject.name + "is sending move " + move.moveName);
                hentaiSexyTimeEventMessenger.sendEvent_sendHMove(move, response.senderGO_ID);
            }
        }
    }
}
