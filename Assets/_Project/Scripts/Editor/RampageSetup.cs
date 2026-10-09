using System.Collections.Generic;
using System.IO;
using FarmFuryRampage.Data;
using FarmFuryRampage.Run;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FarmFuryRampage.Editor
{
    /// <summary>
    /// Phase 1 project setup, runnable from the menu or headless:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod FarmFuryRampage.Editor.RampageSetup.RunAllBatch
    /// Content assets are only created when missing, so tuning done in the Inspector is never overwritten.
    /// </summary>
    public static class RampageSetup
    {
        const string Root = "Assets/_Project";
        const string ContentRoot = Root + "/ScriptableObjects";
        const string ScenePath = Root + "/Scenes/Run.unity";
        const string BundleId = "com.farmfury.rampage";
        const string ProductName = "FarmFury Rampage";
        const string CompanyName = "FarmFury Games";
        static readonly Color SkyColor = new(0.53f, 0.78f, 0.92f);
        /// <summary>Metres between robots in a horde: shoulder to shoulder for a Bolt Walker.</summary>
        const float HordeSpacing = 0.95f;

        [MenuItem("FarmFury Rampage/Setup/Run All")]
        public static void RunAll()
        {
            ApplyProjectSettings();
            CreatePrototypeContent();
            BuildRunScene(false);
            AssetDatabase.SaveAssets();
            Debug.Log("[RampageSetup] Done.");
        }

        public static void RunAllBatch()
        {
            RunAll();
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Rewrites the prototype hero, robots and levels with the values in this script (GameTuning is left alone).
        /// Assets are updated in place, so scene references survive.
        /// </summary>
        [MenuItem("FarmFury Rampage/Setup/Reset Prototype Content (overwrites hero, robots, levels)")]
        public static void ResetPrototypeContent()
        {
            CreateContent(true);
            BuildRunScene(false);
        }

        public static void ResetPrototypeContentBatch()
        {
            ResetPrototypeContent();
            EditorApplication.Exit(0);
        }

        [MenuItem("FarmFury Rampage/Setup/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("[RampageSetup] Project settings applied.");
        }

        [MenuItem("FarmFury Rampage/Setup/Create Prototype Content")]
        public static void CreatePrototypeContent() => CreateContent(false);

        static void CreateContent(bool overwrite)
        {
            EnsureFolder(ContentRoot + "/Tuning");
            EnsureFolder(ContentRoot + "/Heroes");
            EnsureFolder(ContentRoot + "/Robots");
            EnsureFolder(ContentRoot + "/Levels");

            CreateOrUpdate<GameTuning>(ContentRoot + "/Tuning/GameTuning.asset", false, t =>
            {
                t.tickRate = 60;
                t.track = new TrackTuning
                {
                    width = 9f, pace = 2.5f, viewWidth = 10f, herdScreenY = 0.3f, spawnAhead = 16f, despawnBehind = 8f,
                };
                t.herd = new HerdTuning
                {
                    cap = 300, drawnCap = 60, slotSpacing = 0.28f, steerSpeed = 8f, dragSensitivity = 1f,
                    steadySteerMultiplier = 0.5f,
                };
                t.gates = new GateTuning { hitsPerStep = 2, maxImprove = 5, hitCooldown = 0.15f, panelDepth = 0.6f };
                t.combat = new CombatTuning
                {
                    hpScalePerLevel = 0.12f, projectileCap = 400, heroDpsPerAnimal = 10f, heroBalanceTolerance = 0.05f,
                };
            });

            // Cluck lobs egg grenades: one every 1.2 s per animal, 12 damage to every robot in a 1.5 m blast.
            // Single-target DPS stays at the 10/animal balance target; the blast is what clears hordes.
            HeroDef cluck = CreateOrUpdate<HeroDef>(ContentRoot + "/Heroes/Hero_Cluck.asset", overwrite, h =>
            {
                h.id = "cluck";
                h.displayName = "Cluck";
                h.greyboxColor = new Color(1f, 0.92f, 0.55f);
                h.stats = new HeroStats
                {
                    pattern = AttackPattern.Lob, fireInterval = 1.2f, projectilesPerShot = 1, damagePerHit = 12f,
                    range = 11f, blastRadius = 1.5f, flightTime = 0.7f, scatter = 0.6f, arcHeight = 2.5f,
                };
            });

            RobotDef drone = CreateOrUpdate<RobotDef>(ContentRoot + "/Robots/Robot_BuzzDrone.asset", overwrite, r =>
            {
                r.id = "buzz_drone";
                r.displayName = "Buzz Drone";
                r.tier = 1;
                r.greyboxColor = new Color(0.82f, 0.84f, 0.88f);
                r.stats = new RobotStats { hp = 20f, speed = 3f, bite = 1, radius = 0.3f, homing = 0.6f, scrap = 1 };
            });

            // The horde grunt: slow, packed shoulder to shoulder, rolls straight at the herd. Low HP so an egg
            // blast demolishes a chunk of the horde (GDD's 60 HP was for single shots).
            RobotDef walker = CreateOrUpdate<RobotDef>(ContentRoot + "/Robots/Robot_BoltWalker.asset", overwrite, r =>
            {
                r.id = "bolt_walker";
                r.displayName = "Bolt Walker";
                r.tier = 1;
                r.greyboxColor = new Color(0.45f, 0.48f, 0.55f);
                r.stats = new RobotStats { hp = 20f, speed = 1.2f, bite = 2, radius = 0.42f, homing = 0f, scrap = 2 };
            });

            CreateOrUpdate<LevelDef>(ContentRoot + "/Levels/Level_Proto01.asset", overwrite, l =>
            {
                Level(l, "proto_01", "Proto 1 · First Horde", 180f, 1, 5, 101);
                Row(l, 18f, Mul(2), Add(3));
                Row(l, 45f, Add(5), Sub(3));
                Row(l, 75f, Mul(2), Add(8));
                Row(l, 105f, Mul(3), Add(10));
                Row(l, 135f, Sub(5), Add(5), Mul(2));
                Row(l, 162f, Add(10), Sub(20));
                Horde(l, 28f, walker, 0f, 3, 4);
                Wave(l, 55f, drone, 0f, 6, 0.8f, 2.5f);
                Horde(l, 62f, walker, -1.5f, 4, 4);
                Horde(l, 88f, walker, 1f, 4, 6);
                Wave(l, 118f, drone, 0f, 10, 0.6f, 3.5f);
                Horde(l, 125f, walker, 0f, 6, 7);
                Horde(l, 150f, walker, 0f, 6, 8);
            });

            CreateOrUpdate<LevelDef>(ContentRoot + "/Levels/Level_Proto02.asset", overwrite, l =>
            {
                Level(l, "proto_02", "Proto 2 · Shoot the Reds", 210f, 2, 5, 202);
                Row(l, 20f, Sub(3), Add(2));
                Row(l, 45f, Mul(2), Sub(5));
                Row(l, 75f, Sub(8), Add(4));
                Row(l, 105f, Div(2), Mul(2));
                Row(l, 140f, Sub(6), Add(6), Sub(2));
                Row(l, 175f, Mul(2), Add(15));
                Horde(l, 30f, walker, 0f, 3, 5);
                Wave(l, 58f, drone, -1f, 8, 0.7f, 3f);
                Horde(l, 66f, walker, -1f, 5, 5);
                Horde(l, 92f, walker, 0f, 5, 7);
                Wave(l, 122f, drone, 0f, 12, 0.5f, 3.5f);
                Horde(l, 130f, walker, 0f, 7, 7);
                Horde(l, 160f, walker, 0f, 8, 8);
                Horde(l, 188f, walker, 0f, 8, 9);
            });

            CreateOrUpdate<LevelDef>(ContentRoot + "/Levels/Level_Proto03.asset", overwrite, l =>
            {
                Level(l, "proto_03", "Proto 3 · The Swarm", 240f, 3, 5, 303);
                Row(l, 18f, Mul(2), Add(4));
                Row(l, 45f, Add(6), Mul(2));
                Row(l, 75f, Sub(4), Mul(3), Sub(10));
                Row(l, 105f, Add(12), Div(2));
                Row(l, 140f, Mul(2), Add(20));
                Row(l, 175f, Sub(15), Add(10));
                Row(l, 210f, Mul(2), Sub(5), Add(25));
                Horde(l, 30f, walker, 0f, 3, 5);
                Horde(l, 62f, walker, 0f, 4, 5);
                Wave(l, 80f, drone, 0f, 12, 0.5f, 3f);
                Horde(l, 92f, walker, 0f, 6, 8);
                Wave(l, 120f, drone, 0f, 20, 0.35f, 3.5f);
                Horde(l, 128f, walker, 0f, 6, 9);
                Horde(l, 158f, walker, 0f, 7, 9);
                Horde(l, 188f, walker, 0f, 8, 9);
                Horde(l, 220f, walker, 0f, 10, 9);
            });

            AssetDatabase.SaveAssets();
            Debug.Log($"[RampageSetup] Prototype content ready (hero {cluck.displayName}).");
        }

        [MenuItem("FarmFury Rampage/Setup/Rebuild Run Scene")]
        public static void RebuildRunScene() => BuildRunScene(true);

        static void BuildRunScene(bool overwrite)
        {
            EnsureFolder(Root + "/Scenes");
            if (!overwrite && File.Exists(ScenePath))
            {
                RegisterScenes();
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;

            var runGo = new GameObject("Run");
            var bootstrap = runGo.AddComponent<RunBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("tuning").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameTuning>(ContentRoot + "/Tuning/GameTuning.asset");
            so.FindProperty("hero").objectReferenceValue = AssetDatabase.LoadAssetAtPath<HeroDef>(ContentRoot + "/Heroes/Hero_Cluck.asset");
            var levels = so.FindProperty("levels");
            string[] levelPaths = FindAssetPaths<LevelDef>(ContentRoot + "/Levels");
            levels.arraySize = levelPaths.Length;
            for (int i = 0; i < levelPaths.Length; i++)
                levels.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDef>(levelPaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            Debug.Log("[RampageSetup] Run scene built.");
        }

        static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ---- content helpers ----

        static void Level(LevelDef l, string id, string displayName, float length, int difficulty, int startingHerd, uint seed)
        {
            l.id = id;
            l.displayName = displayName;
            l.length = length;
            l.difficultyLevel = difficulty;
            l.startingHerd = startingHerd;
            l.seed = seed;
            l.gateRows = new List<GateRowDef>();
            l.waves = new List<WaveDef>();
        }

        static void Row(LevelDef l, float distance, params GatePanelDef[] panels) =>
            l.gateRows.Add(new GateRowDef { distance = distance, panels = panels });

        static void Wave(LevelDef l, float distance, RobotDef robot, float x, int count, float spacing, float xJitter) =>
            l.waves.Add(new WaveDef { distance = distance, robot = robot, x = x, count = count, spacing = spacing, xJitter = xJitter });

        /// <summary>A tightly packed block of robots, <paramref name="rows"/> deep and <paramref name="columns"/> wide.</summary>
        static void Horde(LevelDef l, float distance, RobotDef robot, float x, int rows, int columns) =>
            l.waves.Add(new WaveDef
            {
                distance = distance, robot = robot, x = x, count = rows, spacing = HordeSpacing,
                columns = columns, columnSpacing = HordeSpacing,
            });

        static GatePanelDef Add(int v) => new(GateKind.Add, v);
        static GatePanelDef Sub(int v) => new(GateKind.Subtract, v);
        static GatePanelDef Mul(int v) => new(GateKind.Multiply, v);
        static GatePanelDef Div(int v) => new(GateKind.Divide, v);

        /// <summary>Creates the asset if missing. With <paramref name="overwrite"/>, re-applies the values in place (keeps the GUID).</summary>
        static T CreateOrUpdate<T>(string path, bool overwrite, System.Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                if (!overwrite) return existing;
                init(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static string[] FindAssetPaths<T>(string folder) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            System.Array.Sort(paths, System.StringComparer.Ordinal);
            return paths;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
