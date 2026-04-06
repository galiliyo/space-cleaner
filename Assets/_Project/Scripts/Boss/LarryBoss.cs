using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

namespace SpaceCleaner.Boss
{
    [RequireComponent(typeof(Health))]
    public class LarryBoss : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] private float fireRate = 3f;
        [SerializeField] private float trashBallSpeed = 20f;
        [SerializeField] private int poolSize = 8;

        [Header("Taunts")]
        [SerializeField] private float tauntMinInterval = 8f;
        [SerializeField] private float tauntMaxInterval = 12f;
        [SerializeField] private float tauntDisplayTime = 3f;
        [SerializeField] private string[] tauntLines = new[]
        {
            "You call that cleaning?!",
            "My minions will take out the trash\u2014and by trash, I mean YOU!",
            "This galaxy was boring when it was clean!",
            "Give up already!",
            "You will never beat me!"
        };

        public event Action OnDefeated;

        private Health health;
        private Transform playerTransform;
        private float fireTimer;
        private float tauntTimer;
        private ObjectPool trashBallPool;
        private GameObject trashBallTemplate;

        private Canvas speechCanvas;
        private TextMeshProUGUI speechText;
        private Coroutine hideSpeechCoroutine;

        public Health Health => health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.OnDeath += HandleDeath;
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                playerTransform = player.transform;

            CreateTrashBallPool();
            CreateSpeechBubble();

            fireTimer = fireRate;
            tauntTimer = UnityEngine.Random.Range(tauntMinInterval, tauntMaxInterval);

            foreach (var r in GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (health.IsDead || playerTransform == null) return;

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                FireTrashBall();
                fireTimer = fireRate;
            }

            tauntTimer -= Time.deltaTime;
            if (tauntTimer <= 0f)
            {
                ShowRandomTaunt();
                tauntTimer = UnityEngine.Random.Range(tauntMinInterval, tauntMaxInterval);
            }
        }

        private void FireTrashBall()
        {
            if (trashBallPool == null || playerTransform == null) return;

            Vector3 firePos = transform.position + transform.up * 3f;
            Vector3 dir = (playerTransform.position - firePos).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            GameObject ball = trashBallPool.Get(firePos, rot);

            var trashBall = ball.GetComponent<LarryTrashBall>();
            if (trashBall != null)
                trashBall.SetShooterLayer(gameObject.layer);

            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = dir * trashBallSpeed;

            SFXManager.Instance?.Play(SFXType.AIShoot);
        }

        private void ShowRandomTaunt()
        {
            if (tauntLines == null || tauntLines.Length == 0) return;
            string line = tauntLines[UnityEngine.Random.Range(0, tauntLines.Length)];

            if (speechText != null)
            {
                speechText.text = line;
                speechCanvas.gameObject.SetActive(true);

                if (hideSpeechCoroutine != null)
                    StopCoroutine(hideSpeechCoroutine);
                hideSpeechCoroutine = StartCoroutine(HideSpeechAfterDelay());
            }
        }

        private IEnumerator HideSpeechAfterDelay()
        {
            yield return new WaitForSeconds(tauntDisplayTime);
            if (speechCanvas != null)
                speechCanvas.gameObject.SetActive(false);
        }

        private void HandleDeath()
        {
            OnDefeated?.Invoke();
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            SFXManager.Instance?.Play(SFXType.AIDeath);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void CreateTrashBallPool()
        {
            trashBallTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trashBallTemplate.name = "LarryTrashBall_Template";
            trashBallTemplate.layer = 9;
            trashBallTemplate.transform.localScale = Vector3.one * 1.5f;

            var rb = trashBallTemplate.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var col = trashBallTemplate.GetComponent<SphereCollider>();
            col.isTrigger = true;

            trashBallTemplate.AddComponent<LarryTrashBall>();

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color baseColor = new Color(0.3f, 1f, 0.2f, 1f);
                mat.SetColor("_BaseColor", baseColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", baseColor * 2.5f);
                var mr = trashBallTemplate.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            var trail = trashBallTemplate.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.6f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var trailShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (trailShader != null)
            {
                var trailMat = new Material(trailShader);
                trailMat.SetColor("_BaseColor", new Color(0.4f, 1f, 0.3f, 1f));
                trailMat.SetFloat("_Surface", 1f);
                trailMat.SetFloat("_Blend", 1f);
                trailMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                trailMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                trailMat.SetFloat("_ZWrite", 0f);
                trailMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                trailMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                trail.sharedMaterial = trailMat;
            }

            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.4f, 1f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.6f, 0.1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = colorGrad;

            trashBallTemplate.SetActive(false);

            var poolGO = new GameObject("LarryTrashBallPool");
            poolGO.SetActive(false);
            poolGO.transform.SetParent(transform);

            trashBallPool = poolGO.AddComponent<ObjectPool>();
            trashBallPool.InitializeRuntime(trashBallTemplate, poolSize, poolGO.transform);
            poolGO.SetActive(true);
        }

        private void CreateSpeechBubble()
        {
            var canvasGO = new GameObject("SpeechBubble");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = Vector3.up * 5f;

            speechCanvas = canvasGO.AddComponent<Canvas>();
            speechCanvas.renderMode = RenderMode.WorldSpace;
            speechCanvas.sortingOrder = 10;

            var canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(6f, 2f);
            canvasRT.localScale = Vector3.one * 0.5f;

            var bgGO = new GameObject("Background");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(canvasRT, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            var textGO = new GameObject("TauntText");
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(canvasRT, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(0.2f, 0.1f);
            textRT.offsetMax = new Vector2(-0.2f, -0.1f);

            speechText = textGO.AddComponent<TextMeshProUGUI>();
            speechText.fontSize = 1.2f;
            speechText.color = new Color(1f, 0.9f, 0.3f, 1f);
            speechText.alignment = TextAlignmentOptions.Center;
            speechText.fontStyle = FontStyles.Bold;
            speechText.enableWordWrapping = true;

            canvasGO.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }
    }
}
