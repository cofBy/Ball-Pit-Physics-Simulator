using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Bounce")]
    [Range(0, 1)] public float restitution;
    [Range(0, 1)] public float friction;

    public float radius;

    [Header("Vars")]
    public Vector3 vel;
    public Vector3 acc;

    public int framesAsleep;
    public bool Asleep;

    public Vector3 prevPos;

    public int ID;

    [Header("Colors")]
    public Material mat;

    private void OnValidate()
    {
        transform.localScale = Vector3.one * radius * 2;
    }
    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        rend.material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
    public Vector2 inBounds(Wall wall)
    {
        Vector3 localOffset = transform.position - wall.transform.position;

        float alongRight = Vector3.Dot(localOffset, wall.transform.right);
        float alongForward = Vector3.Dot(localOffset, wall.transform.forward);

        return new Vector2(alongRight, alongForward);
    }
    public void bounceBalls(Ball ball1, Ball ball2)
    {
        Vector3 dir = (ball2.transform.position - ball1.transform.position).normalized;

        Vector3 localVelocity = ball1.vel - ball2.vel;

        float velNormal = Vector3.Dot(localVelocity, dir);

        if (velNormal > 0) return;

        Vector3 velNormalComponent = velNormal * dir;
        Vector3 velTangentComponent = localVelocity - velNormalComponent;

        float averageRes = (ball1.restitution + ball2.restitution) / 2;

        Vector3 force = (velNormalComponent / 2 * (1 + averageRes)) + (velTangentComponent / 2 * friction);

        ball1.vel -= force;
        ball2.vel += force;
    }
    public void bounce(Vector3 normal)
    {
        float VelInNormal = Vector3.Dot(vel, normal);
        
        Vector3 velNormalComponent = VelInNormal * normal;
        Vector3 velTangentComponent = vel - velNormalComponent;

        vel = (velNormalComponent * -restitution) + (velTangentComponent * Mathf.Abs(friction - 1));
    }
}
