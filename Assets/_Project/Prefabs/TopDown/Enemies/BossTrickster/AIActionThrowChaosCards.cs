using System.Collections;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Throw Chaos Cards")]
    public class AIActionThrowChaosCards : AIAction
    {
        public enum CardEffect
        {
            Damage,
            Poison,
            Confusion,
            Slow,
            Heal
        }

        [Header("Card Settings")]
        [Tooltip("Card projectile prefab")]
        public GameObject CardPrefab;
        
        [Tooltip("Number of cards per throw")]
        public int CardsPerThrow = 3;
        
        [Tooltip("Spread angle")]
        public float SpreadAngle = 30f;
        
        [Tooltip("Time between throws")]
        public float ThrowCooldown = 2f;

        [Tooltip("Задержка между анимацией и спавном карт")]
        public float SpawnDelay = 0.3f;
        
        [Header("Possible Effects")]
        public CardEffect[] PossibleEffects = new CardEffect[]
        {
            CardEffect.Damage,
            CardEffect.Poison,
            CardEffect.Slow
        };

        protected CharacterHandleWeapon _handleWeapon;
        protected float _lastThrowTime;
        protected WeaponAim _weaponAim;
        public Animator _animator;
        protected WaitForSeconds _spawnDelayWFS;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            Character character = GetComponentInParent<Character>();
            _handleWeapon = character?.FindAbility<CharacterHandleWeapon>();
            _lastThrowTime = -ThrowCooldown;
            _spawnDelayWFS = new WaitForSeconds(SpawnDelay);
        }

        public override void PerformAction()
        {
            if (Time.time - _lastThrowTime < ThrowCooldown) return;
            if (_brain.Target == null) return;

            ThrowCards();
        }

        protected virtual void ThrowCards()
        {
            // Блокируем сразу, иначе пока корутина ждёт — будут плодиться новые
            _lastThrowTime = Time.time;
            StartCoroutine(ThrowCardsSequence());
        }

        protected virtual IEnumerator ThrowCardsSequence()
        {
            if (_handleWeapon?.CurrentWeapon == null) yield break;
            ProjectileWeapon weapon = _handleWeapon.CurrentWeapon as ProjectileWeapon;
            if (weapon == null) yield break;
            if (_weaponAim == null)
                _weaponAim = weapon.GetComponent<WeaponAim>();

            // Целимся и запускаем анимацию до задержки
            Vector3 baseDirection = (_brain.Target.position - transform.position).normalized;
            if (_weaponAim != null)
            {
                _weaponAim.SetCurrentAim(baseDirection);
            }
            _animator?.SetTrigger("Shoot");

            // Ждём пока анимация дойдёт до момента броска
            yield return _spawnDelayWFS;

            // Цель могла исчезнуть за время ожидания
            if (_brain.Target == null) yield break;

            // Пересчитываем направление и позицию — цель могла сместиться
            baseDirection = (_brain.Target.position - transform.position).normalized;
            weapon.DetermineSpawnPosition();

            float startAngle = -SpreadAngle / 2f;
            float angleStep = CardsPerThrow > 1 ? SpreadAngle / (CardsPerThrow - 1) : 0;

            for (int i = 0; i < CardsPerThrow; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;

                GameObject card = weapon.SpawnProjectile(
                    weapon.SpawnPosition,
                    i,
                    CardsPerThrow,
                    true
                );

                if (card != null)
                {
                    Projectile projectile = card.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        Quaternion newRotation = Quaternion.LookRotation(direction);
                        projectile.SetDirection(direction, newRotation, true);
                    }

                    CardEffect effect = PossibleEffects[Random.Range(0, PossibleEffects.Length)];
                    ChaosCard chaosCard = card.GetComponent<ChaosCard>();
                    if (chaosCard == null)
                    {
                        chaosCard = card.AddComponent<ChaosCard>();
                    }
                    chaosCard.Effect = effect;
                    SetCardVisual(card, effect);
                }
            }
        }

        protected virtual void SetCardVisual(GameObject card, CardEffect effect)
        {
            Renderer renderer = card.GetComponent<Renderer>();
            if (renderer != null)
            {
                switch (effect)
                {
                    case CardEffect.Damage:
                        renderer.material.color = Color.red;
                        break;
                    case CardEffect.Poison:
                        renderer.material.color = Color.green;
                        break;
                    case CardEffect.Slow:
                        renderer.material.color = Color.blue;
                        break;
                    case CardEffect.Confusion:
                        renderer.material.color = Color.magenta;
                        break;
                    case CardEffect.Heal:
                        renderer.material.color = Color.yellow;
                        break;
                }
            }
        }
    }
}