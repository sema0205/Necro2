using UnityEngine;

public class ShotgunWeapon : WeaponBase
{
    [Header("Hitscan Cone")]
    public LayerMask enemyLayers;
    public float maxRange = 15f;
    [Min(1)] public int pelletCount = 8;
    public float coneAngle = 10f;

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public GameObject tracerPrefab;
    public Transform muzzlePoint;

    protected override void TryFire()
    {
        // Origin/direction from the camera so spread is aligned with where the player looks.
        var cam = Camera.main;
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 baseDirection = cam != null ? cam.transform.forward : transform.forward;
        int mask = enemyLayers.value == 0 ? Physics.DefaultRaycastLayers : enemyLayers.value;

        Vector3 muzzleWorldPos = muzzlePoint != null ? muzzlePoint.position : transform.position;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = ApplyConeSpread(baseDirection);
            if (RaycastSkippingSelf(new Ray(origin, dir), maxRange, mask, out RaycastHit hit))
            {
                HandlePelletHit(hit);
                SpawnHitEffect(hit.point, hit.normal);
                SpawnTracer(muzzleWorldPos, hit.point);
            }
            else
            {
                // Miss — still draw a tracer out to max range for visual feedback.
                SpawnTracer(muzzleWorldPos, origin + dir * maxRange);
            }
        }

        ApplyHeat(data.heatPerShot);
        SpawnMuzzleFlash();

        AddRecoilFromData();
        PlayFireSfx();
    }

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    private static bool RaycastSkippingSelf(Ray ray, float range, int mask, out RaycastHit firstValidHit)
    {
        int count = Physics.RaycastNonAlloc(ray, HitBuffer, range, mask, QueryTriggerInteraction.Ignore);
        if (count == 0) { firstValidHit = default; return false; }

        System.Array.Sort(HitBuffer, 0, count, RaycastComparer.Instance);

        for (int i = 0; i < count; i++)
        {
            var col = HitBuffer[i].collider;
            if (col == null) continue;
            if (col.transform.root.CompareTag("Player")) continue;
            firstValidHit = HitBuffer[i];
            return true;
        }
        firstValidHit = default;
        return false;
    }

    private class RaycastComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly RaycastComparer Instance = new();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    private void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (hitEffectPrefab == null) return;
        var fx = Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
        Destroy(fx, 1f);
    }

    private void SpawnTracer(Vector3 from, Vector3 to)
    {
        if (tracerPrefab == null) return;
        var tracer = Instantiate(tracerPrefab);
        var line = tracer.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }
        else
        {
            tracer.transform.position = from;
            tracer.transform.rotation = Quaternion.LookRotation(to - from);
        }
        Destroy(tracer, 0.08f);
    }

    protected override void ExecuteSkill()
    {
        // Reserved for future "slug" alt-fire — single high-damage pellet
    }

    private void HandlePelletHit(RaycastHit hit)
    {
        var enemy = hit.collider.GetComponentInParent<EnemyBase>();
        if (enemy == null) return;

        float damage = data.baseDamage;
        if (!isOverheated && currentHeat >= data.optimalZoneStart && currentHeat <= data.optimalZoneEnd)
            damage *= data.optimalHeatMultiplier;

        enemy.TakeDamage(damage);
        Debug.Log($"[Shotgun] pellet hit {enemy.name} for {damage}");
    }

    private Vector3 ApplyConeSpread(Vector3 forward)
    {
        float yaw = Random.Range(-coneAngle, coneAngle);
        float pitch = Random.Range(-coneAngle, coneAngle);
        return Quaternion.Euler(pitch, yaw, 0f) * forward;
    }

    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null) return;
        var flash = Instantiate(muzzleFlashPrefab, transform.position, transform.rotation);
        Destroy(flash, 0.1f);
    }

    private void ApplyHeat(float amount)
    {
        currentHeat = Mathf.Clamp(currentHeat + amount, 0f, data.overheatThreshold);
        if (!isOverheated && currentHeat >= data.overheatThreshold)
            isOverheated = true;
    }
}
