using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmFuryRampage.Tests
{
    /// <summary>
    /// Dev tool, not part of the normal suite: plays the Run scene and saves portrait screenshots, for checking art
    /// without opening the editor. Run explicitly (batch mode without -nographics):
    /// Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter RunCaptureTests
    /// Output folder: RAMPAGE_CAPTURE_DIR environment variable, or Application.temporaryCachePath.
    /// </summary>
    [Explicit("Dev tool: captures screenshots of the Run scene.")]
    public sealed class RunCaptureTests
    {
        const int Width = 540;
        const int Height = 1170;
        static readonly float[] CaptureSeconds = { 6f, 14f, 24f };

        [UnityTest]
        public IEnumerator CaptureRunScene()
        {
            yield return SceneManager.LoadSceneAsync(0);
            string dir = Environment.GetEnvironmentVariable("RAMPAGE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Application.temporaryCachePath;
            Directory.CreateDirectory(dir);

            Camera cam = Camera.main;
            Assert.IsNotNull(cam);
            var target = new RenderTexture(Width, Height, 24);
            cam.targetTexture = target;

            float start = Time.time;
            foreach (float at in CaptureSeconds)
            {
                while (Time.time - start < at) yield return null;
                cam.Render(); // manual render: WaitForEndOfFrame never fires in batch mode
                RenderTexture.active = target;
                var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                string path = Path.Combine(dir, $"run_{at:00}s.png");
                File.WriteAllBytes(path, shot.EncodeToPNG());
                UnityEngine.Object.Destroy(shot);
                Debug.Log($"[RunCapture] {path}");
            }

            cam.targetTexture = null;
            target.Release();
        }
    }
}
