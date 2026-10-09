using System.IO;
using FarmFuryRampage.Data;
using FarmFuryRampage.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FarmFuryRampage.Tests
{
    /// <summary>Drop-in art: a PNG placed in the Art folders is imported as a sprite and wired to the game.</summary>
    public sealed class ArtPipelineTests
    {
        const string BlastPath = RampageArt.ArtRoot + "/Effects/blast_zz_test.png";
        const string RunPath = RampageArt.ArtRoot + "/Heroes/Cluck/run_zz_test.png";
        const string EggPath = RampageArt.ArtRoot + "/Heroes/Cluck/egg_zz_test.png";

        static void WritePng(string path)
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void DroppedPngs_AreImportedAsSprites_AndWiredToTheGame()
        {
            RampageArt.CreateFolders();
            try
            {
                WritePng(BlastPath);
                WritePng(RunPath);
                WritePng(EggPath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(BlastPath);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);

                RampageArt.AssignAll();

                RunArt art = RampageArt.GetOrCreateRunArt();
                Assert.IsNotNull(art.blast, "blast wired from Effects/");
                var cluck = AssetDatabase.LoadAssetAtPath<HeroDef>("Assets/_Project/ScriptableObjects/Heroes/Hero_Cluck.asset");
                Assert.IsNotNull(cluck.projectileSprite, "egg wired from Heroes/Cluck/");
                Assert.AreEqual("egg_zz_test", cluck.projectileSprite.name);
                Assert.IsTrue(System.Array.Exists(cluck.runFrames, s => s.name == "run_zz_test"), "run frame wired");
                Assert.IsFalse(System.Array.Exists(cluck.runFrames, s => s.name == "egg_zz_test"), "egg is not a run frame");
            }
            finally
            {
                AssetDatabase.DeleteAsset(BlastPath);
                AssetDatabase.DeleteAsset(RunPath);
                AssetDatabase.DeleteAsset(EggPath);
                RampageArt.AssignAll();
            }
        }
    }
}
