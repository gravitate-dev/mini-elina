using Invector.vCharacterController;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When the player is downed their recovery bar starts rising, when they click they get up / escape
/// Note when tied up the player can not escape until an additional delay
/// </summary>
public class HentaiRecovery_ : MonoBehaviour
{
    // Time until player can recover
    // Getting tied up adds 5 seconds
    [Required]
    public Image recoveryHealthSlider;

    [HideInEditorMode]
    public float timeTillRecovery = 0;
    [HideInEditorMode]
    public float recoveredHealth = 0;
    [HideInEditorMode]
    public float recoveryRate = 5;

	//Private
    private bool knockedOut;
    private float tiedRecoveryDelay = 5.0f;
    private float baseRecoveryDelay = 5.0f;
    private vCharacter characterHackForNow;
    private int GO_ID;
    private bool detained;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private List<System.Guid> disposables = new List<System.Guid>();

    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        characterHackForNow = GetComponent<vCharacter>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        disposables.Add(WickedObserver.AddListener("onStateTieUp:" + GO_ID, (obj)=> { timeTillRecovery += tiedRecoveryDelay; }));
        disposables.Add(WickedObserver.AddListener("onStateKnockedOut:" + GO_ID, onStateKnockedOut));
        disposables.Add(WickedObserver.AddListener("onStateRegainControl:" + GO_ID, onStateRegainControl));
        disposables.Add(WickedObserver.AddListener("OnPreventEscapeByTime:" + GO_ID, OnPreventEscapeByTime));
        if (recoveryHealthSlider == null)
        {
            // cheap stuff
            recoveryHealthSlider = GameObject.Find("recoveredhealth").GetComponent<Image>();
        }
        recoveryHealthSlider.fillAmount = 0;
        recoveryHealthSlider.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    void Update()
    {
		//Stop sex key only if not knocked out
        if (!knockedOut && Input.GetKeyUp(KeyCode.Space))
		{
			hentaiSexCoordinator.StopAllSexIfAny();
			return;
		}

        if (timeTillRecovery > 0)
        {
            if (!detained)
            {
                detained = true;
                WickedObserver.SendMessage("OnDisplayChibi", HentaiChibiGui.CHIBI_BONDAGE);
            }

			timeTillRecovery -= Time.deltaTime;
        }
		else
        {
            recoveredHealth = Mathf.Min(characterHackForNow.maxHealth, recoveredHealth + (Time.deltaTime* recoveryRate));
            recoveryHealthSlider.fillAmount = recoveredHealth;
            if (detained)
            {
                recoveryHealthSlider.gameObject.SetActive(true);
                detained = false;
                WickedObserver.SendMessage("OnDisplayChibi", HentaiChibiGui.CHIBI_ENJOYING);
            }
        }

        if (knockedOut && Input.GetKeyDown(KeyCode.Space))
        {
            if (timeTillRecovery < 0)
            {
                transform.position += transform.forward * 2.0f;
                doRecovery();
            }
			else
                Debug.Log("You aren't recovered yet!");
        }
        
    }

    private void doRecovery()
    {
        WickedObserver.SendMessage("OnShortCircuitAI", 1.0f);
        WickedObserver.SendMessage("onStateRegainControl:" + GO_ID);
        characterHackForNow.ChangeHealth((int)recoveredHealth);
        recoveredHealth = 0;
        recoveryHealthSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// Used by <see cref="HentaiSexCoordinator"/>
    /// Used indirectly by <see cref="HentaiLustManager"/>
    /// </summary>
    /// <param name="message"></param>
    private void OnPreventEscapeByTime(object message)
    {
        float duration = (float)message;
        timeTillRecovery += duration;
    }

    private void onStateKnockedOut(object message)
    {
        recoveryHealthSlider.gameObject.SetActive(true);
        recoveryHealthSlider.fillAmount = 0;
        timeTillRecovery = baseRecoveryDelay;
        knockedOut = true;
    }

    private void onStateRegainControl(object message)
    {
        knockedOut = false;
        WickedObserver.SendMessage("OnDisplayChibi", HentaiChibiGui.CHIBI_NORMAL);
    }

    /// <summary>
    /// <see cref="HentaiCancelMove"/> Elina needs to recover before she can even escape her own moves
    /// </summary>
    /// <returns></returns>
    public bool canRecover()
    {
        return !detained;
    }
}
