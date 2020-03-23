using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NPCDressUpItem : MonoBehaviour
{
    [ValueDropdown("CharTypeValue")]
    public int characterType;

        private static IEnumerable CharTypeValue = new ValueDropdownList<int>()
    {
        { "ElinaChan", 1 },
        { "ShadowMale", 2 }
    };

    [ValueDropdown("ItemTypeValue")]
    public int itemType;

    private static IEnumerable ItemTypeValue = new ValueDropdownList<int>()
    {
        { "Hair", 1 },
        { "Cloth", 2 },
        { "Lingerie", 3 }
    };

    public const int ITEM_HAIR = 1;
    public const int ITEM_CLOTH = 2;
    public const int ITEM_LINGERIE = 3;

    public Sprite icon;

    public List<GameObject> items;
    

    public void Clothe(CharacterDressUp characterDressUp)
    {
        switch (itemType)
        {
            case ITEM_HAIR:
                characterDressUp.ChangeHair(items);
                break;
            case ITEM_CLOTH:
                characterDressUp.ChangeClothes(items);
                break;
            case ITEM_LINGERIE:
                characterDressUp.ChangeLingerie(items);
                break;
            default:
                Debug.LogError("DIDNT SET CLOTH ITEM TYPE!");
                break;
        }

    }
}
