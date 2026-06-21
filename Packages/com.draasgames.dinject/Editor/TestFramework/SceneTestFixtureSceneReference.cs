#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace DInject
{
    public class SceneTestFixtureSceneReference : ScriptableObject
    {
        public SceneAsset Scene;
    }
}

#endif
