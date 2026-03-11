using System.Collections;
using MoreMountains.Tools;
using Unity.Cinemachine;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Laughs maniacally and taunts player (adds atmosphere)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Maniacal Laugh")]
    public class AIActionManiacalLaugh : AIAction
    {
        [Header("Audio")]
        [Tooltip("Laugh sound effects")]
        public AudioClip[] LaughSounds;
        
        [Tooltip("Taunt text to display")]
        public string[] Taunts = new string[]
        {
            "Let's play a game!",
            "Can you tell which is real?",
            "The rules have changed!",
            "Madness is the only truth!"
        };

        [Header("Camera")]
        [Tooltip("Cinemachine виртуальная камера для эффекта поворота")]
        public CinemachineCamera CameraTarget;

        [Tooltip("Длительность поворота до 180")]
        public float RotateToDuration = 0.4f;

        [Tooltip("Длительность возврата обратно к 0")]
        public float RotateBackDuration = 0.4f;

        protected AudioSource _audioSource;
        protected bool _hasLaughed = false;
        protected Coroutine _cameraCoroutine;
        protected Vector3 _initialCameraRotation;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            _audioSource = _brain.Owner.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = _brain.Owner.gameObject.AddComponent<AudioSource>();
            }
        }

        public override void PerformAction()
        {
            if (_hasLaughed) return;

            Laugh();
            _hasLaughed = true;
        }

        protected virtual void Laugh()
        {
            if (LaughSounds.Length > 0 && _audioSource != null)
            {
                AudioClip laugh = LaughSounds[Random.Range(0, LaughSounds.Length)];
                _audioSource.PlayOneShot(laugh);
            }

            if (Taunts.Length > 0)
            {
                string taunt = Taunts[Random.Range(0, Taunts.Length)];
                Debug.Log($"[JESTER]: {taunt}");
            }

            if (CameraTarget != null)
            {
                if (_cameraCoroutine != null)
                {
                    StopCoroutine(_cameraCoroutine);
                    RestoreCamera();
                }
                _initialCameraRotation = CameraTarget.transform.eulerAngles;
                _cameraCoroutine = StartCoroutine(RotateCameraSequence());
            }
        }

        protected virtual IEnumerator RotateCameraSequence()
        {
            float targetZ = _initialCameraRotation.z + 180f;

            // Поворот до 180
            float elapsed = 0f;
            while (elapsed < RotateToDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / RotateToDuration));
                float currentZ = Mathf.Lerp(_initialCameraRotation.z, targetZ, t);
                CameraTarget.transform.eulerAngles = new Vector3(
                    _initialCameraRotation.x,
                    _initialCameraRotation.y,
                    currentZ
                );
                yield return null;
            }

            // Точно 180, без погрешности из-за deltaTime
            CameraTarget.transform.eulerAngles = new Vector3(
                _initialCameraRotation.x,
                _initialCameraRotation.y,
                targetZ
            );

            // Возврат к 0
            elapsed = 0f;
            while (elapsed < RotateBackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / RotateBackDuration));
                float currentZ = Mathf.Lerp(targetZ, _initialCameraRotation.z, t);
                CameraTarget.transform.eulerAngles = new Vector3(
                    _initialCameraRotation.x,
                    _initialCameraRotation.y,
                    currentZ
                );
                yield return null;
            }

            RestoreCamera();
            _cameraCoroutine = null;
        }

        protected virtual void RestoreCamera()
        {
            if (CameraTarget != null)
            {
                CameraTarget.transform.eulerAngles = _initialCameraRotation;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasLaughed = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            if (_cameraCoroutine != null)
            {
                StopCoroutine(_cameraCoroutine);
                _cameraCoroutine = null;
                RestoreCamera();
            }
        }
    }
}