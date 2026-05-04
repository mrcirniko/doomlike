using UnityEngine;

namespace Doomlike.Player
{
    /// <summary>
    /// Adds a subtle position bob to the camera's local position when the player is moving on the ground.
    /// Place on the camera. Does not touch rotation, so PlayerController's mouse-look stays intact.
    /// </summary>
    public class CameraBob : MonoBehaviour
    {
        [SerializeField] CharacterController controller;
        [SerializeField] float frequency = 8f;
        [SerializeField] float verticalAmount = 0.04f;
        [SerializeField] float horizontalAmount = 0.02f;
        [SerializeField] float walkReferenceSpeed = 6f;
        [SerializeField] float response = 12f;

        Vector3 basePos;
        float phase;

        void Awake()
        {
            basePos = transform.localPosition;
            if (controller == null) controller = GetComponentInParent<CharacterController>();
        }

        void LateUpdate()
        {
            float speed = 0f;
            if (controller != null && controller.isGrounded)
            {
                Vector3 v = controller.velocity;
                speed = new Vector2(v.x, v.z).magnitude;
            }
            float strength = Mathf.Clamp01(speed / Mathf.Max(0.01f, walkReferenceSpeed));

            phase += Time.deltaTime * frequency * Mathf.Lerp(0.6f, 1.4f, strength);
            float bx = Mathf.Sin(phase) * horizontalAmount * strength;
            float by = Mathf.Sin(phase * 2f) * verticalAmount * strength;

            Vector3 desired = basePos + new Vector3(bx, by, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, desired, response * Time.deltaTime);
        }
    }
}
