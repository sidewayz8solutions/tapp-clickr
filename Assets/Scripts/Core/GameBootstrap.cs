using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TappBird
{
    /// <summary>
    /// Builds the entire game from code at runtime. No scene references, no GUIDs.
    /// Spawned via [RuntimeInitializeOnLoadMethod] so the Bootstrap scene stays empty.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap Inst;
        static Font uiFont;

        ArtGen art;
        Transform worldRoot;
        TreeBlock treeBlock;
        Bird bird;
        GameObject shopPanel;
        Text coinText;
        Text[] shopLevels, shopCosts;
        Transform[] clouds;
        float cloudSpeed = 0.28f;
        float autoTimer;
        float treeScale, birdScale;
        Vector3 peckHoleWorld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Inst) return;
            var go = new GameObject("[TappBird Game]");
            Inst = go.AddComponent<GameBootstrap>();
        }

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;

            art = ArtGen.Build();
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "DejaVu Sans", "Liberation Sans", "Helvetica" }, 48);
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var audioGo = new GameObject("[Audio]");
            audioGo.transform.SetParent(transform, false);
            var src = audioGo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            Sfx.Init(src);

            if (!GameManager.Inst)
            {
                var gm = new GameObject("[GameManager]");
                gm.AddComponent<GameManager>();
                gm.AddComponent<AudioSource>().playOnAwake = false;
            }
            Sfx.SetMuted(!GameManager.Inst.SoundOn);
            GameManager.Inst.OnCoinsChanged += RefreshCoinUI;

            SetupCamera();
            SetupEnvironment();
            SetupCharacters();
            SetupParticles();
            SetupUI();

            treeScale = GameCfg.TreeWorldWidth / art.Tree.bounds.size.x;
            birdScale = GameCfg.BirdWorldWidth / art.Bird.bounds.size.x;

            treeBlock.SetLevel(GameManager.Inst.TreeIndex);
            treeBlock.transform.localScale = Vector3.one * treeScale;
            float halfTreeH = art.Tree.bounds.size.y * 0.5f * treeScale;
            treeBlock.transform.localPosition = new Vector3(GameCfg.TreePos.x, GameCfg.GroundLine + halfTreeH, 0);
            peckHoleWorld = new Vector3(GameCfg.TreePos.x, GameCfg.GroundLine + halfTreeH + GameCfg.PeckHoleOffsetY, 0);

            bird.size = birdScale;

            autoTimer = GameCfg.AutoPeckInterval(GameManager.Inst.Data.autoLevel);
            RefreshCoinUI();
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.SkyHorizon;
            cam.orthographic = true;
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = GameCfg.WorldWidth / (2f * aspect);
            cam.transform.position = new Vector3(0, 0, -10);
        }

        void SetupEnvironment()
        {
            float ortho = Camera.main.orthographicSize;
            float camW = ortho * 2f * Camera.main.aspect;

            worldRoot = new GameObject("[World]").transform;
            worldRoot.SetParent(transform, false);

            // sky: stretch to cover the whole view
            MakeSprite(worldRoot, art.Sky, "Sky", new Vector3(0, 0, 0), -1,
                new Vector3((camW + 1.5f) / art.Sky.bounds.size.x, (ortho * 2f + 1.5f) / art.Sky.bounds.size.y, 1));

            // warm sun glow, upper right
            MakeSprite(worldRoot, art.Sun, "Sun", new Vector3(camW * 0.29f, ortho * 0.7f, 0), 0, FitSprite(art.Sun, 4.4f));

            // drifting clouds
            clouds = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                float x = Random.Range(-camW * 0.5f, camW * 0.5f);
                float y = ortho * (0.42f + i * 0.22f);
                float w = 2.1f + i * 0.3f;
                clouds[i] = MakeSprite(worldRoot, art.Cloud, "Cloud" + i, new Vector3(x, y, 0), 1, FitSprite(art.Cloud, w)).transform;
            }

            // landscape bands, bottom-anchored behind the ground
            MakeSprite(worldRoot, art.HillsFar, "HillsFar", new Vector2(0, -2.3f), 2, FitSprite(art.HillsFar, camW * 1.05f));
            MakeSprite(worldRoot, art.ForestMid, "ForestMid", new Vector2(0, -2.05f), 3, FitSprite(art.ForestMid, camW * 1.05f));

            // ground: flat band from the ground line down to beneath the camera bottom
            Vector3 gs = FitSprite(art.Ground, camW * 1.1f);
            float groundH = GameCfg.GroundLine + ortho;
            gs.y = groundH / art.Ground.bounds.size.y;
            MakeSprite(worldRoot, art.Ground, "Ground", new Vector2(0, GameCfg.GroundLine - groundH * 0.5f), 4, gs);
        }

        static Vector3 FitSprite(Sprite s, float worldWidth)
        {
            float u = worldWidth / s.bounds.size.x;
            return new Vector3(u, u, 1f);
        }

        void SetupCharacters()
        {
            var chars = new GameObject("[Characters]").transform;
            chars.SetParent(transform, false);

            var treeGO = new GameObject("Tree", typeof(SpriteRenderer), typeof(TreeBlock));
            treeGO.transform.SetParent(chars, false);
            treeBlock = treeGO.GetComponent<TreeBlock>();
            treeBlock.sr = treeGO.GetComponent<SpriteRenderer>();
            treeBlock.sr.sprite = art.Tree;
            treeBlock.sr.sortingOrder = 10;
            treeBlock.sr.sortingLayerName = "Default";

            var birdHolder = new GameObject("BirdHolder").transform;
            birdHolder.SetParent(chars, false);
            birdHolder.localPosition = GameCfg.BirdPos;

            var birdGO = new GameObject("Bird", typeof(SpriteRenderer), typeof(Bird));
            birdGO.transform.SetParent(birdHolder, false);
            birdGO.transform.localPosition = Vector3.zero;
            bird = birdGO.GetComponent<Bird>();
            bird.sr = birdGO.GetComponent<SpriteRenderer>();
            bird.sr.sprite = art.Bird;
            bird.sr.sortingOrder = 20;
            bird.holder = birdHolder;
        }

        void SetupParticles()
        {
            var go = new GameObject("[Ambient Motes]", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 2.5f, 0);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.14f;
            main.startSize = 0.10f;
            main.maxParticles = 22;
            main.gravityModifier = -0.02f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.55f));
            var emission = ps.emission;
            emission.rateOverTime = 2.5f;
            var shape = ps.shape;
            shape.enabled = false;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            var alpha = ps.colorOverLifetime;
            alpha.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.8f, 0.25f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0, 1) });
            alpha.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = art.ParticleDot;
            renderer.material = mat;
            renderer.sortingOrder = 30;
        }

        void SetupUI()
        {
            if (!FindObjectOfType<EventSystem>())
            {
                new GameObject("[EventSystem]", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasGO = new GameObject("[UI Canvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(GameCfg.DesignWidth, GameCfg.DesignHeight);
            scaler.matchWidthOrHeight = 0.5f;

            // ---- coin pill (top center) ----
            var coinPill = MakeImage(canvasGO.transform, art.BtnRect, Palette.UiCream, "CoinPill", new Vector2(330, 92), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 46));
            MakeImage(coinPill.rectTransform, art.Coin, Color.white, "CoinIcon", new Vector2(58, 58), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(18, 0)).transform.localScale = Vector3.one;
            coinText = MakeText(coinPill.rectTransform, "0", new Vector2(240, 60), new Vector2(0.55f, 0.5f), Vector2.zero, 56);

            // ---- sound toggle (top right) ----
            MakeButton(canvasGO.transform, art.SoundOn, "SoundBtn", new Vector2(88, 88), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30, -34), OnSoundToggle);

            // ---- shop button (bottom right) ----
            var shopBtn = MakeButton(canvasGO.transform, art.BtnRect, "ShopBtn", new Vector2(270, 104), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36, 130), OpenShop);
            MakeText(shopBtn.transform, "Upgrades", new Vector2(260, 70), new Vector2(0.5f, 0.5f), Vector2.zero, 40);

            // ---- shop panel ----
            shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasGroup));
            var shopRT = shopPanel.GetComponent<RectTransform>();
            shopRT.SetParent(canvasGO.transform, false);
            shopRT.anchorMin = Vector2.zero;
            shopRT.anchorMax = Vector2.one;
            shopRT.offsetMin = shopRT.offsetMax = Vector2.zero;
            MakeImage(shopRT, art.PanelRect, Color.white, "PanelBg", new Vector2(640, 800), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            shopPanel.GetComponent<CanvasGroup>().alpha = 0f;
            shopPanel.SetActive(false);

            shopLevels = new Text[3];
            shopCosts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var type = (GameCfg.UpgradeType)i;
                float yPos = 230 - i * 235;
                var card = MakeImage(shopRT, art.BtnGreenRect, Color.white, GameCfg.UpgradeName(type), new Vector2(550, 175), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, yPos));
                MakeText(card.rectTransform, GameCfg.UpgradeName(type), new Vector2(520, 52), new Vector2(0.5f, 0.84f), Vector2.zero, 34);
                shopLevels[i] = MakeText(card.rectTransform, "Lv 0", new Vector2(520, 44), new Vector2(0.5f, 0.54f), Vector2.zero, 28);
                shopCosts[i] = MakeText(card.rectTransform, "", new Vector2(520, 44), new Vector2(0.5f, 0.18f), Vector2.zero, 28);
                MakeButton(shopRT, null, "BuyCard" + i, new Vector2(550, 175), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, yPos),
                    () => OnBuyUpgrade(type));
            }

            var closeBtn = MakeButton(shopRT, art.BtnRect, "CloseBtn", new Vector2(220, 84), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -372), CloseShop);
            MakeText(closeBtn.transform, "Close", new Vector2(210, 55), new Vector2(0.5f, 0.5f), Vector2.zero, 36);
        }

        void Update()
        {
            Tween.TickAll(Time.deltaTime);
            HandleInput();
            HandleAutoPeck();
            UpdateClouds();
        }

        void HandleInput()
        {
            if (shopPanel.activeSelf) return;
            bool tapped = false;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                tapped = !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            else if (Input.GetMouseButtonDown(0))
                tapped = !EventSystem.current.IsPointerOverGameObject();

            if (tapped) DoPeck();
        }

        void HandleAutoPeck()
        {
            if (GameManager.Inst.Data.autoLevel <= 0) return;
            autoTimer -= Time.deltaTime;
            if (autoTimer <= 0f)
            {
                autoTimer = GameManager.Inst.CurrentAutoInterval;
                DoPeck();
            }
        }

        void DoPeck()
        {
            GameManager.Inst.RecordPeck();
            int treeLevel = GameManager.Inst.TreeIndex;
            bool dead = treeBlock.Damage(GameManager.Inst.CurrentPeckPower);
            bird.PlayPeck();
            if (dead)
            {
                OnTreeCleared(treeLevel);
            }
            else
            {
                Sfx.Play(Sfx.Peck, 0.85f, Random.Range(0.92f, 1.08f));
                SpawnChips(peckHoleWorld, 6, 3.2f);
            }
        }

        void OnTreeCleared(int oldLevel)
        {
            GameManager.Inst.ClearTree();
            Sfx.Play(Sfx.Break, 1f);
            SpawnChips(peckHoleWorld, 26, 4.6f);
            int coins = Mathf.CeilToInt(GameCfg.CoinsForTree(oldLevel) * GameManager.Inst.CurrentCoinBoost);
            GameManager.Inst.AddCoins(coins);
            SpawnFloatingText("+" + coins, treeBlock.transform.position + new Vector3(0, 3.1f, 0));

            Vector3 home = treeBlock.transform.localScale;
            Tween.Float(0f, 1f, GameCfg.TreePopDuration, v =>
            {
                float e = Ease.InQuad(v);
                treeBlock.transform.localScale = Vector3.LerpUnclamped(home, Vector3.zero, e);
                treeBlock.transform.localRotation = Quaternion.Euler(0, 0, 20f * e);
            }, () =>
            {
                int next = GameManager.Inst.TreeIndex;
                treeBlock.SetLevel(next);
                treeBlock.transform.localRotation = Quaternion.identity;
                Vector3 start = new Vector3(treeScale * 0.25f, treeScale * 0.15f, 1);
                treeBlock.transform.localScale = start;
                Tween.Float(0f, 1f, 0.5f, v =>
                {
                    treeBlock.transform.localScale = Vector3.LerpUnclamped(start, Vector3.one * treeScale, Ease.OutBack(v));
                }, null);
            });
        }

        void SpawnChips(Vector3 worldPos, int count, float power)
        {
            var go = new GameObject("ChipBurst", typeof(ParticleSystem));
            go.transform.position = worldPos;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(power * 0.6f, power * 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.19f);
            main.maxParticles = 30;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 2.2f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = art.ParticleDot;
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 35;
            var colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Palette.Chip, 0), new GradientColorKey(Palette.ChipDark, 1) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            colorModule.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Play();
            Destroy(go, 1.2f);
        }

        void UpdateClouds()
        {
            float camW = Camera.main.orthographicSize * 2f * Camera.main.aspect;
            for (int i = 0; i < clouds.Length; i++)
            {
                var t = clouds[i];
                t.position += Vector3.right * (cloudSpeed * (1f + i * 0.18f) * Time.deltaTime);
                if (t.position.x > camW * 0.55f + 1.2f)
                    t.position = new Vector3(-camW * 0.55f - 1.2f, t.position.y, t.position.z);
            }
        }

        void SpawnFloatingText(string msg, Vector3 worldPos)
        {
            var canvasGO = new GameObject("FloatCanvas", typeof(Canvas), typeof(CanvasGroup));
            var wc = canvasGO.GetComponent<Canvas>();
            wc.renderMode = RenderMode.WorldSpace;
            wc.sortingOrder = 60;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(4f, 2f);
            canvasGO.transform.position = worldPos;
            canvasGO.transform.localScale = Vector3.one * 0.004f;

            var textGO = new GameObject("T", typeof(RectTransform), typeof(Text));
            var tRT = textGO.GetComponent<RectTransform>();
            tRT.SetParent(rt, false);
            tRT.anchorMin = tRT.anchorMax = Vector2.zero;
            tRT.pivot = new Vector2(0.5f, 0.5f);
            tRT.anchoredPosition = Vector2.zero;
            tRT.sizeDelta = new Vector2(600, 120);
            var txt = textGO.GetComponent<Text>();
            txt.font = uiFont;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 76;
            txt.color = Palette.Coin;
            txt.text = msg;

            var cg = canvasGO.GetComponent<CanvasGroup>();
            Tween.Float(0f, 1f, 0.85f, v =>
            {
                canvasGO.transform.position = worldPos + Vector3.up * (v * 2.6f);
                cg.alpha = 1f - v;
            }, () => Destroy(canvasGO, 0.1f));
        }

        // ---- UI actions ------------------------------------------------------

        void RefreshCoinUI()
        {
            if (coinText != null)
            {
                coinText.text = GameManager.Inst.Data.coins.ToString("N0");
            }
            RefreshShopUI();
        }

        void RefreshShopUI()
        {
            if (shopLevels == null) return;
            for (int i = 0; i < 3; i++)
            {
                var t = (GameCfg.UpgradeType)i;
                int lvl = GameManager.Inst.GetLevel(t);
                shopLevels[i].text = "Level " + lvl;
                shopCosts[i].text = (lvl >= GameCfg.MaxUpgradeLevel) ? "MAX" : GameCfg.UpgradeCost(t, lvl).ToString("N0") + " coins";
            }
        }

        void OnSoundToggle()
        {
            GameManager.Inst.ToggleSound();
            var btn = canvasFind("[UI Canvas]/SoundBtn");
            if (btn)
            {
                var img = btn.GetComponent<Image>();
                if (img) img.sprite = GameManager.Inst.SoundOn ? art.SoundOn : art.SoundOff;
            }
            Sfx.Play(Sfx.Pop, 0.6f);
        }

        Transform canvasFind(string path) => transform.Find(path);

        void OpenShop()
        {
            shopPanel.SetActive(true);
            var cg = shopPanel.GetComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            Tween.Float(0f, 1f, 0.26f, v =>
            {
                cg.alpha = v;
                shopPanel.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, Ease.OutCubic(v));
            }, () => { cg.interactable = true; cg.blocksRaycasts = true; });
            RefreshShopUI();
            Sfx.Play(Sfx.Pop, 0.5f);
        }

        void CloseShop()
        {
            var cg = shopPanel.GetComponent<CanvasGroup>();
            cg.interactable = false; cg.blocksRaycasts = false;
            Tween.Float(1f, 0f, 0.2f, v =>
            {
                cg.alpha = v;
                shopPanel.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, Ease.OutCubic(v));
            }, () => shopPanel.SetActive(false));
            Sfx.Play(Sfx.Pop, 0.5f);
        }

        void OnBuyUpgrade(GameCfg.UpgradeType type)
        {
            if (GameManager.Inst.BuyUpgrade(type))
            {
                Sfx.Play(Sfx.Buy, 0.9f);
                RefreshShopUI();
            }
        }

        // ---- uGUI builders ---------------------------------------------------

        static GameObject MakeSprite(Transform parent, Sprite spr, string name, Vector3 pos, int order, Vector3 scale)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = order;
            return go;
        }

        static Image MakeImage(Transform parent, Sprite spr, Color color, string name, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.color = color;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            return img;
        }

        Text MakeText(Transform parent, string txt, Vector2 size, Vector2 anchor, Vector2 offset, int fontSize)
        {
            var go = new GameObject("T", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var text = go.GetComponent<Text>();
            text.font = uiFont;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = Palette.UiBrown;
            text.text = txt;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static Button MakeButton(Transform parent, Sprite bg, string name, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 offset, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            if (bg != null)
            {
                img.sprite = bg;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0, 0, 0, 0.001f);
            }
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}