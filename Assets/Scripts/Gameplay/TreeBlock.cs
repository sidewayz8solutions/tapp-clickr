using UnityEngine;

namespace TappBird
{
    /// <summary>The tree the bird pecks. Owns its health and hit feedback.</summary>
    public class TreeBlock : MonoBehaviour
    {
        public SpriteRenderer sr;

        int level;
        float hp, maxHp;
        bool dead;

        public void SetLevel(int n)
        {
            level = n;
            maxHp = GameCfg.TreeHealth(n);
            hp = maxHp;
            dead = false;
        }

        /// <summary>Apply damage. Returns true when this tree breaks.</summary>
        public bool Damage(float dmg)
        {
            if (dead) return false;
            hp -= dmg;

            // hit feedback: tiny shake + hardness pulse
            Tween.Shake(transform, 0.05f, 3, 0.035f);
            if (sr != null)
            {
                sr.color = new Color(0.78f, 0.82f, 0.78f);
                Tween.Float(0f, 1f, 0.16f, v => sr.color = Color.Lerp(new Color(0.78f, 0.82f, 0.78f), Color.white, v));
            }

            if (hp <= 0f)
            {
                dead = true;
                return true;
            }
            return false;
        }
    }
}