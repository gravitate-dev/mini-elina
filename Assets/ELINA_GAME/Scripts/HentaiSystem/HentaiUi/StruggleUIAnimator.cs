using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StruggleUIAnimator : MonoBehaviour
{
    public GameObject spaceUp;
    public GameObject spaceDown;

    void Start()
    {
        spaceUp.SetActive(false);
        spaceDown.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        gameObject.SetActive(true);
        spaceUp.SetActive(true);
        spaceDown.SetActive(false);
        InvokeRepeating("ToggleStruggleUI", 0, 0.1f);
    }

    private void OnDisable()
    {
        gameObject.SetActive(false);
        CancelInvoke();
        spaceUp.SetActive(false);
        spaceDown.SetActive(false);
    }

    private void ToggleStruggleUI()
    {
        bool showSpaceUp = !spaceUp.activeSelf;
        spaceUp.SetActive(showSpaceUp);
        spaceDown.SetActive(!showSpaceUp);
    }
    // Update is called once per frame
    void Update()
    {
        // no op
    }
}
