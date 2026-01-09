using UnityEngine;

[CreateAssetMenu(fileName = "Choice", menuName = "Game/Choice Data")]
public class GameCreatorChoice : ScriptableObject, IBuffChoice
{
    public string displayName;
    public string description;
    public Sprite icon;
    public Color backgroundColor = Color.white;
    
    public string GetName() => displayName;
    public string GetDescription() => description;
    public Sprite GetIcon() => icon;
    public Color GetBackgroundColor() => backgroundColor;
}