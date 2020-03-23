using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCustomizeOption : MonoBehaviour
{
    [ValueDropdown("ItemTypeValue")]
    public int itemType;

    public bool useSpecificColor;
    public Color specificColor;

    private static IEnumerable ItemTypeValue = new ValueDropdownList<int>()
{
    { "Hair", 1 },
    { "Clothes", 2 },
    { "Sex Clothes", 3 },
};

    void Start()
    {
        
    }

    void ChangeNpc()
    {

    }
}
