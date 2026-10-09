using UnityEngine;

namespace SpaceCleaner.Player
{
    /// <summary>
    /// Purely cosmetic animation for the sv12 / xr GLB ship models. Looks up named
    /// child nodes (intake fan, exhaust glows, alien halo) and animates them.
    /// Missing nodes are skipped, so it is safe on any model.
    /// </summary>
    public class ShipDetailAnimator : MonoBehaviour
    {
        [SerializeField] private float fanSpinSpeed = 720f;
        [SerializeField] private float haloSpinSpeed = 80f;
        [SerializeField] private float exhaustBase = 0.6f;
        [SerializeField] private float exhaustSpeedGain = 0.08f;
        [SerializeField] private float exhaustFlicker = 0.08f;

        private Transform fan;
        private Transform halo;
        private Transform[] exhausts;
        private Vector3[] exhaustBaseScale;
        private Vector3 lastPos;

        private void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "intake_fan": fan = t; break;
                    case "alien_halo": halo = t; break;
                }
            }

            var list = new System.Collections.Generic.List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "exhaust_glow") list.Add(t);
            exhausts = list.ToArray();
            exhaustBaseScale = new Vector3[exhausts.Length];
            for (int i = 0; i < exhausts.Length; i++)
                exhaustBaseScale[i] = exhausts[i].localScale;

            lastPos = transform.position;
        }

        private void OnEnable() => lastPos = transform.position;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (fan != null) fan.Rotate(0f, 0f, fanSpinSpeed * dt, Space.Self);
            if (halo != null) halo.Rotate(0f, 0f, haloSpinSpeed * dt, Space.Self);

            if (exhausts.Length == 0 || dt <= 0f) return;

            float speed = (transform.position - lastPos).magnitude / dt;
            lastPos = transform.position;
            float len = exhaustBase + speed * exhaustSpeedGain + Random.Range(-exhaustFlicker, exhaustFlicker);
            for (int i = 0; i < exhausts.Length; i++)
            {
                var s = exhaustBaseScale[i];
                exhausts[i].localScale = new Vector3(s.x, s.y, s.z * Mathf.Max(0.2f, len));
            }
        }
    }
}
