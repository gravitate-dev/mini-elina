using MagicaCloth;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDressUp : MonoBehaviour
{
    [ValueDropdown("CharTypeValue")]
    public int characterType;

    private static IEnumerable CharTypeValue = new ValueDropdownList<int>()
{
    { "ElinaChan", 1 },
    { "ShadowMale", 2 }
};


    public const int CHAR_TYPE_ELINA_CHAN = 1;
    public const int CHAR_TYPE_SHADOW_MALE = 2;
    [SerializeField]
    public List<GameObject> clothes = new List<GameObject>();
    public List<GameObject> sexClothes = new List<GameObject>();
    public GameObject hair;

    [InfoBox("Only need to set this in inspector for PlaygroundSexables")]
    public Transform head;

    private List<GameObject> instancedClothes = new List<GameObject>();

    private void Awake()
    {
        DressUp();
    }

    public void DressUp()
    {
        Animancer.HybridAnimancerComponent animancer = GetComponent<Animancer.HybridAnimancerComponent>();
        
        foreach (GameObject old in instancedClothes)
        {
            Destroy(old);
        }
        instancedClothes.Clear();
        foreach (GameObject go in clothes)
        {
            GameObject temp = Instantiate(go, transform);
            ClothBinder clothBinder = temp.GetComponent<ClothBinder>();
            clothBinder.showOnlyDuringSex = false;
            clothBinder.hideDuringSex = true;
            clothBinder.ForceUpdateVisibility();
            instancedClothes.Add(temp);
        }
        foreach ( GameObject go in sexClothes)
        {
            
            GameObject temp = Instantiate(go, transform);
            ClothBinder clothBinder = temp.GetComponent<ClothBinder>();
            clothBinder.hideDuringSex = false;
            clothBinder.showOnlyDuringSex = true;
            clothBinder.ForceUpdateVisibility();
            instancedClothes.Add(temp);
        }

        if (head == null)
        {
            head = animancer.Animator.GetBoneTransform(HumanBodyBones.Head);
        }
        if (head == null)
        {
            Debug.LogError("Please set head for gameObject " + gameObject.name);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        GameObject newHair = Instantiate(hair, head);
        instancedClothes.Add(newHair);
    }

    public void FullDressUp(List<GameObject> uniform, List<GameObject> uniformSex, List<GameObject> chosenHair)
    {
        clothes = uniform;
        sexClothes = uniformSex;
        if (chosenHair != null)
        {
            hair = chosenHair[0];
        }
        DressUp();
    }

    #region === Change Outfits ===
    public void ChangeHair(GameObject newHair)
    {
        hair = newHair;
        DressUp();
    }

    public void ChangeHair(List<GameObject> newHair)
    {
        hair = newHair[0];
        DressUp();
    }

    public void ChangeClothes(List<GameObject> newClothes)
    {
        clothes = newClothes;
        DressUp();
    }

    public void ChangeClothes(GameObject newCloth)
    {
        List<GameObject> temp = new List<GameObject>();
        temp.Add(newCloth);
        ChangeClothes(temp);
    }

    public void ChangeLingerie(List<GameObject> newLingeries)
    {
        sexClothes = newLingeries;
        DressUp();
    }

    public void ChangeLingerie(GameObject newLingerie)
    {
        List<GameObject> temp = new List<GameObject>();
        temp.Add(newLingerie);
        ChangeLingerie(temp);
    }

    #endregion

    public void AddTempItem(GameObject tempitem, float duration)
    {
        //todo
    }
    private bool HasSexClothes()
    {
        return sexClothes.Count != 0;
    }
}
