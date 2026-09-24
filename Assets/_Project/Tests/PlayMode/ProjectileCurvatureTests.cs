using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SpaceCleaner.Core;

namespace SpaceCleaner.Tests
{
    /// <summary>
    /// Both ships are pinned to a shell of radius 52 (planet 50 + hover 2). A shot fired
    /// along the tangent in a straight line climbs d^2/(2R) above that shell, so it clears
    /// the enemy capsule (collider radius 0.5) after roughly 9 units of travel — the
    /// "my shots go over their head even though I'm close" bug.
    /// </summary>
    public class ProjectileCurvatureTests
    {
        private const float PlanetRadius = 50f;
        private const float HoverHeight = 2f;
        private const float ShellRadius = PlanetRadius + HoverHeight;
        private const float Speed = 8f;

        private static readonly Vector3 PlanetCenter = new Vector3(0f, 0f, 0f);

        private GameObject projectileGO;

        private Projectile SpawnTangentShot()
        {
            projectileGO = new GameObject("TestProjectile");
            projectileGO.transform.position = PlanetCenter + Vector3.up * ShellRadius;

            var col = projectileGO.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.234f;

            var rb = projectileGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;

            var projectile = projectileGO.AddComponent<Projectile>();
            rb.linearVelocity = Vector3.forward * Speed; // tangent at the north pole
            return projectile;
        }

        [TearDown]
        public void TearDown()
        {
            if (projectileGO != null) Object.DestroyImmediate(projectileGO);
        }

        [UnityTest]
        public IEnumerator StraightShot_ClimbsOffTheShell_AndMissesOverhead()
        {
            SpawnTangentShot(); // no SetPlanetCenter — the old, uncorrected behaviour

            // Fly far enough to cover ~10 units of ground: 10 / 8 = 1.25s
            for (int i = 0; i < 70; i++) yield return new WaitForFixedUpdate();

            float altitude = Vector3.Distance(projectileGO.transform.position, PlanetCenter) - ShellRadius;
            Assert.Greater(altitude, 0.73f,
                "Baseline: an uncorrected tangent shot should sail above the combined " +
                "projectile+enemy collider tolerance. This is the bug being fixed.");
        }

        [UnityTest]
        public IEnumerator CurvedShot_HoldsItsAltitude_OverTheWholeFlight()
        {
            var projectile = SpawnTangentShot();
            projectile.SetPlanetCenter(PlanetCenter);

            for (int i = 0; i < 70; i++)
            {
                yield return new WaitForFixedUpdate();

                float altitude = Vector3.Distance(projectileGO.transform.position, PlanetCenter) - ShellRadius;
                Assert.LessOrEqual(Mathf.Abs(altitude), 0.01f,
                    $"Shot drifted off the shell at step {i} (altitude {altitude}).");
            }
        }

        [UnityTest]
        public IEnumerator CurvedShot_KeepsItsSpeed_AndSweepsTheExpectedArc()
        {
            var projectile = SpawnTangentShot();
            projectile.SetPlanetCenter(PlanetCenter);

            Vector3 start = projectileGO.transform.position;
            float elapsed = 0f;
            for (int i = 0; i < 70; i++)
            {
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            var rb = projectileGO.GetComponent<Rigidbody>();
            Assert.AreEqual(Speed, rb.linearVelocity.magnitude, 0.01f, "Curving must not bleed speed.");

            // Great-circle arc length should match distance the shot would have travelled.
            float sweptAngle = Vector3.Angle(start - PlanetCenter, projectileGO.transform.position - PlanetCenter);
            float arcLength = sweptAngle * Mathf.Deg2Rad * ShellRadius;
            Assert.AreEqual(Speed * elapsed, arcLength, 0.5f,
                "Ground distance covered should still equal speed x time.");
        }

        [UnityTest]
        public IEnumerator CurvedShot_StaysInRangeOfATargetOnTheShell()
        {
            var projectile = SpawnTangentShot();
            projectile.SetPlanetCenter(PlanetCenter);

            // A target sitting on the shell ~10 units of arc ahead — well inside the range
            // where the uncorrected shot flew overhead.
            float arc = 10f / ShellRadius;
            Vector3 target = PlanetCenter + new Vector3(0f, Mathf.Cos(arc), Mathf.Sin(arc)) * ShellRadius;

            float closest = float.MaxValue;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                closest = Mathf.Min(closest, Vector3.Distance(projectileGO.transform.position, target));
            }

            Assert.Less(closest, 0.73f,
                "Curved shot should pass within the projectile+enemy collider tolerance of a target 10 units away.");
        }
    }
}
