using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Size")]
    public float size;

    public void OnValidate()
    {
        transform.localScale = Vector3.one * size / 10;
    }
}
