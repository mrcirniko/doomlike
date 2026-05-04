using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Doomlike.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "Arena_Prototype";
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
        }

        void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(StartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(StartGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        public void StartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
