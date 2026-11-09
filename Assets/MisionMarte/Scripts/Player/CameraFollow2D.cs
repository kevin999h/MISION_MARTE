using UnityEngine;

namespace MisionMarte
{
    /// <summary>Cámara que sigue al jugador con suavizado, mira hacia adelante y no se sale del nivel.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(2f, 1.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float lookAhead = 1.5f;
        [Header("Límites del nivel")]
        [SerializeField] private float minX = -6f;
        [SerializeField] private float maxX = 120f;
        [SerializeField] private float minY = -4f;
        [SerializeField] private float maxY = 14f;

        private Camera cam;
        private Rigidbody2D targetRb;
        private Vector3 velocity;
        private float currentLook;
        private Vector3 shakeOffset;

        // Vibración de cámara (retroalimentación de errores). Se puede reducir en Ajustes.
        private static float shakeTime, shakeDuration, shakeAmp;
        public static void Shake(float duration = 0.25f, float amplitude = 0.15f)
        {
            if (AccessibilitySettings.ReduceShake) return;
            shakeDuration = shakeTime = duration; shakeAmp = amplitude;
        }

        private void Awake() { cam = GetComponent<Camera>(); shakeTime = 0f; }

        private void Start()
        {
            if (target == null && PlayerController.Instance != null) target = PlayerController.Instance.transform;
            if (target != null) { targetRb = target.GetComponent<Rigidbody2D>(); transform.position = Desired(true); }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (targetRb == null) targetRb = target.GetComponent<Rigidbody2D>();
            Vector3 pos = Vector3.SmoothDamp(transform.position - shakeOffset, Desired(false), ref velocity, smoothTime);
            shakeOffset = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                Vector2 r = Random.insideUnitCircle * shakeAmp * Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDuration));
                shakeOffset = new Vector3(r.x, r.y, 0f);
            }
            transform.position = pos + shakeOffset;
        }

        private Vector3 Desired(bool instant)
        {
            float h = 0f;
            if (targetRb != null) h = Mathf.Clamp(targetRb.linearVelocity.x / 6f, -1f, 1f);
            currentLook = instant ? h * lookAhead : Mathf.Lerp(currentLook, h * lookAhead, Time.deltaTime * 2f);

            float x = target.position.x + offset.x * 0.5f + currentLook;
            float y = target.position.y + offset.y;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            x = Mathf.Clamp(x, minX + halfW, Mathf.Max(minX + halfW, maxX - halfW));
            y = Mathf.Clamp(y, minY + halfH, Mathf.Max(minY + halfH, maxY - halfH));
            return new Vector3(x, y, transform.position.z);
        }
    }
}
