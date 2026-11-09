using System.Collections.Generic;
using UnityEngine;

namespace MisionMarte
{
    /// <summary>Chorro de gotas de agua hecho con sprites (sin sistema de partículas, funciona en cualquier pipeline).</summary>
    public class WaterSpray : MonoBehaviour
    {
        [SerializeField] private Sprite dropSprite;
        [SerializeField] private float rate = 18f;
        [SerializeField] private Vector2 baseVelocity = new Vector2(2.2f, 1.2f);
        [SerializeField] private float gravity = 14f;
        [SerializeField] private float life = 0.7f;

        class Drop { public Transform t; public SpriteRenderer sr; public Vector2 v; public float age; }
        private readonly List<Drop> drops = new List<Drop>();
        private readonly Stack<Drop> pool = new Stack<Drop>();
        private float acc;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (enabled && dropSprite != null)
            {
                acc += dt * rate;
                while (acc >= 1f) { acc -= 1f; Spawn(); }
            }
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.age += dt;
                d.v.y -= gravity * dt;
                d.t.position += (Vector3)(d.v * dt);
                var c = d.sr.color; c.a = 1f - d.age / life; d.sr.color = c;
                if (d.age >= life) { d.t.gameObject.SetActive(false); pool.Push(d); drops.RemoveAt(i); }
            }
        }

        private void OnDisable()
        {
            foreach (var d in drops) if (d.t != null) d.t.gameObject.SetActive(false);
            foreach (var d in drops) pool.Push(d);
            drops.Clear();
        }

        private void Spawn()
        {
            Drop d = pool.Count > 0 ? pool.Pop() : null;
            if (d == null || d.t == null)
            {
                var go = new GameObject("gota");
                go.transform.SetParent(transform, false);
                d = new Drop { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
                d.sr.sprite = dropSprite; d.sr.sortingOrder = 12; d.sr.sortingLayerName = "FX"; SpriteRenderFix.Apply(d.sr);
                go.transform.localScale = Vector3.one * 0.35f;
            }
            d.t.gameObject.SetActive(true);
            d.t.position = transform.position;
            d.v = new Vector2(baseVelocity.x * Random.Range(0.7f, 1.2f), baseVelocity.y * Random.Range(0.5f, 1.3f));
            d.age = 0f;
            d.sr.color = new Color(0.6f, 0.85f, 1f, 1f);
            drops.Add(d);
        }
    }
}
