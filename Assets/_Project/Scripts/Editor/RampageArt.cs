using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FarmFuryRampage.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FarmFuryRampage.Editor
{
    /// <summary>
    /// Drop-in art. PNGs placed under Assets/_Project/Art/ are imported as sprites and wired to the game by folder and
    /// file name (see Art/README.md):
    ///   Heroes/&lt;DisplayName&gt;/run_*.png, egg.png      -> HeroDef.runFrames, projectileSprite
    ///   Robots/&lt;DisplayNameNoSpaces&gt;/walk_*.png        -> RobotDef.walkFrames
    ///   Track/ground.png, Effects/blast.png               -> RunArt.ground, RunArt.blast
    ///   Gates/add.png, subtract.png, multiply.png, divide.png -> RunArt gate frames
    /// Wiring re-runs automatically whenever anything under Art/ changes, or via FarmFury Rampage > Art.
    /// </summary>
    public static class RampageArt
    {
        public const string ArtRoot = "Assets/_Project/Art";
        public const string RunArtPath = "Assets/_Project/ScriptableObjects/Art/RunArt.asset";

        public static readonly string[] Folders =
        {
            "Heroes/Cluck", "Robots/BoltWalker", "Robots/BuzzDrone", "Robots/TillerTank", "Track", "Effects", "Gates",
        };

        [MenuItem("FarmFury Rampage/Art/Assign Art From Folders")]
        public static void AssignAll()
        {
            var report = new StringBuilder("[RampageArt] ");

            foreach (HeroDef hero in LoadAll<HeroDef>())
            {
                List<(string name, Sprite sprite)> sprites = SpritesIn($"{ArtRoot}/Heroes/{FolderName(hero.displayName)}");
                Sprite[] run = Matching(sprites, "run");
                if (run.Length == 0) run = Excluding(sprites, "egg", "projectile");
                Sprite egg = First(sprites, "egg", "projectile");
                hero.runFrames = run;
                hero.projectileSprite = egg;
                EditorUtility.SetDirty(hero);
                report.Append($"{hero.displayName}: {run.Length} run frames{(egg != null ? " + egg" : "")}; ");
            }

            foreach (RobotDef robot in LoadAll<RobotDef>())
            {
                List<(string name, Sprite sprite)> sprites = SpritesIn($"{ArtRoot}/Robots/{FolderName(robot.displayName)}");
                Sprite[] walk = Matching(sprites, "walk");
                if (walk.Length == 0) walk = Excluding(sprites);
                robot.walkFrames = walk;
                EditorUtility.SetDirty(robot);
                report.Append($"{robot.displayName}: {walk.Length} walk frames; ");
            }

            RunArt art = GetOrCreateRunArt();
            List<(string name, Sprite sprite)> track = SpritesIn($"{ArtRoot}/Track");
            List<(string name, Sprite sprite)> effects = SpritesIn($"{ArtRoot}/Effects");
            List<(string name, Sprite sprite)> gates = SpritesIn($"{ArtRoot}/Gates");
            art.ground = First(track, "ground");
            art.blast = First(effects, "blast", "explosion");
            art.gateAdd = First(gates, "add", "plus");
            art.gateSubtract = First(gates, "subtract", "minus");
            art.gateMultiply = First(gates, "multiply", "times");
            art.gateDivide = First(gates, "divide");
            EditorUtility.SetDirty(art);
            report.Append($"ground {(art.ground != null ? "yes" : "no")}, blast {(art.blast != null ? "yes" : "no")}, gates ");
            report.Append($"{Count(art.gateAdd, art.gateSubtract, art.gateMultiply, art.gateDivide)}/4.");

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        [MenuItem("FarmFury Rampage/Art/Create Art Folders")]
        public static void CreateFolders()
        {
            foreach (string folder in Folders) EnsureFolder($"{ArtRoot}/{folder}");
        }

        public static RunArt GetOrCreateRunArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<RunArt>(RunArtPath);
            if (art != null) return art;
            EnsureFolder(Path.GetDirectoryName(RunArtPath).Replace('\\', '/'));
            art = ScriptableObject.CreateInstance<RunArt>();
            AssetDatabase.CreateAsset(art, RunArtPath);
            return art;
        }

        /// <summary>"Bolt Walker" -> "BoltWalker".</summary>
        public static string FolderName(string displayName) => (displayName ?? string.Empty).Replace(" ", string.Empty);

        static int Count(params Sprite[] sprites)
        {
            int n = 0;
            foreach (Sprite s in sprites) if (s != null) n++;
            return n;
        }

        static List<(string name, Sprite sprite)> SpritesIn(string folder)
        {
            var list = new List<(string, Sprite)>();
            if (!AssetDatabase.IsValidFolder(folder)) return list;
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue; // this folder only
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) list.Add((Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), sprite));
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
            return list;
        }

        static Sprite[] Matching(List<(string name, Sprite sprite)> sprites, string prefix)
        {
            var result = new List<Sprite>();
            foreach ((string name, Sprite sprite) in sprites)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) result.Add(sprite);
            return result.ToArray();
        }

        static Sprite[] Excluding(List<(string name, Sprite sprite)> sprites, params string[] prefixes)
        {
            var result = new List<Sprite>();
            foreach ((string name, Sprite sprite) in sprites)
            {
                bool excluded = false;
                foreach (string prefix in prefixes) excluded |= name.StartsWith(prefix, StringComparison.Ordinal);
                if (!excluded) result.Add(sprite);
            }
            return result.ToArray();
        }

        static Sprite First(List<(string name, Sprite sprite)> sprites, params string[] prefixes)
        {
            foreach (string prefix in prefixes)
            foreach ((string name, Sprite sprite) in sprites)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return sprite;
            return null;
        }

        static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project" }))
                list.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    /// <summary>Imports anything dropped under Assets/_Project/Art/ as a sprite and re-wires the art afterwards.</summary>
    public sealed class RampageArtImporter : AssetPostprocessor
    {
        const int MaxTextureSize = 2048;
        /// <summary>Pixels per unit only affects the import; the game sizes every sprite in metres itself.</summary>
        const float PixelsPerUnit = 256f;
        static bool assignQueued;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(RampageArt.ArtRoot + "/", StringComparison.Ordinal)) return;
            if (!assetImporter.importSettingsMissing) return; // first import only, so manual tweaks stick

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (assignQueued || !(Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))) return;
            assignQueued = true;
            EditorApplication.delayCall += () =>
            {
                assignQueued = false;
                RampageArt.AssignAll();
            };
        }

        static bool Touches(string[] paths)
        {
            foreach (string path in paths)
                if (path.StartsWith(RampageArt.ArtRoot + "/", StringComparison.Ordinal) && !path.EndsWith(".md", StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
