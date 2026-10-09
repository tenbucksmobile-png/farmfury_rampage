using FarmFuryRampage.Data;
using FarmFuryRampage.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryRampage.Run
{
    /// <summary>
    /// Phase 1 greybox entry point: builds a <see cref="RunSim"/> from the level assets, steps it at a fixed tick,
    /// and drives the placeholder view and HUD. Replaced by the RunDirector / services flow in later phases.
    /// </summary>
    public sealed class RunBootstrap : MonoBehaviour
    {
        /// <summary>Longest frame the sim will catch up on, so a hitch doesn't fast-forward the run.</summary>
        const float MaxFrameCatchUp = 0.25f;

        [SerializeField] GameTuning tuning;
        [SerializeField] HeroDef hero;
        [SerializeField, Tooltip("Shared run art; optional, greybox shapes are used for anything missing.")] RunArt art;
        [SerializeField] LevelDef[] levels;
        [SerializeField] int startLevel;

        RunSim sim;
        RunInputReader input;
        RunView view;
        GreyboxHud hud;
        int levelIndex;
        float accumulator;

        void Start()
        {
            if (tuning == null || hero == null || levels == null || levels.Length == 0)
            {
                Debug.LogError("[RunBootstrap] Tuning, hero or levels not assigned. Run FarmFury Rampage > Setup > Run All.");
                enabled = false;
                return;
            }

            Application.targetFrameRate = tuning.tickRate;
            view = gameObject.AddComponent<RunView>();
            hud = gameObject.AddComponent<GreyboxHud>();
            input = new RunInputReader(tuning.track.width, tuning.herd.dragSensitivity);
            StartLevel(Mathf.Clamp(startLevel, 0, levels.Length - 1));
        }

        void StartLevel(int index)
        {
            levelIndex = index;
            RunConfig config = RunConfigFactory.Create(tuning, hero, levels[index], out RobotDef[] robotTypes);
            sim = new RunSim(config);
            view.Bind(sim, robotTypes, hero, art);
            input.Reset();
            accumulator = 0f;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) StartLevel(levelIndex);
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame) StartLevel((levelIndex + 1) % levels.Length);

            if (sim.Phase == RunPhase.Running)
            {
                input.Update(sim);
                accumulator += Mathf.Min(Time.deltaTime, MaxFrameCatchUp);
                float step = sim.Config.TickSeconds;
                var tickInput = new RunInput { targetX = input.TargetX };
                while (accumulator >= step && sim.Phase == RunPhase.Running)
                {
                    sim.Tick(tickInput);
                    view.OnEvents(sim.Events);
                    accumulator -= step;
                }
            }
            else if (RunInputReader.ConfirmPressed())
            {
                StartLevel(sim.Phase == RunPhase.Won ? (levelIndex + 1) % levels.Length : levelIndex);
            }

            hud.Render(sim, levels[levelIndex].displayName);
        }
    }
}
