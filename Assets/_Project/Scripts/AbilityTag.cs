// AbilityTag.cs
// Каждый тег — это просто именованный ассет. Никакой логики.
// Создаёшь в Project: Create → TopDown Engine → Ability Tag
// Примеры: Evasion, Block, Attack_Melee, Attack_Ranged, Movement, Counter

using UnityEngine;

[CreateAssetMenu(menuName = "TopDown Engine/Ability Tag")]
public class AbilityTag : ScriptableObject { }