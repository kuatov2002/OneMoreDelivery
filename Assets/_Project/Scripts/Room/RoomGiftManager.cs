using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using UnityEngine;

/// <summary>
/// Manages stat gift selection after a room is cleared.
/// Subscribes to Room.OnRoomCleared, pauses the game,
/// presents 3 random gifts via BuffSelectionModule,
/// and applies the selected gift to the player's CharacterStats.
///
/// Place on the same GameObject as Room, or assign Room reference in inspector.
/// </summary>
public class RoomGiftManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Room to watch. If null, tries GetComponent<Room>() on this object.")]
    [SerializeField] private Room room;

    [Header("Gift Pool")]
    [Tooltip("All possible stat gifts. 3 random ones are shown after room clear.")]
    [SerializeField] private List<StatGiftChoice> giftPool;

    [Header("Settings")]
    [Tooltip("How many gift choices to present.")]
    [SerializeField] private int choiceCount = 3;

    private BuffSelectionService _selectionService;
    private int _giftCounter;

    private void Awake()
    {
        if (room == null)
            room = GetComponent<Room>();

        if (room == null)
        {
            Debug.LogError("[RoomGiftManager] No Room reference assigned or found.", this);
            enabled = false;
            return;
        }

        // Wrap StatGiftChoice list as IBuffChoice for the service.
        var choices = new List<IBuffChoice>(giftPool);
        _selectionService = new BuffSelectionService(choices);
    }

    private void OnEnable()
    {
        if (room != null)
            room.OnRoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        if (room != null)
            room.OnRoomCleared -= HandleRoomCleared;
    }

    private void HandleRoomCleared()
    {
        if (giftPool == null || giftPool.Count == 0)
        {
            Debug.LogWarning("[RoomGiftManager] Gift pool is empty, skipping selection.", this);
            return;
        }

        // Pause the game while player picks a gift.
        Time.timeScale = 0f;

        _selectionService.ShowRandomSelection(choiceCount, OnGiftSelected);
    }

    private void OnGiftSelected(IBuffChoice choice)
    {
        // Unpause the game.
        Time.timeScale = 1f;

        if (choice == null)
        {
            Debug.LogWarning("[RoomGiftManager] No gift was selected.");
            RoomSequencer.Instance?.OnRoomCleared();
            return;
        }

        if (choice is not StatGiftChoice gift)
        {
            Debug.LogError("[RoomGiftManager] Selected choice is not a StatGiftChoice.");
            RoomSequencer.Instance?.OnRoomCleared();
            return;
        }

        ApplyGift(gift);

        // Notify RoomSequencer now that the gift is applied and the game is unpaused.
        // This is intentionally deferred from Room.OnRoomCleared to avoid loading the
        // next scene before the player has finished selecting.
        RoomSequencer.Instance?.OnRoomCleared();
    }

    private void ApplyGift(StatGiftChoice gift)
    {
        // Find the player's CharacterStats via TopDownEngine's LevelManager.
        CharacterStats stats = FindPlayerStats();
        if (stats == null)
        {
            Debug.LogError("[RoomGiftManager] Could not find player CharacterStats.");
            return;
        }

        // Generate a unique modifier ID so gifts don't overwrite each other.
        _giftCounter++;
        string modifierID = $"gift_{gift.statType}_{_giftCounter}";
        var modifier = new StatModifier(modifierID, gift.value);

        stats.AddModifier(gift.statType, modifier);

        // Persist the modifier in RunState so CharacterStats can reapply it after a scene load.
        if (RunState.Instance != null && RunState.Instance.IsRunActive)
            RunState.Instance.AddRunModifier(gift.statType, modifier);

        Debug.Log($"[RoomGiftManager] Applied gift: {gift.displayName} " +
                  $"({gift.statType} +{gift.value}), ID={modifierID}");
    }

    private CharacterStats FindPlayerStats()
    {
        // Try LevelManager first (standard TopDownEngine approach).
        if (LevelManager.Instance != null
            && LevelManager.Instance.Players != null
            && LevelManager.Instance.Players.Count > 0)
        {
            Character player = LevelManager.Instance.Players[0];
            if (player != null)
                return player.FindAbility<CharacterStats>();
        }

        // Fallback: search by "Player" tag.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Character character = playerObj.GetComponentInParent<Character>();
            if (character != null)
                return character.FindAbility<CharacterStats>();
        }

        return null;
    }
}
