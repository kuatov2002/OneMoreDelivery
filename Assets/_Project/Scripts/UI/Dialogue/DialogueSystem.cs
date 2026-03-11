using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using Sirenix.OdinInspector;

public class DialogueSystem : MonoBehaviour
{
    [TitleGroup("UI References")]
    
    [Required("Speaker name text is required")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    
    [Required("Dialogue text is required")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    
    [Required("Dialogue panel is required")]
    [SerializeField] private GameObject dialoguePanel;

    [TitleGroup("Settings")]
    [InfoBox("When enabled, sets Time.timeScale to 0 during dialogue")]
    [SerializeField] private bool pauseGameDuringDialogue = true;
    
    [SerializeField] private bool playOnStart = false;
    
    [Range(0.01f, 0.1f)]
    [SerializeField] private float textSpeed = 0.05f;

    [TitleGroup("Dialogue Data")]
    [InlineEditor(InlineEditorModes.GUIOnly)]
    [SerializeField] private Dialogue dialogue;

    [TitleGroup("Events")]
    [InfoBox("Invoked when dialogue sequence completes")]
    public UnityEvent onDialogueComplete;

    [TitleGroup("Debug", "Runtime Information", TitleAlignments.Split)]
    [ShowInInspector, ReadOnly, ProgressBar(0, 1, ColorGetter = "GetProgressBarColor")]
    private float DialogueProgress => dialogue != null && dialogue.lines.Length > 0 
        ? (float)currentLineIndex / dialogue.lines.Length 
        : 0f;

    [ShowInInspector, ReadOnly]
    private int CurrentLine => currentLineIndex + 1;

    [ShowInInspector, ReadOnly]
    private int TotalLines => dialogue != null ? dialogue.lines.Length : 0;

    [ShowInInspector, ReadOnly]
    private bool IsActive => dialogueActive;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;
    private float previousTimeScale;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (playOnStart)
            StartDialogue();
    }

    private void Update()
    {
        if (!dialogueActive) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopTyping();
                DisplayCompleteLine();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    [TitleGroup("Controls")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    [EnableIf("@!dialogueActive && dialogue != null && dialogue.lines.Length > 0")]
    public void StartDialogue()
    {
        if (dialogue == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("No dialogue assigned or dialogue is empty.");
            return;
        }

        dialogueActive = true;
        currentLineIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (pauseGameDuringDialogue)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        ShowLine(currentLineIndex);
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    [EnableIf("dialogueActive")]
    [TitleGroup("Controls")]
    public void StopDialogue()
    {
        EndDialogue();
    }

    private void ShowLine(int index)
    {
        if (index >= dialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogue.lines[index];

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    private void DisplayCompleteLine()
    {
        dialogueText.text = dialogue.lines[currentLineIndex].text;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        ShowLine(currentLineIndex);
    }

    private void EndDialogue()
    {
        dialogueActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (pauseGameDuringDialogue)
            Time.timeScale = previousTimeScale;

        onDialogueComplete?.Invoke();
    }

    public void SetDialogue(Dialogue newDialogue)
    {
        dialogue = newDialogue;
    }

    public bool IsDialogueActive()
    {
        return dialogueActive;
    }

    private Color GetProgressBarColor()
    {
        return Color.Lerp(Color.red, Color.green, DialogueProgress);
    }

    [OnInspectorInit]
    private void ValidateSetup()
    {
        if (dialogue != null && dialogue.lines.Length > 0)
        {
            foreach (var line in dialogue.lines)
            {
                if (string.IsNullOrEmpty(line.text))
                    Debug.LogWarning("Dialogue contains empty text lines");
            }
        }
    }
}
