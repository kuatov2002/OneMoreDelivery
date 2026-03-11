using System;
using System.Collections;
using UnityEngine;

public class DisableAfterTime : MonoBehaviour
{
    [SerializeField] private float time;
    [SerializeField] private bool startOnAwake;

    private void Awake()
    {
        if (startOnAwake) StartDisable();
    }

    public void StartDisable() => StartCoroutine(Disable());

    private IEnumerator Disable()
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }
}
