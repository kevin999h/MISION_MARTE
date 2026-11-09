using UnityEngine;

namespace MisionMarte
{
    /// <summary>Capa de fondo que se mueve más lento que la cámara (efecto de profundidad).</summary>
    public class Parallax : MonoBehaviour
    {
        [SerializeField] private Transform cam;
        [SerializeField] private float factorX = 0.6f;
        [SerializeField] private float factorY = 0.8f;
        private Vector3 start;

        private void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            start = transform.position;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            transform.position = new Vector3(start.x + cam.position.x * factorX, start.y + cam.position.y * factorY, start.z);
        }
    }
}
