using UnityEngine;

namespace TappBird
{
    /// <summary>
    /// All sound effects are synthesized at runtime (no audio files in the project).
    /// Generates short WAV-shaped PCM clips: wood knocks, crackle, coins, UI blips.
    /// </summary>
    public static class Sfx
    {
        public static AudioClip Peck;
        public static AudioClip Break;
        public static AudioClip Coin;
        public static AudioClip Buy;
        public static AudioClip Pop;

        static AudioSource source;

        public static void Init(AudioSource host)
        {
            source = host;
            Peck = Make("peck", MakePeck());
            Break = Make("break", MakeBreak());
            Coin = Make("coin", MakeCoin());
            Buy = Make("buy", MakeBuy());
            Pop = Make("pop", MakePop());
        }

        public static void SetMuted(bool muted) => AudioListener.volume = muted ? 0f : 1f;

        public static void Play(AudioClip clip, float vol = 1f, float pitch = 1f)
        {
            if (clip == null || source == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, vol);
            source.pitch = 1f;
        }

        static AudioClip Make(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, 44100, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ---- synthesis helpers ------------------------------------------

        public static float[] SineSweep(float f0, float f1, float dur, float attack)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(44100f * dur));
            var s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / 44100f;
                float env = Env(t, dur, attack);
                s[i] = Mathf.Sin(phase) * env;
            }
            return s;
        }

        public static float[] NoiseBurst(float dur, float decayExp = 6f, int lowpass = 2)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(44100f * dur));
            var s = new float[n];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.6f;
                if (lowpass > 1)
                {
                    lp = (lp * 3f + x) * 0.25f;
                    x = lp;
                }
                float env = Mathf.Exp(-decayExp * t);
                s[i] = x * env;
            }
            return s;
        }

        static float Env(float t /*0..1*/, float dur, float attack) =>
            dur <= 0.0001f ? 1f : Mathf.Min(1f, (t * dur) / Mathf.Max(0.001f, attack)) * Mathf.Exp(-3.5f * t);

        public static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n = Mathf.Max(n, p.Length);
            var outS = new float[n];
            foreach (var p in parts)
            {
                for (int i = 0; i < p.Length; i++) outS[i] += p[i] * 0.8f;
            }
            Trim(outS);
            return outS;
        }

        static void Trim(float[] s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                s[i] = Mathf.Clamp(s[i], -1f, 1f) * 0.9f;
            }
        }

        // ---- the sounds ---------------------------------------------------

        static float[] MakePeck()
        {
            // quick wooden knock: noise crack + low body thump
            var crack = NoiseBurst(0.02f, 14f, 1);
            var thump = SineSweep(150f, 70f, 0.06f, 0.002f);
            for (int i = 0; i < thump.Length; i++) thump[i] *= 0.55f;
            return Mix(crack, thump);
        }

        static float[] MakeBreak()
        {
            var crack = NoiseBurst(0.18f, 7f, 2);
            for (int i = 0; i < crack.Length; i++) crack[i] *= 0.85f;
            var low = SineSweep(95f, 45f, 0.18f, 0.004f);
            for (int i = 0; i < low.Length; i++) low[i] *= 0.8f;
            var chips = NoiseBurst(0.34f, 5f, 3);
            for (int i = 0; i < chips.Length; i++) chips[i] *= 0.4f;
            return Mix(crack, low, chips);
        }

        static float[] MakeCoin()
        {
            var n1 = SineSweep(1318f, 1318f, 0.09f, 0.002f); // E6
            var n2 = SineSweep(1760f, 1760f, 0.22f, 0.002f); // A6
            for (int i = 0; i < n1.Length; i++) n1[i] *= 0.5f;
            var outS = new float[Mathf.Max(n1.Length, n2.Length + (int)(0.07f * 44100f))];
            for (int i = 0; i < n1.Length; i++) outS[i] += n1[i] * 0.55f;
            int off = (int)(0.07f * 44100f);
            for (int i = 0; i < n2.Length && off + i < outS.Length; i++) outS[i + off] += n2[i] * 0.8f;
            Trim(outS);
            return outS;
        }

        static float[] MakeBuy()
        {
            int off = (int)(0.06f * 44100f);
            var a = SineSweep(660f, 660f, 0.1f, 0.002f);
            var b = SineSweep(880f, 880f, 0.1f, 0.002f);
            var c = SineSweep(1320f, 1320f, 0.24f, 0.002f);
            var outS = new float[a.Length + off * 2 + c.Length];
            for (int i = 0; i < a.Length; i++) outS[i] += a[i] * 0.5f;
            for (int i = 0; i < b.Length && off + i < outS.Length; i++) outS[i + off] += b[i] * 0.5f;
            int off2 = off * 2;
            for (int i = 0; i < c.Length && off2 + i < outS.Length; i++) outS[i + off2] += c[i] * 0.8f;
            Trim(outS);
            return outS;
        }

        static float[] MakePop() => Mix(SineSweep(520f, 900f, 0.05f, 0.002f));
    }
}