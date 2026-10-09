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
        public static void CreatePrototypeContent()
        {
            EnsureFolder(ContentRoot + "/Tuning");
            EnsureFolder(ContentRoot + "/Heroes");
            EnsureFolder(ContentRoot + "/Robots");
            EnsureFolder(ContentRoot + "/Levels");

            GetOrCreate<GameTuning>(ContentRoot + "/Tuning/GameTuning.asset", t =>
            {
                t.tickRate = 60;
                t.track = new TrackTuning
                {
                    width = 9f, pace = 3f, viewWidth = 10f, herdScreenY = 0.3f, spawnAhead = 16f, despawnBehind = 8f,
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

            HeroDef cluck = GetOrCreate<HeroDef>(ContentRoot + "/Heroes/Hero_Cluck.asset", h =>
            {
                h.id = "cluck";
                h.displayName = "Cluck";
                h.greyboxColor = new Color(1f, 0.92f, 0.55f);
                h.stats = new HeroStats
                {
                    fireInterval = 0.6f, projectilesPerShot = 3, spreadDegrees = 30f, damagePerHit = 2f,
                    range = 12f, projectileSpeed = 14f, projectileRadius = 0.12f,
                };
            });

            RobotDef drone = GetOrCreate<RobotDef>(ContentRoot + "/Robots/Robot_BuzzDrone.asset", r =>
            {
                r.id = "buzz_drone";
                r.displayName = "Buzz Drone";
                r.tier = 1;
                r.greyboxColor = new Color(0.82f, 0.84f, 0.88f);
                r.stats = new RobotStats { hp = 20f, speed = 3f, bite = 1, radius = 0.3f, homing = 0.6f, scrap = 1 };
            });

            RobotDef walker = GetOrCreate<RobotDef>(ContentRoot + "/Robots/Robot_BoltWalker.asset", r =>
            {
                r.id = "bolt_walker";
                r.displayName = "Bolt Walker";
                r.tier = 1;
                r.greyboxColor = new Color(0.45f, 0.48f, 0.55f);
                r.stats = new RobotStats { hp = 60f, speed = 1.8f, bite = 2, radius = 0.45f, homing = 0f, scrap = 2 };
            });

            GetOrCreate<LevelDef>(ContentRoot + "/Levels/Level_Proto01.asset", l =>
            {
                Level(l, "proto_01", "Proto 1 · First Gates", 180f, 1, 5, 101);
                Row(l, 20f, Mul(2), Add(3));
                Row(l, 45f, Add(5), Sub(3));
                Row(l, 70f, Add(8), Div(2));
                Row(l, 100f, Mul(3), Add(10));
                Row(l, 130f, Sub(5), Add(5), Mul(2));
                Row(l, 160f, Add(10), Sub(20));
                Wave(l, 30f, drone, 0f, 5, 1f, 2f);
                Wave(l, 55f, walker, -2f, 3, 3f, 0f);
                Wave(l, 80f, drone, 1f, 8, 0.8f, 3f);
                Wave(l, 110f, walker, 1f, 5, 2.5f, 1.5f);
                Wave(l, 140f, drone, 0f, 10, 0.6f, 3.5f);
                Wave(l, 150f, walker, 0f, 6, 2f, 3f);
            });

            GetOrCreate<LevelDef>(ContentRoot + "/Levels/Level_Proto02.asset", l =>
            {
                Level(l, "proto_02", "Proto 2 · Shoot the Reds", 210f, 2, 5, 202);
                Row(l, 20f, Sub(3), Add(2));
                Row(l, 45f, Mul(2), Sub(5));
                Row(l, 75f, Sub(8), Add(4));
                Row(l, 105f, Div(2), Mul(2));
                Row(l, 140f, Sub(6), Add(6), Sub(2));
                Row(l, 175f, Mul(2), Add(15));
                Wave(l, 30f, walker, 0f, 3, 2.5f, 1f);
                Wave(l, 60f, drone, -1f, 8, 0.7f, 3f);
                Wave(l, 90f, walker, 2f, 5, 2f, 1f);
                Wave(l, 120f, drone, 0f, 12, 0.5f, 3.5f);
                Wave(l, 155f, walker, -1f, 8, 1.6f, 2.5f);
                Wave(l, 185f, drone, 0f, 14, 0.5f, 3.5f);
            });

            GetOrCreate<LevelDef>(ContentRoot + "/Levels/Level_Proto03.asset", l =>
            {
                Level(l, "proto_03", "Proto 3 · The Swarm", 240f, 3, 5, 303);
                Row(l, 18f, Mul(2), Add(4));
                Row(l, 40f, Add(6), Mul(2));
                Row(l, 70f, Sub(4), Mul(3), Sub(10));
                Row(l, 100f, Add(12), Div(2));
                Row(l, 135f, Mul(2), Add(20));
                Row(l, 170f, Sub(15), Add(10));
                Row(l, 205f, Mul(2), Sub(5), Add(25));
                Wave(l, 28f, drone, 0f, 10, 0.6f, 3f);
                Wave(l, 55f, walker, 0f, 6, 1.5f, 3f);
                Wave(l, 85f, drone, -2f, 15, 0.4f, 2f);
                Wave(l, 115f, walker, 2f, 8, 1.2f, 2f);
                Wave(l, 150f, drone, 0f, 20, 0.35f, 3.5f);
                Wave(l, 185f, walker, 0f, 12, 1f, 3.5f);
                Wave(l, 215f, drone, 0f, 20, 0.4f, 3.5f);
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

        static GatePanelDef Add(int v) => new(GateKind.Add, v);
        static GatePanelDef Sub(int v) => new(GateKind.Subtract, v);
        static GatePanelDef Mul(int v) => new(GateKind.Multiply, v);
        static GatePanelDef Div(int v) => new(GateKind.Divide, v);

        static T GetOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
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
