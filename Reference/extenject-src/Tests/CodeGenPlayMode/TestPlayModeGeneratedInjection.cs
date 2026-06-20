using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Zenject.Tests.CodeGenPlayMode
{
    // M5: the real PlayMode MonoBehaviour case. In play mode a runtime MonoBehaviour is AddComponent'd
    // onto a GameObject and injected with runtime reflection DISABLED (NoCheckAssumeFullCoverage). This
    // exercises the full generated path end-to-end: the per-assembly registry registers the generated
    // getter at SubsystemRegistration, and DiContainer.Inject uses it - no direct reflection.
    public class TestPlayModeGeneratedInjection
    {
        static bool GeneratorActive()
        {
            return typeof(PlayConsumerMono).GetMethod("__zenCreateInjectTypeInfo",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null;
        }

        [UnityTest]
        public IEnumerator InjectsGeneratedMonoBehaviourWithReflectionOff()
        {
            // Let play mode (and the generated registry's RuntimeInitializeOnLoadMethod) settle.
            yield return null;

            if (!GeneratorActive())
            {
                Assert.Ignore("DInject generator not active for the PlayMode assembly - " +
                    "verify the RoslynAnalyzer label on the copied Zenject.CodeGen.dll.");
            }

            var previousMode = TypeAnalyzer.ReflectionBakingCoverageMode;
            GameObject go = null;
            try
            {
                TypeAnalyzer.ClearTypeInfoCache();
                TypeAnalyzer.ReflectionBakingCoverageMode =
                    ReflectionBakingCoverageModes.NoCheckAssumeFullCoverage;

                var container = new DiContainer();
                container.Bind<PlayService>().FromInstance(new PlayService());

                go = new GameObject("play-consumer");
                var mb = go.AddComponent<PlayConsumerMono>();

                container.Inject(mb);

                Assert.IsNotNull(mb.Service,
                    "MonoBehaviour injected via generated metadata with reflection off");
            }
            finally
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
                TypeAnalyzer.ReflectionBakingCoverageMode = previousMode;
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }
    }
}
