using UnityEngine;

namespace MisionMarte
{
    /// <summary>Pequeña explosión de partículas (al recoger objetos, activar mecanismos...).</summary>
    public class Burst : MonoBehaviour
    {
        private Transform[] parts;
        private Vector2[] vel;
        private SpriteRenderer[] srs;
        private float age;
        private const float Life = 0.6f;

        public static void Spawn(Vector3 pos, Color color, Sprite sprite, int count = 12)
        {
            if (sprite == null) return;
            var go = new GameObject("Burst");
            go.transform.position = pos;
            var b = go.AddComponent<Burst>();
            b.parts = new Transform[count]; b.vel = new Vector2[count]; b.srs = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                var p = new GameObject("p");
                p.transform.SetParent(go.transform, false);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.color = color; sr.sortingOrder = 30; sr.sortingLayerName = "FX"; SpriteRenderFix.Apply(sr);
                p.transform.localScale = Vector3.one * Random.Range(0.35f, 0.6f);
                float a = Random.value * Mathf.PI * 2f;
                b.vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2f, 5f);
                b.parts[i] = p.transform; b.srs[i] = sr;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            for (int i = 0; i < parts.Length; i++)
            {
                vel[i] *= 1f - 3f * dt;
                parts[i].localPosition += (Vector3)(vel[i] * dt);
                var c = srs[i].color; c.a = 1f - age / Life; srs[i].color = c;
            }
            if (age >= Life) Destroy(gameObject);
        }
    }
}
