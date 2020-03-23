using JacobGames.SuperInvoke;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HMove;

public class SexShadowClone : MonoBehaviour, FreeFlowGapListener
{

    private HentaiSexCoordinator hentaiSexCoordinator;
    private FreeFlowGapCloser freeFlowGapCloser;
    private HentaiMoveSystem hentaiMoveSystem;
    private GameObject victim;

    private int GO_ID;

    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        freeFlowGapCloser = GetComponent<FreeFlowGapCloser>();
        hentaiMoveSystem = HentaiMoveSystem.INSTANCE;

        //spawn in effeccts
        StartCoroutine(DoSpawnInEffect());

        // they got 5 seconds to reach their destination else disappear
        Invoke("Disappear", 5.0f);
        InvokeRepeating("MonitorVictim", 1.0f, 1.0f);
    }
    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    public void MoveThenSex()
    {
        if (victim == null)
        {
            Disappear();
            return;
        }
        freeFlowGapCloser.MoveToTargetForSex(victim, this);
    }

    private void MonitorVictim()
    {
        if (victim == null)
        {
            Debug.LogError("FOUND THE ISSUE LOL");
        }
    }

    public void SetTarget(GameObject victim)
    {
        this.victim = victim;
        WickedObserver.RemoveListener(disposables);
        disposables.Clear();
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + victim.gameObject.GetInstanceID(), (obj) => {
            Disappear();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            CancelInvoke();
        }));
    }

    private void Disappear()
    {
        if (this != null && gameObject!=null)
        {
            Destroy(gameObject,0.1f);
        }
    }
    private IEnumerator DoSpawnInEffect()
    {
        yield return new WaitForEndOfFrame();
        SpecialFxRequestBuilder.newBuilder("PinkCloud")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();
    }

    public void onReachedDestination()
    {
        CancelInvoke();
        HentaiSexCoordinator victimCoordinator = victim.gameObject.GetComponent<HentaiSexCoordinator>();
        HMove move = hentaiMoveSystem.GetRandomHMoveForParts(hentaiSexCoordinator.getAvailableParts(), victimCoordinator.getAvailableParts());
        if (move == null)
        {
            Disappear();
            return;
        }

        if (move.attackers != null && move.attackers.Length > 0)
        {
            // agressors
            move.attackers[0].gameObject = gameObject;
            for (int i = 1; i < move.attackers.Length; i++)
            {
                Attacker attacker = move.attackers[i];
                GameObject sexExtra;
                if (attacker.usingPart == "penis")
                {
                    sexExtra = SpecialFxRequestBuilder.newBuilder("FutaSexExtra")
                .setOwner(transform, false)
                .build().Play();
                }
                else
                {
                    sexExtra = SpecialFxRequestBuilder.newBuilder("FemaleSexExtra")
                .setOwner(transform, false)
                .build().Play();
                }
                SexShadowClone slaveShadow = sexExtra.GetComponent<SexShadowClone>();

                // todo spawn in matching outfit
                slaveShadow.SetTarget(victim);
                move.attackers[i].gameObject = sexExtra;
            }
        }
        move.victim.gameObject = victim.gameObject;
        StartCoroutine(StartMoveDelayed(move));

        IJobRepeat job = SuperInvoke.RunRepeat(0, 0.01f, 100, () =>
        {
            var lookPos = victim.gameObject.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        });
        _ = SuperInvoke.Run(0.1f, () =>
        {
            if (job != null)
            {
                job.Kill();
            }
        });
    }

    private IEnumerator StartMoveDelayed(HMove move)
    {
        yield return new WaitForEndOfFrame();
        victim.GetComponent<HentaiSexCoordinator>().StartNewSexMove(gameObject, move, false);
    }

    public void onReachedDestinationFail()
    {
        Disappear();
    }
}
