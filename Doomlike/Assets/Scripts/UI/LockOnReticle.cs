using UnityEngine;
using Doomlike.Player;

namespace Doomlike.UI
{
    public class LockOnReticle : MonoBehaviour
    {
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] Camera worldCamera;
        [SerializeField] RectTransform reticle;
        [SerializeField] Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        void LateUpdate()
        {
            if (lockOn == null || reticle == null) return;
            bool active = lockOn.IsActive && lockOn.CurrentTarget != null;
            reticle.gameObject.SetActive(active);
            if (!active) return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;

            Vector3 worldPos = lockOn.CurrentTarget.position + worldOffset;
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z < 0f) { reticle.gameObject.SetActive(false); return; }
            reticle.position = screen;
        }
    }
}
