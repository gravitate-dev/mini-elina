using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class ColorableItem : MonoBehaviour
{
    public List<ColorableData> matColorData = new List<ColorableData>();
    public Renderer render;
    // todo make a on equip maybe?
    public void Awake()
    {
        if (GetComponentInParent<IAmElina>() != null)
        {
            if (GetComponentInParent<ClothingDummyMirror>() == null)
            {
                // this item is  worn by elina and not by sexdummy
                if (render == null)
                {
                    render = gameObject.GetComponent<Renderer>();
                }
                Init();
            }
        }
    }

    public void Init()
    {
        for (int i = 0; i < render.materials.Length;i++)
        {
            Material material = render.materials[i];
            string shaderName = material.shader.ToString();
            if (shaderName.StartsWith("RealToon"))
            {
                float outlined = material.GetFloat(Shader.PropertyToID("_N_F_O"));
                if (outlined > 0.5)
                {
                    matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_REALTOON, material.name, material.GetColor(Shader.PropertyToID("_MainColor")), true));
                }
                else
                {
                    matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_REALTOON, material.name, material.GetColor(Shader.PropertyToID("_MainColor")), false));
                }
            }
            else if (shaderName.StartsWith("UnityChan"))
            {
                matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_UNITYCHAN, material.name, material.GetColor(Shader.PropertyToID("_BaseColor")), false));
            } else if (shaderName.StartsWith("Custom/Anime"))
            {
                matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_JORDAN, material.name, material.GetColor(Shader.PropertyToID("_BotEmiss")), false));
            } else if (shaderName.StartsWith(".poiyomi")) {
                matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_POIYOMI, material.name, material.color, false));
            } else {
                Debug.LogError("Unrecoginized shader! " + material.shader.ToString());
                matColorData.Add(new ColorableData(gameObject.name, i, ColorableData.SHADER_TYPE_UNITYCHAN, material.name, material.color, false));
            }
        }
    }

    public void ApplyColorChange(ColorableData colorableData)
    {
        if (colorableData.matIndex >= render.materials.Length)
        {
            Debug.LogError("Outside of index! Colorable data");
            return;
        }
        Material material = render.materials[colorableData.matIndex];
        if (colorableData.shaderType == ColorableData.SHADER_TYPE_REALTOON)
        {
            material.SetColor(Shader.PropertyToID("_MainColor"), colorableData.color);
            if (colorableData.outline)
            {
                material.SetFloat(Shader.PropertyToID("_N_F_O"), 1.0f);
            }
            else
            {
                material.SetFloat(Shader.PropertyToID("_N_F_O"), 0f);
            }
        }
        else if (colorableData.shaderType == ColorableData.SHADER_TYPE_UNITYCHAN)
        {
            material.SetColor(Shader.PropertyToID("_BaseColor"), colorableData.color);
            material.SetColor(Shader.PropertyToID("_1st_ShadeColor"), colorableData.color);
        }
        else if (colorableData.shaderType == ColorableData.SHADER_TYPE_POIYOMI)
        {
            material.color = colorableData.color;
        }
        else if (colorableData.shaderType == ColorableData.SHADER_TYPE_JORDAN)
        {
            material.SetColor(Shader.PropertyToID("_BotEmiss"), colorableData.color);
        } else
        {
            material.color = colorableData.color;
        }
    }
}

