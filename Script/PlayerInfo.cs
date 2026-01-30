using UnityEngine;

[CreateAssetMenu(menuName = "Info/Player")]
public class PlayerInfo : ScriptableObject
{
    public PlayerId PlayerId;
    public string DisplayName;
    public Color Colour;
    public PawnClass PawnClass;

    public void Reset()
    {
        DisplayName = name;
        PawnClass = PawnClass.Invalid;
    }
}
