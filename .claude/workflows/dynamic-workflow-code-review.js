export const meta = {
  name: 'spacecleaner-full-audit',
  description: 'Whole-project audit of Space Cleaner: correctness, mobile perf, architecture, scene wiring',
  phases: [
    { title: 'Find', detail: '5 finders across correctness (x2), mobile perf, architecture, scene wiring' },
    { title: 'Verify', detail: 'adversarial refutation of top-ranked findings' },
    { title: 'Synthesize', detail: 'merge into a prioritized audit report' },
  ],
}

const REPO = 'C:/Users/yonga/SpaceCleaner'

const CONTEXT = `
PROJECT: Space Cleaner — dual-joystick mobile space shooter, Unity 6 (6000.3.10f1), URP 17.3.0, IL2CPP.
Player vacuums space trash and shoots it as projectiles. Movement is SPHERICAL — the player moves on the
surface of a planet (radius ~50), not on a flat plane. Primary target: Android ARM64, min SDK 25. iOS later.

Repo root: ${REPO}
Runtime code: Assets/_Project/Scripts/{Boss,Camera,Core,Enemies,Player,Progression,UI}/
Editor code:  Assets/_Project/Scripts/Editor/   (editor-only, judge it by different standards)
Scene:        Assets/_Project/Scenes/Gameplay/Gameplay.unity
Prefabs:      Assets/_Project/Prefabs/{Enemies,Planets,Player,Projectiles,Trash}/

Physics layers: Player(6), Enemy(7), Trash(8), Projectile(9), Planet(10)
Tags: Trash, PlayerShip, EnemyShip, Planet, Projectile

Input: New Input System ONLY (legacy Input is disabled, activeInputHandler=1). Any use of legacy
UnityEngine.Input is a REAL BUG that will throw at runtime.

44 C# files, ~8956 lines total.

RULES FOR YOUR REPORT:
- Read whole files, not excerpts. You have the budget; use it.
- Report ONLY concrete, actionable defects with a specific file and line. No vague advice
  ("consider adding tests", "could be more modular") — those are worthless here.
- For each finding you MUST give a concrete failure scenario: specific inputs/state -> wrong behavior.
- If you are not confident a thing is actually wrong, do not report it. Precision over recall.
- Severity: critical = crash/broken gameplay; high = wrong behavior or serious perf cliff on mobile;
  medium = real but bounded; low = polish.
`

const FINDINGS_SCHEMA = {
  type: 'object',
  required: ['findings'],
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        required: ['file', 'line', 'severity', 'category', 'summary', 'failure_scenario'],
        properties: {
          file: { type: 'string', description: 'repo-relative path' },
          line: { type: 'integer' },
          severity: { type: 'string', enum: ['critical', 'high', 'medium', 'low'] },
          category: { type: 'string' },
          summary: { type: 'string', description: 'one sentence statement of the defect' },
          failure_scenario: { type: 'string', description: 'concrete inputs/state -> wrong output' },
          suggested_fix: { type: 'string' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object',
  required: ['refuted', 'reasoning', 'confidence'],
  properties: {
    refuted: { type: 'boolean', description: 'true if the finding is wrong, already handled, or not reachable' },
    reasoning: { type: 'string' },
    confidence: { type: 'string', enum: ['high', 'medium', 'low'] },
    corrected_severity: { type: 'string', enum: ['critical', 'high', 'medium', 'low'] },
    notes: { type: 'string', description: 'anything the finder got right but described imprecisely' },
  },
}

const FINDERS = [
  {
    key: 'correctness-core',
    prompt: `${CONTEXT}

YOUR LENS: C# correctness bugs in the Core systems.

Read EVERY file in Assets/_Project/Scripts/Core/ (19 files: BuffHUD, BuffIcons, BuffManager, BuffPickup,
BuffReceiver, BuffType, GameManager, Health, LevelSetup, MobileManager, Moon, ObjectPool, PlanetEarth,
Projectile, SFXManager, SFXType, SpaceSkybox, TrashPickup, TrashSpawner).

Hunt specifically for:
- Null reference risks: unassigned [SerializeField] used without guard, GetComponent<T>() results used
  unchecked, singletons accessed before Awake ordering guarantees them.
- MonoBehaviour lifecycle errors: work in Awake that depends on another object's Awake; OnDestroy /
  OnDisable not unsubscribing from events (this leaks and causes "MissingReferenceException" on scene
  reload); coroutines started on disabled objects.
- Object pooling bugs in ObjectPool.cs and its consumers: state not reset on reuse, double-return,
  returning an object to the pool twice, pooled objects still subscribed to events.
- Buff system logic (BuffManager/BuffReceiver/BuffPickup/BuffHUD/BuffIcons — these are NEW, uncommitted
  code, scrutinize hardest): stacking two of the same buff, buff expiry while the pickup respawns,
  timers not cleared on player death, HUD desyncing from actual buff state.
- Timer/duration math, off-by-one, integer division, float equality comparison.
- Legacy UnityEngine.Input usage (guaranteed runtime failure — see rules above).

Return every real defect you find.`,
  },
  {
    key: 'correctness-gameplay',
    prompt: `${CONTEXT}

YOUR LENS: C# correctness bugs in player, boss, enemy, camera, and UI code.

Read EVERY file in:
  Assets/_Project/Scripts/Player/    (AimingCone, PlayerController, PlayerDeathHandler, ShootingSystem,
                                      SphericalMovement, VacuumCollector)
  Assets/_Project/Scripts/Boss/      (BossFightManager, CarryOverData, LarryBoss, LarryTrashBall)
  Assets/_Project/Scripts/Enemies/   (AIOpponent)
  Assets/_Project/Scripts/Camera/    (SphericalCamera)
  Assets/_Project/Scripts/UI/        (DeathOverlayUI, FireButton, GameplayHUD, OpponentBanner,
                                      RadarMinimap, SafeAreaHandler, VirtualJoystick)

Hunt specifically for:
- SPHERICAL MOVEMENT MATH. This is the highest-risk area. The player moves on a sphere of radius ~50.
  Look for: normalization missing before use, gimbal/pole singularities (what happens at the exact
  north/south pole of the planet?), quaternion vs euler mistakes, up-vector drift accumulating over
  time, camera flipping when crossing the pole, projectiles that travel straight instead of along
  the surface, Vector3.Angle vs signed angle confusion.
- AI state machines in AIOpponent and LarryBoss: states with no exit transition (a freeze/deadlock),
  target lost mid-state, NaN propagating into a rotation and freezing the object permanently.
- Input handling: New Input System action lifecycle (Enable/Disable), touch handling in VirtualJoystick
  and FireButton, multi-touch conflicts (can you drag the joystick and fire simultaneously?).
- Death/respawn: PlayerDeathHandler and the boss fight handoff via CarryOverData — state that survives
  a scene load when it shouldn't, or is lost when it should survive.
- Legacy UnityEngine.Input usage (guaranteed runtime failure).

Return every real defect you find.`,
  },
  {
    key: 'mobile-perf',
    prompt: `${CONTEXT}

YOUR LENS: mobile (Android, low-end ARM64) performance. Be concrete and quantitative where you can.

Read all runtime scripts under Assets/_Project/Scripts/ EXCEPT the Editor/ folder.

Hunt specifically for:
- PER-FRAME HEAP ALLOCATION in Update/FixedUpdate/LateUpdate. On IL2CPP+Android every allocation feeds
  the GC and causes frame hitches. Look for: string concatenation or interpolation (especially in HUD /
  score / RadarMinimap label updates), .ToString() on numbers, LINQ (.Where/.Select/.OrderBy/.First),
  new List<>/new array/new Vector3[] per frame, lambdas capturing locals, foreach over a struct-boxing
  collection, params arrays.
- Physics.OverlapSphere / RaycastAll / SphereCastAll allocating a new array every call — should be the
  NonAlloc variants with a reused buffer. VacuumCollector and ShootingSystem are prime suspects.
- GetComponent / FindObjectOfType / GameObject.Find / Camera.main called in Update rather than cached
  in Awake. Camera.main in particular does a tag search.
- Missing layer masks on physics queries — querying all layers then filtering in C# is far more
  expensive than letting the physics engine cull. Layers are Player(6) Enemy(7) Trash(8) Projectile(9)
  Planet(10).
- O(n^2) scans: every trash object checking every other, spawners iterating all objects each frame.
  TrashSpawner and RadarMinimap are suspects.
- UI: Canvas rebuild storms (changing any element on a Canvas dirties the whole canvas — is the
  frequently-updating HUD on its own canvas?), unbatched UI text updated every frame instead of on change.
- Instantiate/Destroy churn where the existing ObjectPool should be used instead.
- Update() methods that exist but do nothing or could be event-driven (each empty Update still costs a
  native->managed call per object per frame; with many trash objects this adds up).

For each finding state WHY it costs on mobile specifically and roughly how often it fires.`,
  },
  {
    key: 'architecture',
    prompt: `${CONTEXT}

YOUR LENS: architecture, design, and project hygiene. Be rigorous but concrete — no generic advice.

Read broadly across all 44 files (skim for structure, read deeply where something looks wrong), plus:
  Assets/_Project/Scripts/SpaceCleaner.asmdef
  Assets/_Project/Scripts/Editor/SpaceCleaner.Editor.asmdef
  Assets/_Project/Tests/EditMode/SpaceCleaner.Tests.EditMode.asmdef
  CLAUDE.md and docs/CLAUDE-structure.md

Hunt specifically for:
- The Core/ folder holds 19 of 44 scripts. Determine whether it has become a catch-all dumping ground,
  and if so name the specific files that belong elsewhere and where they should go.
- Namespace consistency: CLAUDE.md mandates SpaceCleaner.{Domain} matching the folder. Find every file
  whose namespace does not match its folder, and any file missing a namespace entirely.
- Assembly definition correctness: are references right, does Editor code leak into the runtime assembly
  (would break IL2CPP player builds), does the runtime assembly reference anything editor-only?
  Check for UnityEditor usage outside the Editor folder NOT guarded by #if UNITY_EDITOR — this breaks
  the Android build and is a critical finding.
- Singleton / static state abuse: what breaks on scene reload, what makes things untestable, hidden
  cross-system coupling via statics.
- The Tests/EditMode asmdef exists — determine whether ANY tests actually exist. If the test assembly is
  empty, say so plainly and identify the 2-3 highest-value pure-logic targets that could be tested
  without a scene (e.g. buff duration math, spherical coordinate conversion).
- Doc/code drift: CLAUDE.md and docs/CLAUDE-structure.md claim scenes exist at Scenes/{Gameplay,MainMenu,
  BossFight}/ and a Scripts/Progression/ folder. Verify what actually exists on disk and report each
  specific false claim in the docs.
- Duplicated logic: the same computation implemented in two places that can drift apart.

Return concrete, located findings.`,
  },
  {
    key: 'scene-wiring',
    prompt: `${CONTEXT}

YOUR LENS: Unity scene and prefab wiring. These are text-serialized YAML — read them directly.

Read:
  Assets/_Project/Scenes/Gameplay/Gameplay.unity
  Assets/_Project/Prefabs/Player/PlayerShip.prefab
  Assets/_Project/Prefabs/Enemies/AIShip.prefab
  Assets/_Project/Prefabs/Projectiles/Projectile.prefab
  Assets/_Project/Prefabs/Planets/Planet.prefab
  Assets/_Project/Prefabs/Trash/Trash_A.prefab, Trash_B.prefab, Trash_C.prefab

Then cross-reference against the C# scripts that are attached to those objects.

METHOD: for each MonoBehaviour in the YAML, resolve its script GUID to the .cs file (the GUID is in the
matching .cs.meta file). Then compare every [SerializeField] / public field declared in that script
against the values serialized in the YAML.

Hunt specifically for:
- UNASSIGNED OBJECT REFERENCES: a serialized field of an object type with value {fileID: 0}. This is a
  guaranteed NullReferenceException at runtime if the script uses it without a null guard. Cross-check
  against the script to see whether it IS guarded. This is the single highest-value thing you can find.
- Layer and tag mismatches: an object whose layer contradicts what the code expects (Player=6, Enemy=7,
  Trash=8, Projectile=9, Planet=10), or a tag the code compares against that is misspelled or absent.
  Check that trash prefabs are actually on layer 8, projectiles on 9, etc.
- Collider/Rigidbody setup: a trigger-based pickup with no trigger collider; a Rigidbody-less object that
  code tries to AddForce to; isKinematic contradicting how code moves it; missing collider on something
  raycast against; continuous vs discrete collision on fast projectiles (tunneling through thin colliders).
- Serialized values that contradict code assumptions or look wrong given planet radius ~50 (speeds,
  ranges, spawn distances, vacuum radius, orbit distances).
- Missing components a script requires (check for [RequireComponent] and whether it is satisfied).
- Duplicate singletons: two objects in the scene both carrying the same manager script.

Report each with the exact object name/path in the scene and the field involved.`,
  },
]

// ---- Phase 1: find (barrier is justified — dedupe across overlapping lenses before paying for verify)
phase('Find')
log(`Auditing 44 scripts / ~8956 lines across 5 lenses...`)

const raw = await parallel(
  FINDERS.map(f => () => agent(f.prompt, { label: `find:${f.key}`, phase: 'Find', schema: FINDINGS_SCHEMA }))
)

const all = []
raw.filter(Boolean).forEach((r, i) => {
  ;(r.findings || []).forEach(f => all.push({ ...f, lens: FINDERS[i].key }))
})
log(`${all.length} raw findings from ${raw.filter(Boolean).length}/5 finders`)

// dedupe: same file + nearby line + similar summary => keep the more severe
const RANK = { critical: 0, high: 1, medium: 2, low: 3 }
const byKey = new Map()
for (const f of all) {
  const key = `${(f.file || '').toLowerCase()}:${Math.floor((f.line || 0) / 8)}`
  const prev = byKey.get(key)
  if (!prev) { byKey.set(key, { ...f, lenses: [f.lens] }) }
  else {
    prev.lenses.push(f.lens)
    if (RANK[f.severity] < RANK[prev.severity]) {
      byKey.set(key, { ...f, lenses: prev.lenses })
    }
  }
}
const deduped = [...byKey.values()].sort((a, b) => RANK[a.severity] - RANK[b.severity])
log(`${deduped.length} findings after dedupe`)

// verify budget: the most severe findings get adversarial scrutiny
const VERIFY_CAP = 8
const toVerify = deduped.slice(0, VERIFY_CAP)
const unverified = deduped.slice(VERIFY_CAP)
if (unverified.length) {
  log(`Verifying top ${toVerify.length}; ${unverified.length} lower-severity findings pass through UNVERIFIED and will be labeled as such`)
}

// ---- Phase 2: adversarial verify (prompted to refute, not confirm)
phase('Verify')
const verdicts = await parallel(
  toVerify.map(f => () =>
    agent(`${CONTEXT}

You are an ADVERSARIAL VERIFIER. Another agent reported the finding below. Your job is to REFUTE it.
Assume it is wrong until the code proves otherwise. Reviewers routinely produce plausible-sounding
findings that evaporate on inspection — your job is to catch those.

FINDING
  file:     ${f.file}:${f.line}
  severity: ${f.severity}
  category: ${f.category}
  claim:    ${f.summary}
  scenario: ${f.failure_scenario}

METHOD (do all of it):
1. Read the ENTIRE file, not just the cited line.
2. Read the callers — grep for who invokes this code. A "bug" on an unreachable path is not a bug.
3. Check whether something elsewhere already prevents it: a null guard, [RequireComponent], an
   inspector value set in the scene/prefab YAML, execution-order attribute, or an early return.
4. Check whether Unity's own semantics make it a non-issue (e.g. Unity's fake-null on destroyed objects,
   default struct values, Awake/Start ordering that is actually guaranteed here).
5. Only if you cannot refute it: confirm it, and set corrected_severity to what the evidence supports —
   downgrade freely if the real-world impact is smaller than claimed.

Set refuted=true if the finding is wrong, unreachable, already handled, or purely stylistic.
When genuinely uncertain, lean toward refuted=true. A false positive costs the user more than a miss.`,
      { label: `refute:${(f.file || '').split('/').pop()}:${f.line}`, phase: 'Verify', schema: VERDICT_SCHEMA }
    ).then(v => ({ finding: f, verdict: v }))
  )
)

const checked = verdicts.filter(Boolean)
const confirmed = checked.filter(x => x.verdict && !x.verdict.refuted)
  .map(x => ({ ...x.finding, severity: x.verdict.corrected_severity || x.finding.severity, verified: true, verifier_notes: x.verdict.notes }))
const killed = checked.filter(x => x.verdict && x.verdict.refuted)
log(`Verify: ${confirmed.length} confirmed, ${killed.length} refuted and dropped`)

const final = [...confirmed, ...unverified.map(f => ({ ...f, verified: false }))]
  .sort((a, b) => RANK[a.severity] - RANK[b.severity])

// ---- Phase 3: synthesize
phase('Synthesize')
const report = await agent(`${CONTEXT}

You are writing the final audit report for the Space Cleaner project owner.

VERIFIED + PASS-THROUGH FINDINGS (JSON):
${JSON.stringify(final, null, 2)}

REFUTED (dropped — do NOT include these, listed only so you know what was already ruled out):
${JSON.stringify(killed.map(x => ({ file: x.finding.file, line: x.finding.line, claim: x.finding.summary, why_refuted: x.verdict.reasoning })), null, 2)}

Write a markdown report with these sections:

1. **Verdict** — 3-4 sentences on the overall health of the codebase. Be honest and direct. If it is in
   good shape, say so; if there is systemic rot, say that. No hedging, no flattery.
2. **Critical & High** — each finding as: file:line, what breaks, the concrete failure scenario, and the
   fix. Order by severity then by how cheap the fix is.
3. **Medium & Low** — terser, grouped by theme.
4. **Systemic patterns** — recurring problems that appear across many files. This is the most valuable
   section: a pattern repeated 10 times matters more than any single bug. Name the pattern, count the
   occurrences, give the general fix.
5. **Recommended fix order** — a concrete numbered sequence, cheapest-highest-impact first. Mark which
   items are safe mechanical fixes vs. which need design decisions from the owner.

Mark any finding with verified=false as "(unverified)" so the reader knows it did not survive an
adversarial pass. Do not invent findings not present in the data above. Keep it tight — the reader is
the developer who wrote this code and wants to act on it, not read prose.`,
  { label: 'synthesize', phase: 'Synthesize' }
)

return {
  report,
  stats: {
    raw: all.length,
    deduped: deduped.length,
    verified: checked.length,
    confirmed: confirmed.length,
    refuted: killed.length,
    passed_through_unverified: unverified.length,
  },
  findings: final,
}
