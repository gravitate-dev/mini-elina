using UnityEngine;

public class HentaiMoveDebugger : MonoBehaviour
{
    // TODO enforce buttons instead of key presses

    public int VICTIM_GO_ID;
    public int ATTACKER_GO_ID;
    public string HENTAI_MOVE_NAME = "Dv8 Doggy";

    private HentaiMoveSystem hentaiMoveSystem;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private int GO_ID;
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        VICTIM_GO_ID = GO_ID;
        hentaiMoveSystem = GameObject.FindObjectOfType<HentaiMoveSystem>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("v"))
        {
            sendHentaiMoveToTarget();
        }
        if (Input.GetKeyDown("b"))
        {
            stopHentaiMove();
        }
        if (Input.GetKeyDown("m"))
        {
            setVictimIdToMe();
        }
    }

    private void stopHentaiMove()
    {
        Debug.Log("CALLING STOP ALL SEX");
        hentaiSexCoordinator.stopAllSex();
    }
    private void sendHentaiMoveToTarget()
    {
        Debug.Log("Sending a move to (VICTIM,ATTACKER): (" + VICTIM_GO_ID + "," + ATTACKER_GO_ID + ")");
        //TODO set the sex location
        HMove testMove = hentaiMoveSystem.getHMoveByName(HENTAI_MOVE_NAME);
        if (testMove == null)
        {
            Debug.LogError("Unable to find move:" + HENTAI_MOVE_NAME);
        }
        testMove.victim.GO_ID = VICTIM_GO_ID;

        // not how it works now
        if (ATTACKER_GO_ID != 0 && testMove.attackers!=null && testMove.attackers.Length >0)
        {
            testMove.attackers[0].GO_ID = ATTACKER_GO_ID;
        }

        hentaiSexCoordinator.sendSTEM_sendHMove(testMove);
    }
    
    private void setVictimIdToMe()
    {
        stopHentaiMove();
        VICTIM_GO_ID = GO_ID;
    }
}
