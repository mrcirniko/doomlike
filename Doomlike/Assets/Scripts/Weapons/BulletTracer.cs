using UnityEngine;

namespace Doomlike.Weapons
{
    /// <summary>
    /// Lightweight tracer drawn from muzzle to hit-point for hitscan weapons.
    /// Attach to a prefab with a LineRenderer; the script positions both points and fades alpha.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [SerializeField] float fadeDuration = 0.06f;
        [SerializeField] AnimationCurve widthOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        LineRenderer line;
        float age;
        float baseStartWidth;
        float baseEndWidth;
        Color baseStartColor;
        Color baseEndColor;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            baseStartWidth = line.startWidth;
            baseEndWidth = line.endWidth;
            baseStartColor = line.startColor;
            baseEndColor = line.endColor;
        }

        public void Play(Vector3 from, Vector3 to)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            age = 0f;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / Mathf.Max(0.001f, fadeDuration));
            float widthMul = widthOverLife.Evaluate(k);
            line.startWidth = baseStartWidth * widthMul;
            line.endWidth = baseEndWidth * widthMul;
            Color sc = baseStartColor; sc.a = baseStartColor.a * (1f - k);
            Color ec = baseEndColor; ec.a = baseEndColor.a * (1f - k);
            line.startColor = sc;
            line.endColor = ec;
        }
    }
}
