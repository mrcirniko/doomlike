using System.Collections;
using UnityEngine;

namespace Doomlike.Core
{
    /// <summary>
    /// Simple background music controller. Plays one of several tracks (random or sequential),
    /// loops, supports crossfading via PlayTrack(index).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip[] tracks;
        [SerializeField, Range(0f, 1f)] float volume = 0.4f;
        [SerializeField] bool playOnStart = true;
        [SerializeField] bool randomOrder = true;
        [SerializeField] float crossfadeDuration = 1.5f;
        [SerializeField] bool persistAcrossScenes = true;

        AudioSource sourceA;
        AudioSource sourceB;
        AudioSource active;
        int lastIndex = -1;

        void Awake()
        {
            sourceA = GetComponent<AudioSource>();
            sourceA.loop = true;
            sourceA.playOnAwake = false;
            sourceA.volume = 0f;
            sourceA.spatialBlend = 0f;

            sourceB = gameObject.AddComponent<AudioSource>();
            sourceB.loop = true;
            sourceB.playOnAwake = false;
            sourceB.volume = 0f;
            sourceB.spatialBlend = 0f;

            active = sourceA;

            if (persistAcrossScenes && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (playOnStart && tracks != null && tracks.Length > 0)
                PlayNext();
        }

        public void PlayNext()
        {
            if (tracks == null || tracks.Length == 0) return;
            int idx;
            if (randomOrder)
            {
                idx = Random.Range(0, tracks.Length);
                if (tracks.Length > 1 && idx == lastIndex) idx = (idx + 1) % tracks.Length;
            }
            else
            {
                idx = (lastIndex + 1) % tracks.Length;
            }
            PlayTrack(idx);
        }

        public void PlayTrack(int index)
        {
            if (tracks == null || index < 0 || index >= tracks.Length) return;
            AudioClip clip = tracks[index];
            if (clip == null) return;
            lastIndex = index;
            StopAllCoroutines();
            StartCoroutine(Crossfade(clip));
        }

        IEnumerator Crossfade(AudioClip nextClip)
        {
            AudioSource fadingOut = active;
            AudioSource fadingIn = (active == sourceA) ? sourceB : sourceA;

            fadingIn.clip = nextClip;
            fadingIn.volume = 0f;
            fadingIn.Play();

            float t = 0f;
            float duration = Mathf.Max(0.01f, crossfadeDuration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                fadingIn.volume = volume * k;
                fadingOut.volume = volume * (1f - k);
                yield return null;
            }
            fadingIn.volume = volume;
            fadingOut.volume = 0f;
            fadingOut.Stop();
            active = fadingIn;
        }

        public void Stop()
        {
            StopAllCoroutines();
            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();
        }
    }
}
