using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MisionMarte
{
    /// <summary>
    /// Crea el EventSystem con el módulo de entrada correcto (Input System nuevo o clásico)
    /// para que los botones del menú funcionen sin configurar nada a mano.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UIEventSystemFix : MonoBehaviour
    {
        private void Awake()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var old = es.GetComponent<StandaloneInputModule>();
            if (old != null) Destroy(old);
            if (es.GetComponent<InputSystemUIInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (es.GetComponent<StandaloneInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
