using UnityEngine;

namespace UI
{
    public class MobileUIHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showInEditor = true;

        private void Start()
        {
            #if UNITY_EDITOR
            if (showInEditor)
            {
                Debug.Log($"[MobileUIHandler] Running in Editor, keeping {gameObject.name} active.");
                return;
            }
            #endif

            bool isMobile = Application.isMobilePlatform;

            if (!isMobile)
            {
                Debug.Log($"[MobileUIHandler] Non-mobile platform detected. Deactivating {gameObject.name}.");
                gameObject.SetActive(false);
            }
        }
    }
}
