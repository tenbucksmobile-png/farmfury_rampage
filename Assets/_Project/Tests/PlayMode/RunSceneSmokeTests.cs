using System.Collections;
using FarmFuryRampage.Run;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmFuryRampage.Tests
{
    /// <summary>Loads the greybox Run scene and lets it play. Any error logged during the run fails the test.</summary>
    public sealed class RunSceneSmokeTests
    {
        const float PlaySeconds = 3f;

        [UnityTest]
        public IEnumerator RunScene_PlaysWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync(0);

            var bootstrap = Object.FindAnyObjectByType<RunBootstrap>();
            Assert.IsNotNull(bootstrap, "RunBootstrap missing from the Run scene.");

            yield return new WaitForSeconds(PlaySeconds);

            Assert.IsTrue(bootstrap.enabled, "RunBootstrap disabled itself (missing references?).");
            Assert.IsNotNull(Object.FindAnyObjectByType<RunView>(), "Greybox view was not created.");
        }
    }
}
