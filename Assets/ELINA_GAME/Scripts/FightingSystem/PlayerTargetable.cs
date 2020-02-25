using Invector.vCharacterController;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy --> Player actions
/// </summary>
public class PlayerTargetable : MonoBehaviour
{
    public const int HIT_RESULT_NORMAL = 0;
    public const int HIT_RESULT_STUN = 2;

    [BoxGroup("Defense Stats")]
    public int hitsForStun = 5;
    [BoxGroup("Defense Stats")]
    public int orgasmTimesForDefeat = 3;


    private int currentHits;
    public const float DIZZY_STUN_TIME = 5;
    public const float MIN_SEX_TIME = 5.0f;

    [HideInInspector]
    public bool canBeAttacked;
    [HideInInspector]
    public bool canBeSexed; // meaning that cna the player be forced to switch to a new move.
    [HideInInspector]
    public bool canBeCarried;
    private List<System.Guid> disposables = new List<System.Guid>();
    private vShooterMeleeInput shooterMeleeInput;
    private GlideController glideController;


    private void Awake()
    {
        glideController = GetComponent<GlideController>();
        shooterMeleeInput = GetComponent<vShooterMeleeInput>();
        canBeAttacked = true;
        canBeSexed = true;
        canBeCarried = false;
        int GO_ID = gameObject.GetInstanceID();

        // while sexing the player is immune
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            HMove currentMove = new HMove((HMove)obj); // update loop for hentai moves
            canBeSexed = false;
            canBeAttacked = false;
            shooterMeleeInput.jumpInput.useInput = false;
            glideController.enabled = false;
        }));

        WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused) =>
        {
            canBeSexed = true;
            canBeAttacked = true;
            Invoke("AllowJump", 0.1f);
            glideController.enabled = true;
        });
    }

    private void AllowJump()
    {
        shooterMeleeInput.jumpInput.useInput = true;
    }
    public int TakePhysicalHit()
    {
        if (currentHits + 1 == hitsForStun)
        {
            currentHits = 0;
            return HIT_RESULT_STUN;
        }
        currentHits++;
        return HIT_RESULT_NORMAL;
    }
    

}
