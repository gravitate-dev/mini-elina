using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCustomizeController : MonoBehaviour
{
    public static string EVENT_STOP_NPC_COSMETICS = "StopNPCCosmetics";
    public static string EVENT_START_NPC_COSMETICS = "StartNPCCosmetics";
    private List<System.Guid> disposables = new List<System.Guid>();
    private List<GameObject> dressupClothingIcons = new List<GameObject>();

    public GameObject npcItemViewPrefab;

    public GameObject hairContainer;
    public GameObject clotheContainer;

    public List<GameObject> allNpcHairs = new List<GameObject>();
    public List<DualClothes> allNpcClothSets = new List<DualClothes>();
    
    [System.Serializable]
    public class DualClothes
    {
        public GameObject normal;
        public GameObject lingerie;
    }
    private void Awake()
    {
        gameObject.SetActive(false);
        LoadNPCHairs();
        LoadNPCClothes();

        disposables.Add(WickedObserver.AddListener(EVENT_START_NPC_COSMETICS, (characterDressUp) =>
        {
            if (characterDressUp == null)
            {
                Debug.LogError("Character dress up missing!");
                return;
            }
            Show();
            StartDressing((CharacterDressUp)characterDressUp);
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + IAmElina.ELINA.GetInstanceID(), (characterDressUp) =>
        {
            Hide();
        }));
        
    }
    // Update is called once per frame
    private void StartDressing(CharacterDressUp characterDressUp)
    {
        
        HideIcons();
        ShowIcons(characterDressUp);
    }

    private void HideIcons()
    {
        foreach (GameObject go in dressupClothingIcons)
        {
            go.SetActive(false);
        }
    }
    private void ShowIcons(CharacterDressUp characterDressUp)
    {
        foreach (GameObject go in dressupClothingIcons)
        {
            //NPCO_ItemViewController itemController = go.GetComponent<NPCO_ItemViewController>();
            /*bool match = itemController.npcDressUpItems[0].characterType == characterDressUp.characterType;
            if (match)
            {
                go.SetActive(true);
                itemController.SetTarget(characterDressUp);
            }*/
        }
    }

    private void LoadNPCHairs()
    {
        foreach( GameObject icon in allNpcHairs)
        {
            GameObject newGo = Instantiate(npcItemViewPrefab, hairContainer.transform);
            /*NPCO_ItemViewController controller = newGo.GetComponent<NPCO_ItemViewController>();
            if (icon == null || icon.GetComponent<NPCDressUpItem>() == null)
            {
                Debug.LogError("FATAL: missing npcdressupitem on " + gameObject.name);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            controller.SetItem(icon.GetComponent<NPCDressUpItem>());
            dressupClothingIcons.Add(newGo);*/
        }
    }
    private void LoadNPCClothes()
    {
        foreach (DualClothes dualClothes in allNpcClothSets)
        {
            GameObject newGo = Instantiate(npcItemViewPrefab, clotheContainer.transform);
            /*NPCO_ItemViewController controller = newGo.GetComponent<NPCO_ItemViewController>();
            List<NPCDressUpItem> both = new List<NPCDressUpItem>();
            if (dualClothes.normal != null)
            {
                both.Add(dualClothes.normal.GetComponent<NPCDressUpItem>());
            }
            if (dualClothes.lingerie != null)
            {
                both.Add(dualClothes.lingerie.GetComponent<NPCDressUpItem>());
            }
            controller.SetItem(both);
            dressupClothingIcons.Add(newGo);*/
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        WickedObserver.SendMessage(EVENT_STOP_NPC_COSMETICS);
        gameObject.SetActive(false);
    }

}
