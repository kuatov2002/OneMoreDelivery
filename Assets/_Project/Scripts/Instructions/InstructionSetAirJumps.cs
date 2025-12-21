using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Characters;
using GameCreator.Runtime.Common;
using UnityEngine;

namespace GameCreator.Runtime.VisualScripting
{
    [Version(0, 1, 1)]

    [Title("Change Air Jumps")]
    [Description("Changes the Character's number of air jumps over time")]

    [Category("Characters/Properties/Change Air Jumps")]
    
    [Parameter("Air Jumps", "The target number of air jumps for the Character")]
    [Parameter("Duration", "How long it will take to perform the transition")]
    [Parameter("Easing", "The change rate of the parameter over time")]
    [Parameter("Wait to Complete", "Whether to wait until the transition is finished")]

    [Keywords("Jump", "Double", "Multi", "Air", "Hop")]
    [Image(typeof(IconBust), ColorTheme.Type.Yellow)]

    [Serializable]
    public class InstructionCharacterPropertyAirJumps : TInstructionCharacterProperty
    {
        // MEMBERS: -------------------------------------------------------------------------------
        
        [SerializeField] private ChangeInteger m_AirJumps = new ChangeInteger(1);
        [SerializeField] private Transition m_Transition = new Transition();
        
        // PROPERTIES: ----------------------------------------------------------------------------

        public override string Title => $"Air Jumps {this.m_Character} {this.m_AirJumps}";

        // RUN METHOD: ----------------------------------------------------------------------------
        
        protected override async Task Run(Args args)
        {
            Character character = this.m_Character.Get<Character>(args);
            if (character == null) return;

            int valueSource = character.Motion.AirJumps;
            int valueTarget = (int) this.m_AirJumps.Get(valueSource, args);

            ITweenInput tween = new TweenInput<float>(
                valueSource,
                valueTarget,
                this.m_Transition.Duration,
                (a, b, t) => character.Motion.AirJumps = Mathf.RoundToInt(Mathf.Lerp(a, b, t)),
                Tween.GetHash(typeof(Character), "property:air-jumps"),
                this.m_Transition.EasingType,
                this.m_Transition.Time
            );
            
            Tween.To(character.gameObject, tween);
            if (this.m_Transition.WaitToComplete) await this.Until(() => tween.IsFinished);
        }
    }
}