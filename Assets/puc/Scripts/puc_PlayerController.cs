using UnityEngine;

public class puc_PlayerController : MonoBehaviour
{
    public Boss boss;
    public float damagePerMeter = 1f;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;
    public bool isCutscene = false;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isCutscene)
        {
            lastPosition = transform.position;
            return;
        }

        float dx = transform.position.x - lastPosition.x;
        float dz = transform.position.z - lastPosition.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);

        if (distance > 0.001f)
        {
            accumulatedDistance += distance;
            if (accumulatedDistance >= 1f)
            {
                if (boss != null)
                    boss.TakeDamage(damagePerMeter);

                accumulatedDistance -= 1f;
            }
        }
        lastPosition = transform.position;
    }

    public void StartCutscene()
    {
        isCutscene = true;
    }

    public void EndCutscene()
    {
        isCutscene = false;
        lastPosition = transform.position;
    }
}
