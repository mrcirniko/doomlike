using UnityEngine;

namespace Doomlike.Core
{
    /// <summary>
    /// Plays an AudioClip at a world position via a temporary AudioSource. Auto-destroys after the clip ends.
    /// </summary>
    public static class AudioOneShot
    {
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0f, float spatialBlend = 1f, float maxDistance = 25f)
        {
            if (clip == null) return;
            var go = new GameObject("OneShotAudio_" + clip.name);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = spatialBlend;
            src.volume = volume;
            src.maxDistance = maxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            src.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch) + 0.1f);
        }
    }
}
