using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that shoots elemental projectiles (fire, ice, lightning)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Shoot Elemental")]
    public class AIActionShootElemental : AIAction
    {
        public enum ElementType { Fire, Ice, Lightning }
        
        [Header("Settings")]
        [Tooltip("Which element to shoot")]
        public ElementType Element = ElementType.Fire;
        
        [Tooltip("Time between shots")]
        public float TimeBetweenShots = 2f;
        
        [Header("Binding")]
        public CharacterHandleWeapon HandleWeapon;

        protected float _lastShootTime;
        protected WeaponAim _weaponAim;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            
            Character character = GetComponentInParent<Character>();
            if (HandleWeapon == null)
                HandleWeapon = character?.FindAbility<CharacterHandleWeapon>();
                
            _lastShootTime = -TimeBetweenShots;
        }

        public override void PerformAction()
        {
            if (Time.time - _lastShootTime < TimeBetweenShots) return;
            if (_brain.Target == null) return;

            Shoot();
        }

        protected virtual void Shoot()
        {
            if (HandleWeapon?.CurrentWeapon != null)
            {
                // Aim at target
                if (_weaponAim == null)
                    _weaponAim = HandleWeapon.CurrentWeapon.GetComponent<WeaponAim>();
                    
                if (_weaponAim != null && _brain.Target != null)
                {
                    Vector3 direction = _brain.Target.position - transform.position;
                    _weaponAim.SetCurrentAim(direction);
                }

                HandleWeapon.ShootStart();
                _lastShootTime = Time.time;
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            HandleWeapon?.ForceStop();
        }
    }
}