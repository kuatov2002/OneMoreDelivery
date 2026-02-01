using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75, ObjectFieldAlignment.Left)]
    [HideLabel]
    public Sprite characterIcon;

    [VerticalGroup("Split/Right")]
    [LabelWidth(100)]
    public string speakerName;

    [VerticalGroup("Split/Right")]
    [TextArea(3, 6)]
    [HideLabel]
    public string text;
}

[System.Serializable]
public class Dialogue
{
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "speakerName", DraggableItems = true)]
    public DialogueLine[] lines;
}