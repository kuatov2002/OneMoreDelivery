using UnityEngine;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;

namespace YourNamespace
{
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Shoot 3D Bow")]
    public class AIActionShoot3DBow : AIActionShoot3D
    {
        [Header("Bow Settings")]
        [Tooltip("Time between shots for semi-auto weapons")]
        public float TimeBetweenShots = 1f;
        
        protected float _lastShootTime = 0f;

        /// <summary>
        /// Override shoot to allow continuous semi-auto shooting
        /// </summary>
        protected override void Shoot()
        {
            // Для SemiAuto режима - стреляем с интервалом
            if (TargetHandleWeaponAbility.CurrentWeapon != null)
            {
                if (TargetHandleWeaponAbility.CurrentWeapon.TriggerMode == Weapon.TriggerModes.SemiAuto)
                {
                    if (Time.time - _lastShootTime >= TimeBetweenShots)
                    {
                        TargetHandleWeaponAbility.ShootStart();
                        _lastShootTime = Time.time;
                    }
                }
                else
                {
                    // Для Auto режима - стандартное поведение
                    if (_numberOfShoots < 1)
                    {
                        _targetWeapon = TargetHandleWeaponAbility.CurrentWeapon;
                        TargetHandleWeaponAbility.ShootStart();
                        _numberOfShoots++;
                    }
                }
            }

            if ((_targetWeapon == null) || (TargetHandleWeaponAbility.CurrentWeapon != _targetWeapon))
            {
                OnEnterState();
            }
        }

        /// <summary>
        /// Reset shoot time on enter
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();
            _lastShootTime = 0f;
        }
    }
}