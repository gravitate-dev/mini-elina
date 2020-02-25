using UnityEngine;

[System.Serializable]
public class ColorableData
{
    public const int SHADER_TYPE_REALTOON = 1;
    public const int SHADER_TYPE_UNITYCHAN = 2;
    public const int SHADER_TYPE_POIYOMI = 3;
    public const int SHADER_TYPE_JORDAN = 4;
    public ColorableData(string gameObjectName, int matIndex, int shaderType, string matName, Color color, bool outline)
    {
        this.gameObjectName = gameObjectName;
        this.matIndex = matIndex;
        this.shaderType = shaderType;
        this.matName = matName;
        this.color = color;
        this.outline = outline;
    }

    public string gameObjectName;
    public int matIndex;
    public int shaderType;
    public string matName;
    public Color color;
    public bool outline;
}
