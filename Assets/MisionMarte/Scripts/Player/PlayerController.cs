using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MisionMarte
{
    /// <summary>
    /// Control del personaje (Ligi o Mariale) en 2D lateral.
    /// - Caminar (A/D), correr (Shift), saltar con altura variable (Espacio/W)
    /// - Coyote time + buffer de salto (el salto se siente justo, no "se come" teclas)
    /// - Deslizarse (S/Ctrl en movimiento) y agacharse (S/Ctrl quieto); no se para si hay techo encima
    /// - Turbo de velocidad (celdas de energía y cada 10 tuercas)
    /// - Anti-caídas: detección continua de colisiones, sin fricción en paredes y respawn en el último punto de control
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runMultiplier = 1.4f;
        [SerializeField] private float acceleration = 70f;
        [SerializeField] private float deceleration = 80f;
        [SerializeField] private float airControl = 0.75f;

        [Header("Salto (gravedad marciana suave)")]
        [SerializeField] private float jumpForce = 13f;
        [SerializeField] private float baseGravity = 3f;
        [SerializeField] private float fallGravityMultiplier = 1.5f;
        [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
        [SerializeField] private float maxFallSpeed = 18f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Deslizar / Agacharse")]
        [SerializeField] private float slideSpeed = 11f;
        [SerializeField] private float slideDuration = 0.45f;
        [SerializeField] private float slideCooldown = 0.3f;
        [SerializeField] private float crouchSpeedMultiplier = 0.45f;

        [Header("Colisionador (de pie / agachado)")]
        [SerializeField] private Vector2 standSize = new Vector2(0.62f, 1.32f);
        [SerializeField] private Vector2 standOffset = new Vector2(0f, -0.05f);
        [SerializeField] private Vector2 crouchSize = new Vector2(0.62f, 0.62f);
        [SerializeField] private Vector2 crouchOffset = new Vector2(0f, -0.4f);

        [Header("Visual")]
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite ligiSprite;
        [SerializeField] private Sprite mariSprite;
        [SerializeField] private float visualHeight = 1.5f;

        [Header("Seguridad (anti-caídas)")]
        [SerializeField] private float fallLimitY = -12f;

        // --- estado ---
        private Rigidbody2D rb;
        private BoxCollider2D box;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private ContactFilter2D solidFilter;

        private float inputX;
        private bool jumpHeld, slideHeld, slidePressed, runHeld;
        private float jumpBufferTimer, coyoteTimer, slideTimer, slideCooldownTimer, boostTimer, boostMultiplier = 1f;
        private bool grounded, wasGrounded, isSliding, isSmall;
        private float slideDir = 1f, slideCurrentSpeed;
        private Vector2 groundVelocity;
        private Vector3 respawnPoint;
        private Vector3 visualBaseScale = Vector3.one;
        private float bobTime;
        private bool facingRight = true;
        private float invulnerableTimer;

        public bool IsGrounded => grounded;
        public float BoostRemaining => Mathf.Max(0f, boostTimer);
        public bool IsSliding => isSliding;

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();

            // Configuración física robusta (parche anti-caídas: no atraviesa el suelo ni se pega a paredes)
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.gravityScale = baseGravity;
            box.edgeRadius = 0.04f; // bordes redondeados: no se traba en las uniones entre plataformas
            if (box.sharedMaterial == null)
                box.sharedMaterial = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };

            solidFilter = new ContactFilter2D();
            solidFilter.useTriggers = false;
            solidFilter.useLayerMask = false;

            ApplyCharacter();
            SetSmall(false, true);
            respawnPoint = transform.position;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Pone el sprite y los bonos del personaje elegido en el menú.</summary>
        private void ApplyCharacter()
        {
            var s = GameSession.Selected == Heroine.Ligi ? ligiSprite : mariSprite;
            if (spriteRenderer != null && s != null)
            {
                spriteRenderer.sprite = s;
                float h = s.bounds.size.y;
                float k = h > 0.001f ? visualHeight / h : 1f;
                visualBaseScale = new Vector3(k, k, 1f);
                if (visual != null) visual.localScale = visualBaseScale;
            }
        }

        private void Update()
        {
            if (GameState.Locked)
            {
                inputX = 0f; jumpHeld = false; slideHeld = false; runHeld = false;
                jumpBufferTimer = 0f;
                UpdateVisual();
                return;
            }

            inputX = GameInput.Horizontal();
            jumpHeld = GameInput.JumpHeld();
            slideHeld = GameInput.SlideHeld();
            runHeld = GameInput.RunHeld();
            if (GameInput.JumpDown()) jumpBufferTimer = jumpBufferTime;
            if (GameInput.SlideDown()) slidePressed = true;

            jumpBufferTimer -= Time.deltaTime;
            slideCooldownTimer -= Time.deltaTime;
            invulnerableTimer -= Time.deltaTime;
            if (boostTimer > 0f) { boostTimer -= Time.deltaTime; if (boostTimer <= 0f) boostMultiplier = 1f; }

            if (inputX > 0.05f) facingRight = true;
            else if (inputX < -0.05f) facingRight = false;

            // Red de seguridad extra: si por cualquier motivo cae del mapa, vuelve al punto de control.
            if (transform.position.y < fallLimitY) Respawn();

            UpdateVisual();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            wasGrounded = grounded;
            CheckGround();
            if (grounded) coyoteTimer = coyoteTime; else coyoteTimer -= dt;

            // --- Deslizamiento ---
            if (isSliding)
            {
                slideTimer -= dt;
                float t = 1f - Mathf.Clamp01(slideTimer / slideDuration);
                slideCurrentSpeed = Mathf.Lerp(slideSpeed * boostMultiplier, walkSpeed * 0.5f, t * t);
                if (slideTimer <= 0f || !grounded) EndSlide();
            }
            else if (slidePressed && grounded && Mathf.Abs(inputX) > 0.1f && slideCooldownTimer <= 0f && !GameState.Locked)
            {
                StartSlide();
            }
            slidePressed = false;

            bool ceiling = isSmall && IsCeilingBlocked();
            bool wantCrouch = slideHeld && grounded;
            SetSmall(isSliding || wantCrouch || ceiling);

            // --- Movimiento horizontal (relativo a plataformas móviles) ---
            Vector2 v = rb.linearVelocity;
            float relX = v.x - groundVelocity.x;
            if (isSliding)
            {
                relX = slideDir * slideCurrentSpeed;
            }
            else
            {
                float speed = walkSpeed * GameSession.SpeedMultiplier * boostMultiplier * (runHeld ? runMultiplier : 1f);
                if (isSmall) speed *= crouchSpeedMultiplier;
                float target = inputX * speed;
                float accel = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
                if (!grounded) accel *= airControl;
                relX = Mathf.MoveTowards(relX, target, accel * dt);
            }
            v.x = relX + groundVelocity.x;

            // --- Salto ---
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && !ceiling)
            {
                if (isSliding) EndSlide();
                SetSmall(false);
                v.y = jumpForce * GameSession.JumpMultiplier;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                Sfx.Play(SfxType.Jump);
                Squash(0.8f, 1.2f);
            }

            // Gravedad variable: cae más rápido y salto corto si suelta la tecla
            if (v.y < -0.01f) rb.gravityScale = baseGravity * fallGravityMultiplier;
            else if (v.y > 0.01f && !jumpHeld) rb.gravityScale = baseGravity * lowJumpGravityMultiplier;
            else rb.gravityScale = baseGravity;

            v.y = Mathf.Max(v.y, -maxFallSpeed);
            rb.linearVelocity = v;

            if (grounded && !wasGrounded) Squash(1.2f, 0.85f);
        }

        /// <summary>Detecta el suelo lanzando la forma del colisionador hacia abajo (no necesita capas).</summary>
        private void CheckGround()
        {
            grounded = false;
            groundVelocity = Vector2.zero;
            if (rb.linearVelocity.y > 0.5f) return; // subiendo: no está en el suelo
            int n = box.Cast(Vector2.down, solidFilter, hits, 0.06f);
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
                if (hits[i].normal.y < 0.55f) continue;
                grounded = true;
                var other = hits[i].rigidbody;
                if (other != null && other.bodyType == RigidbodyType2D.Kinematic) groundVelocity = other.linearVelocity;
            }
        }

        private bool IsCeilingBlocked()
        {
            float extra = standSize.y - box.size.y + 0.02f;
            if (extra <= 0.03f) return false;
            int n = box.Cast(Vector2.up, solidFilter, hits, extra);
            for (int i = 0; i < n; i++)
                if (hits[i].collider != null && !hits[i].collider.isTrigger && hits[i].normal.y < -0.5f) return true;
            return false;
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideDir = Mathf.Sign(inputX);
            slideCurrentSpeed = slideSpeed * boostMultiplier;
            Sfx.Play(SfxType.Slide);
        }

        private void EndSlide()
        {
            if (!isSliding) return;
            isSliding = false;
            slideCooldownTimer = slideCooldown;
        }

        private void SetSmall(bool small, bool force = false)
        {
            if (!force && small == isSmall) return;
            isSmall = small;
            box.size = small ? crouchSize : standSize;
            box.offset = small ? crouchOffset : standOffset;
        }

        // ---------------- API pública ----------------
        public void ApplyBoost(float duration, float multiplier)
        {
            boostTimer = Mathf.Max(boostTimer, duration);
            boostMultiplier = Mathf.Max(boostMultiplier, multiplier);
            Sfx.Play(SfxType.Boost);
        }

        public void SetCheckpoint(Vector3 point) => respawnPoint = point;

        /// <summary>Devuelve al jugador al último punto de control (caída o zona tóxica).</summary>
        public void Respawn()
        {
            if (invulnerableTimer > 0f) return;
            invulnerableTimer = 0.5f;
            EndSlide();
            SetSmall(false);
            rb.position = respawnPoint;
            transform.position = respawnPoint;
            rb.linearVelocity = Vector2.zero;
            Sfx.Play(SfxType.Hurt);
            StopCoroutine(nameof(Blink));
            StartCoroutine(nameof(Blink));
        }

        private IEnumerator Blink()
        {
            if (spriteRenderer == null) yield break;
            for (int i = 0; i < 8; i++)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.08f);
            }
            spriteRenderer.enabled = true;
        }

        // ---------------- Visual (animación procedural) ----------------
        private float squashTimer; private Vector2 squashAmount = Vector2.one;
        private void Squash(float x, float y) { squashAmount = new Vector2(x, y); squashTimer = 0.12f; }

        private void UpdateVisual()
        {
            if (visual == null) return;
            if (spriteRenderer != null) spriteRenderer.flipX = !facingRight;

            squashTimer -= Time.deltaTime;
            Vector2 sq = squashTimer > 0f ? squashAmount : Vector2.one;

            float smallY = isSmall ? 0.55f : 1f;
            bool moving = grounded && Mathf.Abs(inputX) > 0.1f && !isSmall;
            bobTime = moving ? bobTime + Time.deltaTime * 14f : 0f;
            float bob = moving ? Mathf.Abs(Mathf.Sin(bobTime)) * 0.06f : 0f;

            visual.localScale = new Vector3(visualBaseScale.x * sq.x, visualBaseScale.y * sq.y * smallY, 1f);
            // Mantiene los pies en el suelo al agacharse
            float footY = standOffset.y - standSize.y * 0.5f - box.edgeRadius;
            float y = footY + visualHeight * 0.5f * smallY * sq.y + bob;
            visual.localPosition = new Vector3(0f, y, 0f);
            float tilt = isSliding ? (facingRight ? -12f : 12f) : (grounded ? 0f : -rb.linearVelocity.x * 0.6f);
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt);

            if (spriteRenderer != null)
            {
                var c = Color.white;
                if (boostTimer > 0f) c = Color.Lerp(Color.white, new Color(0.5f, 0.95f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 20f));
                spriteRenderer.color = c;
            }
        }
    }
}
