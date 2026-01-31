using UnityEngine;

public class RandomSprite : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;

    private void Start()
    {
        if (sprites.Length == 0) return;

        Sprite randomSprite = sprites[Random.Range(0, sprites.Length)];
        GetComponent<Renderer>().material.mainTexture = randomSprite.texture;
    }
}