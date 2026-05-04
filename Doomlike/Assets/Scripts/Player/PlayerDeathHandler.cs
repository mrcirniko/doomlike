using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doomlike.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] float restartDelay = 2.5f;
        [SerializeField] CanvasGroup deathScreen;
        [SerializeField] MonoBehaviour[] disableOnDeath;

        PlayerHealth health;

        void Awake() => health = GetComponent<PlayerHealth>();
        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (deathScreen != null)
            {
                deathScreen.alpha = 1f;
                deathScreen.gameObject.SetActive(true);
            }

            StartCoroutine(RestartAfter(restartDelay));
        }

        IEnumerator RestartAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
