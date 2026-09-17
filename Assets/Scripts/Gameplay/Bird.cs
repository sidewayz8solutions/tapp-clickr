using UnityEngine;

namespace TappBird
{
    /// <summary>
    /// The woodpecker. Idles with a gentle bob and plays a 3-phase peck lunge
    /// (wind-up, thrust, spring back) driven entirely by tweens.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Transform holder;       // idle bob happens on the holder
        public float size = 1f;        // uniform world scale, assigned by bootstrap

        bool busy;
        float bobT;

        void Update()
        {
            if (holder == null) return;
            if (!busy)
            {
                bobT += Time.deltaTime * 2.1f;
                holder.localPosition = GameCfg.BirdPos + new Vector3(0f, Mathf.Sin(bobT) * 0.045f, 0f);
            }
        }

        /// <summary>Where wood chips should spawn in front of the beak.</summary>
        public Vector3 ForwardPeckPoint
        {
            get
            {
                Vector2 basePos = holder != null ? holder.localPosition : GameCfg.BirdPos;
                return holder.parent.TransformPoint(new Vector3(basePos.x + 0.8f, basePos.y - 0.5f, 0));
            }
        }

        public void PlayPeck()
        {
            if (busy) return;
            busy = true;
            Vector3 home = Vector3.zero; // local zero under holder

            // phase 1: wind up (lean back + squash)
            Tween.Float(0f, 1f, 0.10f, v =>
            {
                float e = Ease.OutCubic(v);
                transform.localPosition = home + new Vector3(-0.14f * e, 0.02f * e, 0);
                transform.localRotation = Quaternion.Euler(0, 0, 14f * e);
                SetScale(size * (1f + 0.10f * e), size * (1f - 0.09f * e));
            }, () =>
            {
                // phase 2: thrust toward tree (stretch forward)
                Tween.Float(0f, 1f, 0.13f, v =>
                {
                    float e = Ease.InQuad(v);
                    transform.localPosition = home + new Vector3(-0.14f + 0.52f * e, 0.02f - 0.16f * e, 0);
                    transform.localRotation = Quaternion.Euler(0, 0, 14f - 56f * e);
                    SetScale(size * (1f - 0.10f * e), size * (1f + 0.12f * e));
                }, () =>
                {
                    // phase 3: spring back to idle
                    var thrustPos = home + new Vector3(0.38f, -0.14f, 0);
                    Tween.Float(0f, 1f, 0.34f, v =>
                    {
                        float e = Ease.OutElastic(v);
                        transform.localPosition = Vector3.LerpUnclamped(thrustPos, home, e);
                        transform.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-42f, 0f, e));
                        SetScale(Mathf.LerpUnclamped(size * 0.92f, size, e), Mathf.LerpUnclamped(size * 1.10f, size, e));
                    }, () => busy = false);
                });
            });
        }

        void SetScale(float x, float y)
        {
            transform.localScale = new Vector3(x, y, 1f);
        }
    }
}