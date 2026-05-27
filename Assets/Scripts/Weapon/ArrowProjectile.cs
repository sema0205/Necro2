using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 20f;
    public float lifeSpan = 5f; // время жизни стрелы, чтобы не засорять память
    public float speed = 30f;

    [Header("FX")]
    public GameObject hitEffectPrefab;

    private Rigidbody rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifeSpan);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Fallback when the arrow collider isn't set to "Is Trigger".
        if (collision.collider != null)
            HandleHit(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal);
    }

    private void HandleHit(Collider other)
    {
        HandleHit(other, transform.position, -transform.forward);
    }

    private void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        if (hasHit || other == null) return;
        if (other.transform.root.CompareTag("Player")) return; // не лупим по стрелявшему

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"[Arrow] hit {enemy.name} for {damage}");
        }

        if (hitEffectPrefab != null)
        {
            var fx = Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(fx, 1.2f);
        }

        hasHit = true;
        Destroy(gameObject);
    }
}
