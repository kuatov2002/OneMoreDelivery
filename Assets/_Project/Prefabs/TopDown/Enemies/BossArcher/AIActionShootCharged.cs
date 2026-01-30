using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Shoot Charged")]
    public class AIActionShootCharged : AIAction
    {
        [Header("Settings")]
        [Tooltip("Time to charge the shot")]
        public float ChargeTime = 1f;
        
        [Tooltip("Cooldown between charged shots")]
        public float ShootCooldown = 3f;

        [Header("Binding")]
        public CharacterHandleWeapon HandleWeapon;
        
        protected float _lastShootTime;
        protected float _chargeStartTime;
        protected bool _isCharging;
        protected WeaponAim _weaponAim;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            
            Character character = GetComponentInParent<Character>();
            if (HandleWeapon == null)
                HandleWeapon = character?.FindAbility<CharacterHandleWeapon>();
                
            _lastShootTime = -ShootCooldown;
        }

        public override void PerformAction()
        {
            if (Time.time - _lastShootTime < ShootCooldown) return;
            if (_brain.Target == null) return;

            if (!_isCharging)
            {
                StartCharging();
            }
            else if (Time.time - _chargeStartTime >= ChargeTime)
            {
                Shoot();
            }
        }

        protected virtual void StartCharging()
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
            
            if (HandleWeapon?.CurrentWeapon != null)
            {
                _weaponAim = HandleWeapon.CurrentWeapon.GetComponent<WeaponAim>();
            }
        }

        protected virtual void Shoot()
        {
            if (HandleWeapon?.CurrentWeapon != null)
            {
                // Aim at target
                if (_weaponAim != null && _brain.Target != null)
                {
                    Vector3 direction = _brain.Target.position - transform.position;
                    _weaponAim.SetCurrentAim(direction);
                }

                HandleWeapon.ShootStart();
            }

            _isCharging = false;
            _lastShootTime = Time.time;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _isCharging = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _isCharging = false;
            HandleWeapon?.ForceStop();
        }
    }
}