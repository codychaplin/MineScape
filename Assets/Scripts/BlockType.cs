using UnityEngine;

[System.Serializable]
public class BlockType
{
    public string Name;
    public bool isSolid;

    [Header("Texture Values")]
    public int backTexture;
    public int frontTexture;
    public int topTexture;
    public int bottomTexture;
    public int leftTexture;
    public int rightTexture;

    public int GetTextureId (int faceIndex)
    {
        switch (faceIndex)
        {
            case 0: return backTexture;
            case 1: return frontTexture;
            case 2: return topTexture;
            case 3: return bottomTexture;
            case 4: return leftTexture;
            case 5: return rightTexture;
            default:
                Debug.LogError("Error getting texture Id");
                return 0;
        }
    }
}