using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Resonance.Environment
{
    // Generates procedural glass shard meshes at runtime using a Voronoi partition.
    // Seeds are biased toward the hit point so cracks radiate from impact.
    // Each shard is extruded into a thin mesh and returned as a spawned GameObject.
    public static class VoronoiGlassFracture
    {
        // Resolution of the grid used to compute Voronoi cell boundaries.
        // Higher = smoother shard edges, more CPU cost. 48 is a good balance.
        private const int GridResolution = 48;

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        // Generates shard GameObjects in world space.
        //   paneTransform  — the glass pane's transform (used for local-to-world)
        //   paneSize       — width (x) and height (y) of the pane in local units
        //   thickness      — extrusion depth of each shard
        //   shardCount     — number of Voronoi seeds / shards
        //   hitPointLocal  — impact point in the pane's LOCAL space (xy plane)
        //   material       — material assigned to every shard MeshRenderer
        public static List<GameObject> Generate(
            Transform paneTransform,
            Vector2 paneSize,
            float thickness,
            int shardCount,
            Vector2 hitPointLocal,
            Material material,
            float shardMass)
        {
            List<Vector2> seeds = GenerateSeeds(shardCount, paneSize, hitPointLocal);
            int[] cellMap       = BuildCellMap(seeds, paneSize);
            var shards          = new List<GameObject>(seeds.Count);

            for (int i = 0; i < seeds.Count; i++)
            {
                Mesh mesh = BuildShardMesh(i, cellMap, seeds, paneSize, thickness);
                if (mesh == null) continue;

                GameObject go = SpawnShard(mesh, material, paneTransform, shardMass);
                if (go != null)
                    shards.Add(go);
            }

            return shards;
        }

        // ------------------------------------------------------------------
        // Seed Generation — strong bias toward hit point
        // ------------------------------------------------------------------

        private static List<Vector2> GenerateSeeds(int count, Vector2 paneSize, Vector2 hitPointLocal)
        {
            var seeds = new List<Vector2>(count);

            // Half-size for clamping seeds within pane bounds.
            float hw = paneSize.x * 0.5f;
            float hh = paneSize.y * 0.5f;

            // ~60% of seeds clustered tightly around the hit point (inner ring).
            int clustered = Mathf.RoundToInt(count * 0.6f);
            for (int i = 0; i < clustered; i++)
            {
                // Gaussian-ish cluster: small radius, concentrated near impact.
                Vector2 offset = Random.insideUnitCircle * (Mathf.Min(paneSize.x, paneSize.y) * 0.25f);
                Vector2 seed   = hitPointLocal + offset;
                seed.x = Mathf.Clamp(seed.x, -hw, hw);
                seed.y = Mathf.Clamp(seed.y, -hh, hh);
                seeds.Add(seed);
            }

            // ~40% scattered across the rest of the pane for larger outer shards.
            int scattered = count - clustered;
            for (int i = 0; i < scattered; i++)
            {
                Vector2 seed = new Vector2(
                    Random.Range(-hw, hw),
                    Random.Range(-hh, hh)
                );
                seeds.Add(seed);
            }

            return seeds;
        }

        // ------------------------------------------------------------------
        // Cell Map — for each grid pixel, store the index of the nearest seed
        // ------------------------------------------------------------------

        private static int[] BuildCellMap(List<Vector2> seeds, Vector2 paneSize)
        {
            int res      = GridResolution;
            int[] cellMap = new int[res * res];

            for (int py = 0; py < res; py++)
            {
                for (int px = 0; px < res; px++)
                {
                    // Grid pixel → local pane space (-halfSize to +halfSize)
                    float lx = Mathf.Lerp(-paneSize.x * 0.5f, paneSize.x * 0.5f, (px + 0.5f) / res);
                    float ly = Mathf.Lerp(-paneSize.y * 0.5f, paneSize.y * 0.5f, (py + 0.5f) / res);

                    int   nearest  = 0;
                    float minDistSq = float.MaxValue;

                    for (int s = 0; s < seeds.Count; s++)
                    {
                        float dx = lx - seeds[s].x;
                        float dy = ly - seeds[s].y;
                        float dSq = dx * dx + dy * dy;
                        if (dSq < minDistSq)
                        {
                            minDistSq = dSq;
                            nearest   = s;
                        }
                    }

                    cellMap[py * res + px] = nearest;
                }
            }

            return cellMap;
        }

        // ------------------------------------------------------------------
        // Shard Mesh Construction
        // Each shard is a polygon (front face + back face + side walls).
        // ------------------------------------------------------------------

        private static Mesh BuildShardMesh(int cellIndex, int[] cellMap, List<Vector2> seeds, Vector2 paneSize, float thickness)
        {
            int res = GridResolution;

            // Collect all grid pixels belonging to this cell.
            var pixelPoints = new List<Vector2>();

            for (int py = 0; py < res; py++)
            {
                for (int px = 0; px < res; px++)
                {
                    if (cellMap[py * res + px] != cellIndex) continue;

                    float lx = Mathf.Lerp(-paneSize.x * 0.5f, paneSize.x * 0.5f, (px + 0.5f) / res);
                    float ly = Mathf.Lerp(-paneSize.y * 0.5f, paneSize.y * 0.5f, (py + 0.5f) / res);
                    pixelPoints.Add(new Vector2(lx, ly));
                }
            }

            if (pixelPoints.Count < 3) return null;

            // Extract the boundary polygon of the cell using a simple marching approach:
            // find the convex hull of the pixel centers. For Voronoi cells this is
            // a good enough approximation — cells are convex by definition.
            List<Vector2> hull = ConvexHull(pixelPoints);
            if (hull.Count < 3) return null;

            // Center the shard mesh at the cell's centroid so the pivot is sensible
            // for physics force application.
            Vector2 centroid = Centroid(hull);
            for (int i = 0; i < hull.Count; i++)
                hull[i] -= centroid;

            Mesh mesh = ExtrudePolygon(hull, thickness);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Store centroid in the mesh name so the spawner can position the GO.
            mesh.name = $"Shard_{cellIndex}|{centroid.x.ToString("F4", CultureInfo.InvariantCulture)}|{centroid.y.ToString("F4", CultureInfo.InvariantCulture)}";

            return mesh;
        }

        // ------------------------------------------------------------------
        // Polygon Extrusion — front cap + back cap + quad walls
        // ------------------------------------------------------------------

        private static Mesh ExtrudePolygon(List<Vector2> poly, float thickness)
        {
            int n       = poly.Count;
            float halfT = thickness * 0.5f;

            // Vertices: front ring + back ring
            var verts  = new Vector3[n * 2];
            var uvs    = new Vector2[n * 2];

            for (int i = 0; i < n; i++)
            {
                verts[i]     = new Vector3(poly[i].x, poly[i].y,  halfT);  // front
                verts[i + n] = new Vector3(poly[i].x, poly[i].y, -halfT);  // back
                uvs[i]       = new Vector2(poly[i].x, poly[i].y);
                uvs[i + n]   = new Vector2(poly[i].x, poly[i].y);
            }

            var tris = new List<int>();

            // Front cap (fan triangulation — works for convex polygons)
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i + 1);
            }

            // Back cap (reversed winding)
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(n);
                tris.Add(n + i + 1);
                tris.Add(n + i);
            }

            // Side walls — quad per edge
            for (int i = 0; i < n; i++)
            {
                int next  = (i + 1) % n;
                int a     = i;
                int b     = next;
                int c     = i + n;
                int d     = next + n;

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }

            Mesh mesh    = new Mesh();
            mesh.vertices  = verts;
            mesh.uv        = uvs;
            mesh.triangles = tris.ToArray();

            return mesh;
        }

        // ------------------------------------------------------------------
        // Shard GameObject Spawning
        // ------------------------------------------------------------------

        private static GameObject SpawnShard(Mesh mesh, Material material, Transform paneTransform, float mass)
        {
            // Parse centroid from mesh name.
            string[] parts = mesh.name.Split('|');
            if (parts.Length < 3) return null;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float cx)) return null;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float cy)) return null;

            // Convert centroid from pane local space to world space.
            Vector3 localCentroid = new Vector3(cx, cy, 0f);
            Vector3 worldPos      = paneTransform.TransformPoint(localCentroid);

            GameObject go = new GameObject(parts[0]);
            go.transform.SetPositionAndRotation(worldPos, paneTransform.rotation);

            MeshFilter mf   = go.AddComponent<MeshFilter>();
            mf.mesh         = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material     = material;

            // Convex MeshCollider required for Rigidbody collision.
            MeshCollider mc  = go.AddComponent<MeshCollider>();
            mc.sharedMesh    = mesh;
            mc.convex        = true;

            Rigidbody rb     = go.AddComponent<Rigidbody>();
            rb.mass          = mass;
            rb.useGravity    = true;
            rb.isKinematic   = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            return go;
        }

        // ------------------------------------------------------------------
        // Convex Hull — Andrew's monotone chain (O(n log n))
        // ------------------------------------------------------------------

        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            int n = points.Count;
            if (n < 3) return points;

            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var hull = new List<Vector2>(2 * n);

            // Lower hull
            for (int i = 0; i < n; i++)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            // Upper hull
            int lower = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
            => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        private static Vector2 Centroid(List<Vector2> poly)
        {
            Vector2 sum = Vector2.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }
    }
}