using MoreMountains.Tools;
using UnityEngine;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that picks random next state from list
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Random State")]
    public class AIDecisionRandomState : AIDecision
    {
        [Tooltip("Possible next states")]
        public string[] PossibleStates;
        
        [Tooltip("Weights for each state (higher = more likely)")]
        public float[] StateWeights;

        protected string _chosenState = "";

        public override bool Decide()
        {
            ChooseRandomState();

            // Перезаписываем TrueState в транзиции на лету
            // Нет — это грязно. Вместо этого просто возвращаем true,
            // и пусть TrueState в редакторе указывает на нужный стейт.
            // Но тогда нет рандома...
    
            // Правильный подход: сам делаем переход отсюда
            if (!string.IsNullOrEmpty(_chosenState))
            {
                _brain.TransitionToState(_chosenState);
            }
            return false; // false чтобы EvaluateTransitions не делал второй переход через TrueState
        }

        protected virtual void ChooseRandomState()
        {
            if (PossibleStates.Length == 0) return;

            if (StateWeights == null || StateWeights.Length != PossibleStates.Length)
            {
                // Equal probability
                _chosenState = PossibleStates[Random.Range(0, PossibleStates.Length)];
            }
            else
            {
                // Weighted random
                float totalWeight = 0f;
                foreach (float weight in StateWeights)
                {
                    totalWeight += weight;
                }

                float randomValue = Random.Range(0f, totalWeight);
                float currentWeight = 0f;

                for (int i = 0; i < PossibleStates.Length; i++)
                {
                    currentWeight += StateWeights[i];
                    if (randomValue <= currentWeight)
                    {
                        _chosenState = PossibleStates[i];
                        return;
                    }
                }

                _chosenState = PossibleStates[0]; // Fallback
            }
        }

        public string GetChosenState()
        {
            return _chosenState;
        }
    }
}