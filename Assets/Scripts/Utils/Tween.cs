using System;
using System.Collections.Generic;
using UnityEngine;

namespace TappBird
{
    /// <summary>Small tween engine for transform juice and value tweens. No external deps.</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float OutBack(float t)
        {
            float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (float)(2.0943951023931953); // 2PI/3
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        public static float ElasticBounce(float t) => OutElastic(t);
        public static float InQuad(float t) => t * t;
    }

    public static class Tween
    {
        sealed class Active
        {
            public float elapsed, dur;
            public float from, to;
            public Func<float, float> ease;
            public Action<float> update;
            public Action done;
            public bool alive = true;
        }

        static readonly List<Active> tweens = new List<Active>();
        static int frameTweens; // guard against adding during iteration

        /// <summary>Called once per frame from TweenHost.</summary>
        public static void TickAll(float dt)
        {
            frameTweens = 0;
            for (int i = 0; i < tweens.Count; i++)
            {
                var tw = tweens[i];
                if (!tw.alive) continue;
                tw.elapsed += dt;
                float t = Mathf.Clamp01(tw.elapsed / Mathf.Max(0.0001f, tw.dur));
                float e = tw.ease(t);
                tw.update(Mathf.Lerp(tw.from, tw.to, e));
                if (t >= 1f)
                {
                    tw.alive = false;
                    tweens.RemoveAt(i);
                    i--;
                    var done = tw.done;
                    if (done != null) done();
                }
            }
            frameTweens = tweens.Count;
        }

        public static void Float(float from, float to, float dur, Action<float> update, Func<float, float> ease = null)
        {
            tweens.Add(new Active { from = from, to = to, dur = dur, ease = ease ?? Ease.OutCubic, update = update });
        }

        public static void Float(float from, float to, float dur, Action<float> update, Action done, Func<float, float> ease = null)
        {
            tweens.Add(new Active { from = from, to = to, dur = dur, ease = ease ?? Ease.OutCubic, update = update, done = done });
        }

        public static void Scale(Transform t, Vector3 to, float dur, Func<float, float> ease = null, Action done = null)
        {
            var from = t.localScale;
            tweens.Add(new Active
            {
                from = 0f, to = 1f, dur = dur, ease = Ease.Linear,
                update = k =>
                {
                    float e = (ease ?? Ease.OutCubic)(k);
                    t.localScale = Vector3.LerpUnclamped(from, to, e);
                },
                done = done
            });
        }

        public static void MoveLocal(Transform t, Vector3 to, float dur, Func<float, float> ease = null, Action done = null)
        {
            var from = t.localPosition;
            tweens.Add(new Active
            {
                from = 0f, to = 1f, dur = dur, ease = Ease.Linear,
                update = k =>
                {
                    float e = (ease ?? Ease.OutCubic)(k);
                    t.localPosition = Vector3.LerpUnclamped(from, to, e);
                },
                done = done
            });
        }

        public static void RotateLocal(Transform t, float toDeg, float dur, Func<float, float> ease = null, Action done = null)
        {
            var from = t.localRotation.eulerAngles.z;
            tweens.Add(new Active
            {
                from = 0f, to = 1f, dur = dur, ease = Ease.Linear,
                update = k =>
                {
                    float e = (ease ?? Ease.OutCubic)(k);
                    var q = Quaternion.Euler(0, 0, Mathf.Lerp(from, toDeg, e));
                    t.localRotation = q;
                },
                done = done
            });
        }

        public static void Shake(Transform t, float strength, int times, float perPunch, Action done = null)
        {
            var home = t.localPosition;
            int left = times;
            var rng = new System.Random(1234);
            Next();
            void Next()
            {
                if (left <= 0) { t.localPosition = home; done?.Invoke(); return; }
                left--;
                float s = strength * (left / (float)times);
                float k = (float)(rng.NextDouble() * 2.0 - 1.0);
                var target = home + new Vector3(k, (float)(rng.NextDouble() * 2.0 - 1.0), 0f) * s;
                tweens.Add(new Active
                {
                    from = 0f, to = 1f, dur = perPunch, ease = Ease.InQuad,
                    update = v => t.localPosition = Vector3.Lerp(home, target, v),
                    done = Next
                });
            }
        }
    }
}