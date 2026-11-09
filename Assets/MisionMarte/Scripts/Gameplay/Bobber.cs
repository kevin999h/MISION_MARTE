using UnityEngine;

namespace MisionMarte
{
    /// <summary>Hace flotar suavemente un objeto (indicadores "E", decoraciones).</summary>
    public class Bobber : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.1f;
        [SerializeField] private float speed = 3f;
        private Vector3 basePos;

        private void Awake() { basePos = transform.localPosition; }
        private void Update() { transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude; }
    }
}
