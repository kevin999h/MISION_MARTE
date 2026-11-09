using UnityEngine;

namespace MisionMarte
{
    /// <summary>Plataforma que va y viene entre dos puntos. El jugador se mueve con ella.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 travel = new Vector2(6f, 0f);
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float waitTime = 0.5f;

        private Rigidbody2D rb;
        private Vector2 a, b;
        private bool toB = true;
        private float wait;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a = rb.position; b = a + travel;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (wait > 0f) { wait -= dt; rb.linearVelocity = Vector2.zero; return; }
            Vector2 target = toB ? b : a;
            Vector2 delta = target - rb.position;
            if (delta.magnitude <= speed * dt)
            {
                rb.linearVelocity = delta / dt;
                toB = !toB; wait = waitTime;
            }
            else rb.linearVelocity = delta.normalized * speed;
        }
    }
}
