using System.Collections.Generic;
using UnityEngine;

public class ColorableItem : MonoBehaviour
{
    public List<Renderer> renderers = new List<Renderer>();
    public bool DONT_SPLIT_LIGHTNESS_ACROSS_TEXTURES;
    public ColorableItem mirrorChild;
    [HideInInspector]
    public List<ColorableData> matColorData = new List<ColorableData>();
    private List<System.Guid> disposables = new List<System.Guid>();
    private bool hasUnsavedChanges = false;

    public const string ES_COLORS_PLAYER = "player-colors.es3";
    public const string ES_COLORS_ALLY = "ally1-colors.es3";
    public void Awake()
    {
        if (renderers.Count == 0)
        {
            if (GetComponent<Renderer>())
            {
                renderers.Add(GetComponent<Renderer>());
            }
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Renderer>())
                {
                    renderers.Add(child.GetComponent<Renderer>());
                }
            }
        }
        if (GetComponentInParent<IAmElina>() != null)
        {
            disposables.Add(WickedObserver.AddListener("OnInventoryClose", (unused) =>
            {
                SavePlayerColors();
            }));
            Init();
            LoadPlayerSavedColors();
            hasUnsavedChanges = false;
        }
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
        if (GetComponentInParent<IAmElina>() != null)
        {
            SavePlayerColors();
        }
    }

    public void Init()
    {
        for (int renderIdx = 0; renderIdx<renderers.Count; renderIdx++)
        {
            Renderer render = renderers[renderIdx];
            for (int matIndex = 0; matIndex < render.materials.Length; matIndex++)
            {
                Material material = render.materials[matIndex];
                string shaderName = material.shader.ToString();
                if (shaderName.StartsWith("RealToon"))
                {
                    Color primary = material.GetColor(Shader.PropertyToID("_MainColor"));
                    matColorData.Add(new ColorableData(gameObject.name, renderIdx, matIndex, ColorableData.SHADER_TYPE_REALTOON, material.name, primary));
                }
                else if (shaderName.StartsWith("UnityChan"))
                {
                    Color primary = material.GetColor(Shader.PropertyToID("_BaseColor"));
                    matColorData.Add(new ColorableData(gameObject.name, renderIdx, matIndex, ColorableData.SHADER_TYPE_UNITYCHAN, material.name, primary));
                }
                else if (shaderName.StartsWith("Custom/Anime"))
                {
                    matColorData.Add(new ColorableData(gameObject.name, renderIdx, matIndex, ColorableData.SHADER_TYPE_JORDAN, material.name, material.GetColor(Shader.PropertyToID("_BotEmiss"))));
                }
                else if (shaderName.StartsWith(".poiyomi"))
                {
                    matColorData.Add(new ColorableData(gameObject.name, renderIdx, matIndex, ColorableData.SHADER_TYPE_POIYOMI, material.name, material.color));
                }
                else
                {
                    Debug.LogError("Unrecoginized shader! " + material.shader.ToString() + " on " + gameObject.name);
                }
            }
        }
    }

    private string GetSaveBaseName()
    {
        string name = gameObject.name;
        int chop = name.IndexOf("(");
        if (chop == -1)
        {
            return name;
        }
        return name.Substring(0, chop);
    }
    private void SavePlayerColors()
    {
        if (mirrorChild == null)
        {
            // only save when we are mirroring!
            return;
        }
        if (!hasUnsavedChanges)
        {
            return;
        }
        hasUnsavedChanges = false;
        foreach ( ColorableData data in matColorData)
        {
            string key = "player_" + GetSaveBaseName() + data.rendererIndex + "_" + data.matIndex + "_" + data.matName;
            //ES3.Save<Color>(key, data.primaryColor, GameOptionsController.PLAYER_COLORABLE_ITEMS);
        }
    }
    private void LoadPlayerSavedColors()
    {
        foreach (ColorableData data in matColorData)
        {
            string key = "player_" + GetSaveBaseName() + data.rendererIndex + "_" + data.matIndex + "_" + data.matName;
            Color temp = Color.white;// ES3.Load<Color>(key, GameOptionsController.PLAYER_COLORABLE_ITEMS, Color.white);
            if (temp == null)
            {
                temp = Color.white;
            }
            data.primaryColor = temp;
            ApplyColorChange(data);
        }
    }

    private void ColorMaterial(Material material, Color color)
    {
        hasUnsavedChanges = true;
        string shaderName = material.shader.ToString();
        if (shaderName.StartsWith("RealToon"))
        {
            material.SetColor(Shader.PropertyToID("_MainColor"), color);
        }
        else if (shaderName.StartsWith("UnityChan"))
        {
            if (DONT_SPLIT_LIGHTNESS_ACROSS_TEXTURES)
            {
                material.SetColor(Shader.PropertyToID("_BaseColor"), color);
                material.SetColor(Shader.PropertyToID("_1st_ShadeColor"), color);
                material.SetColor(Shader.PropertyToID("_2nd_ShadeColor"), color);
                return;
            }
            float current_H = 0;
            float current_S = 0;
            float current_V = 0;


            Color.RGBToHSV(color, out current_H, out current_S, out current_V);

            Color dimmer = Color.HSVToRGB(current_H, current_S, Mathf.Max(0, current_V - 0.1f));
            Color dimmest = Color.HSVToRGB(current_H, current_S, Mathf.Max(0, current_V - 0.4f));
            material.SetColor(Shader.PropertyToID("_BaseColor"), color);
            material.SetColor(Shader.PropertyToID("_1st_ShadeColor"), dimmer);
            material.SetColor(Shader.PropertyToID("_2nd_ShadeColor"), dimmest);
        }
        else if (shaderName.StartsWith(".poiyomi"))
        {
            material.color = color;
        }
        else
        {
            material.color = color;
        }
    }

    public void ApplyColorChange(ColorableData colorableData)
    {
        if (mirrorChild != null && mirrorChild.gameObject != null)
        {
            mirrorChild.ApplyColorChange(colorableData);
        }
        if (colorableData.rendererIndex >= renderers.Count)
        {
            Debug.LogError("Outside of Renderer index!");
            return;
        }
        if (colorableData.matIndex >= renderers[colorableData.rendererIndex].materials.Length)
        {
            Debug.LogError("Outside of Mat index!");
            return;
        }
        Material material = renderers[colorableData.rendererIndex].materials[colorableData.matIndex];
        ColorMaterial(material, colorableData.primaryColor);
    }

    public void ApplyColorChangeGlobally(Color color)
    {
        if (mirrorChild != null && mirrorChild.gameObject != null)
        {
            mirrorChild.ApplyColorChangeGlobally(color);
        }
        foreach (Renderer render in renderers)
        {
            for (int i = 0; i < render.materials.Length; i++)
            {
                Material material = render.materials[i];
                ColorMaterial(material, color);
            }
        }
    }

    public void SetMirrorChild(ColorableItem mirrorChild)
    {
        this.mirrorChild = mirrorChild;
        foreach (ColorableData colorData in matColorData)
        {
            mirrorChild.ApplyColorChange(colorData);
        }
    }
}

