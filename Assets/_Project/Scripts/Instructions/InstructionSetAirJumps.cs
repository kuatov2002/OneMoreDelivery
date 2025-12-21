using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using GameCreator.Runtime.Characters;
using UnityEngine;

[Title("Set Air Jumps")]
[Category("Characters/Properties")]
[Description("Changes the number of air jumps a character can perform")]
[Serializable]
public class InstructionSetAirJumps : Instruction
{
    [SerializeField] 
    private PropertyGetGameObject m_Character = GetGameObjectPlayer.Create();
    
    [SerializeField] 
    private PropertyGetInteger m_AirJumps = new PropertyGetInteger(1);

    protected override Task Run(Args args)
    {
        Character character = m_Character.Get(args)?.Get<Character>();
        if (character == null) return DefaultResult;
        
        int airJumps = (int)m_AirJumps.Get(args);
        character.Motion.AirJumps = airJumps;
        
        return DefaultResult;
    }
}