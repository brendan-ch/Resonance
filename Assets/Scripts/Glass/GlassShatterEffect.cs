using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Environment
{
    // Drives the full shatter sequence:
    //   1. Calls VoronoiGlassFracture to generate shard meshes at runtime
    //   2. Applies directional physics forces to each shard
    //   3. Attaches GlassShard to each for fade + cleanup
    //
    // Setup:
    //   - Place on the same GameObject as BreakableGlass.
    //   - Set Pane Size to match the glass pane's visible dimensions (local units).
    //   - Assign the glass material to Shard Material.
    //   - No child shard objects needed — everything is generated at runtime.
    public class GlassShatterEffect : MonoBehaviour
    {
        [Header("Pane Properties")]
        [Tooltip("Width (x) and height (y) of the glass pane in local units. Match your mesh size.")]
        [SerializeField] private Vector2 paneSize = new Vector2(1f, 1f);

        [Tooltip("Thickness of generated shards in local units.")]
        [SerializeField] [Range(0.05f, 0.1f)] private float shardThickness = 0.07f;

        [Tooltip("Number of Voronoi shards to generate. 14–20 recommended.")]
        [SerializeField] [Range(8, 30)] private int shardCount = 20;

        [Tooltip("Material assigned to all generated shards. Must be URP Transparent / glass material.")]
        [SerializeField] private Material shardMaterial;

        [Tooltip("Mass of each shard Rigidbody. Higher = less explosive reaction to forces.")]
        [SerializeField] private float shardMass = 10f;

        [Header("Force Settings")]
        [Tooltip("Base outward force applied to all shards via physics explosion.")]
        [SerializeField] private float baseExplosionForce = 5f;

        [Tooltip("Additional impulse applied in the bullet travel direction to bias the shatter.")]
        [SerializeField] private float directionalForceBias = 3f;

        [Tooltip("Upward force modifier passed to AddExplosionForce for a natural arc.")]
        [SerializeField] [Range(0f, 1f)] private float upwardBias = 0.1f;

        [Tooltip("Radius of the explosion force sphere.")]
        [SerializeField] private float explosionRadius = 0.3f;

        [Tooltip("Random spin torque applied per shard (min/max).")]
        [SerializeField] private float torqueMin = 2f;
        [SerializeField] private float torqueMax = 4f;

        [Header("Lifetime Settings")]
        [Tooltip("Seconds after shatter before shards begin fading.")]
        [SerializeField] private float fadeDelay = 6f;

        [Tooltip("Duration of the fade-out in seconds.")]
        [SerializeField] private float fadeDuration = 1.5f;

        // ------------------------------------------------------------------
        // Public API — called by BreakableGlass
        // ------------------------------------------------------------------

        public void Shatter(Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection)
        {
            if (shardMaterial == null)
            {
                Debug.LogWarning("[GlassShatterEffect] Shard Material is not assigned.", this);
                return;
            }

            // Convert world hit point to pane local space (xy plane only).
            Vector3 hitLocal   = transform.InverseTransformPoint(hitPoint);
            Vector2 hitLocal2D = new Vector2(hitLocal.x, hitLocal.y);

            List<GameObject> shards = VoronoiGlassFracture.Generate(
                transform,
                paneSize,
                shardThickness,
                shardCount,
                hitLocal2D,
                shardMaterial,
                shardMass
            );

            // Explosion origin slightly behind the hit surface.
            Vector3 explosionOrigin = hitPoint - hitDirection.normalized * 0.05f;

            Vector3 biasDir = hitDirection.sqrMagnitude > 0.01f
                ? hitDirection.normalized
                : -hitNormal.normalized;

            foreach (GameObject shard in shards)
            {
                Rigidbody rb = shard.GetComponent<Rigidbody>();

                rb.AddExplosionForce(baseExplosionForce, explosionOrigin, explosionRadius, upwardBias, ForceMode.Impulse);
                rb.AddForce(biasDir * directionalForceBias, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * Random.Range(torqueMin, torqueMax), ForceMode.Impulse);

                GlassShard glassShardComponent = shard.AddComponent<GlassShard>();
                glassShardComponent.Initialize(fadeDelay, fadeDuration);
            }
        }
    }
}