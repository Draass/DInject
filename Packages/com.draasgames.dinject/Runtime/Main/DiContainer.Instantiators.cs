using System;
using System.Collections.Generic;
using DInject.Internal;
using UnityEngine;

namespace DInject
{
    public partial class DiContainer
    {
               
        // Use this method to create any non-monobehaviour
        // Any fields marked [Inject] will be set using the bindings on the container
        // Any methods marked with a [Inject] will be called
        // Any constructor parameters will be filled in with values from the container
        public T Instantiate<T>()
        {
            return Instantiate<T>(new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T Instantiate<T>(IEnumerable<object> extraArgs)
        {
            var result = Instantiate(typeof(T), extraArgs);

            if (IsValidating && !(result is T))
            {
                Assert.That(result is ValidationMarker);
                return default(T);
            }

            return (T)result;
        }

        public object Instantiate(Type concreteType)
        {
            return Instantiate(concreteType, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public object Instantiate(
            Type concreteType, IEnumerable<object> extraArgs)
        {
            Assert.That(!extraArgs.ContainsItem(null),
                "Null value given to factory constructor arguments when instantiating object with type '{0}'. In order to use null use InstantiateExplicit", concreteType);

            return InstantiateExplicit(
                concreteType, InjectUtil.CreateArgList(extraArgs));
        }

#if !NOT_UNITY3D
        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        public TContract InstantiateComponent<TContract>(GameObject gameObject)
            where TContract : Component
        {
            return InstantiateComponent<TContract>(gameObject, new object[0]);
        }

        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public TContract InstantiateComponent<TContract>(
            GameObject gameObject, IEnumerable<object> extraArgs)
            where TContract : Component
        {
            return (TContract)InstantiateComponent(typeof(TContract), gameObject, extraArgs);
        }

        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        public Component InstantiateComponent(
            Type componentType, GameObject gameObject)
        {
            return InstantiateComponent(componentType, gameObject, new object[0]);
        }

        // Add new component to existing game object and fill in its dependencies
        // This is the same as AddComponent except the [Inject] fields will be filled in
        // NOTE: Gameobject here is not a prefab prototype, it is an instance
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public Component InstantiateComponent(
            Type componentType, GameObject gameObject, IEnumerable<object> extraArgs)
        {
            return InstantiateComponentExplicit(
                componentType, gameObject, InjectUtil.CreateArgList(extraArgs));
        }

        public T InstantiateComponentOnNewGameObject<T>()
            where T : Component
        {
            return InstantiateComponentOnNewGameObject<T>(typeof(T).Name);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiateComponentOnNewGameObject<T>(IEnumerable<object> extraArgs)
            where T : Component
        {
            return InstantiateComponentOnNewGameObject<T>(typeof(T).Name, extraArgs);
        }

        public T InstantiateComponentOnNewGameObject<T>(string gameObjectName)
            where T : Component
        {
            return InstantiateComponentOnNewGameObject<T>(gameObjectName, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiateComponentOnNewGameObject<T>(
            string gameObjectName, IEnumerable<object> extraArgs)
            where T : Component
        {
            return InstantiateComponent<T>(
                CreateEmptyGameObject(gameObjectName),
                extraArgs);
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(UnityEngine.Object prefab)
        {
            return InstantiatePrefab(
                prefab, GameObjectCreationParameters.Default);
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(UnityEngine.Object prefab, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(
            UnityEngine.Object prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return InstantiatePrefab(
                prefab, new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        // Create a new game object from a prefab and fill in dependencies for all children
        public GameObject InstantiatePrefab(
            UnityEngine.Object prefab, GameObjectCreationParameters gameObjectBindInfo)
        {
            FlushBindings();

            bool shouldMakeActive;
            var gameObj = CreateAndParentPrefab(
                prefab, gameObjectBindInfo, null, out shouldMakeActive);

            InjectGameObject(gameObj);

            if (shouldMakeActive && !IsValidating)
            {
#if ZEN_INTERNAL_PROFILING
                using (ProfileTimers.CreateTimedBlock("User Code"))
#endif
                {
                    gameObj.SetActive(true);
                }
            }

            return gameObj;
        }

        // Create a new game object from a resource path and fill in dependencies for all children
        public GameObject InstantiatePrefabResource(string resourcePath)
        {
            return InstantiatePrefabResource(resourcePath, GameObjectCreationParameters.Default);
        }

        // Create a new game object from a resource path and fill in dependencies for all children
        public GameObject InstantiatePrefabResource(string resourcePath, Transform parentTransform)
        {
            return InstantiatePrefabResource(resourcePath, new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        public GameObject InstantiatePrefabResource(
            string resourcePath, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return InstantiatePrefabResource(
                resourcePath, new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        // Create a new game object from a resource path and fill in dependencies for all children
        public GameObject InstantiatePrefabResource(
            string resourcePath, GameObjectCreationParameters creationInfo)
        {
            var prefab = (GameObject)Resources.Load(resourcePath);

            Assert.IsNotNull(prefab,
                "Could not find prefab at resource location '{0}'".Fmt(resourcePath));

            return InstantiatePrefab(prefab, creationInfo);
        }

        // Same as InstantiatePrefab but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        public T InstantiatePrefabForComponent<T>(UnityEngine.Object prefab)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, null, new object[0]);
        }

        // Same as InstantiatePrefab but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiatePrefabForComponent<T>(
            UnityEngine.Object prefab, IEnumerable<object> extraArgs)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, null, extraArgs);
        }

        public T InstantiatePrefabForComponent<T>(
            UnityEngine.Object prefab, Transform parentTransform)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, parentTransform, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiatePrefabForComponent<T>(
            UnityEngine.Object prefab, Transform parentTransform, IEnumerable<object> extraArgs)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, parentTransform, extraArgs);
        }

        public T InstantiatePrefabForComponent<T>(
            UnityEngine.Object prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, new object[0], new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        public T InstantiatePrefabForComponent<T>(
            UnityEngine.Object prefab, Vector3 position, Quaternion rotation, Transform parentTransform, IEnumerable<object> extraArgs)
        {
            return (T)InstantiatePrefabForComponent(
                typeof(T), prefab, extraArgs, new GameObjectCreationParameters
                {
                    ParentTransform = parentTransform,
                    Position = position,
                    Rotation = rotation
                });
        }

        // Same as InstantiatePrefab but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public object InstantiatePrefabForComponent(
            Type concreteType, UnityEngine.Object prefab,
            Transform parentTransform, IEnumerable<object> extraArgs)
        {
            return InstantiatePrefabForComponent(
                concreteType, prefab, extraArgs,
                new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public object InstantiatePrefabForComponent(
            Type concreteType, UnityEngine.Object prefab,
            IEnumerable<object> extraArgs, GameObjectCreationParameters creationInfo)
        {
            return InstantiatePrefabForComponentExplicit(
                concreteType, prefab,
                InjectUtil.CreateArgList(extraArgs), creationInfo);
        }

        // Same as InstantiatePrefabResource but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        public T InstantiatePrefabResourceForComponent<T>(string resourcePath)
        {
            return (T)InstantiatePrefabResourceForComponent(
                typeof(T), resourcePath, null, new object[0]);
        }

        // Same as InstantiatePrefabResource but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiatePrefabResourceForComponent<T>(
            string resourcePath, IEnumerable<object> extraArgs)
        {
            return (T)InstantiatePrefabResourceForComponent(
                typeof(T), resourcePath, null, extraArgs);
        }

        public T InstantiatePrefabResourceForComponent<T>(
            string resourcePath, Transform parentTransform)
        {
            return (T)InstantiatePrefabResourceForComponent(
                typeof(T), resourcePath, parentTransform, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiatePrefabResourceForComponent<T>(
            string resourcePath, Transform parentTransform, IEnumerable<object> extraArgs)
        {
            return (T)InstantiatePrefabResourceForComponent(
                typeof(T), resourcePath, parentTransform, extraArgs);
        }

        public T InstantiatePrefabResourceForComponent<T>(
            string resourcePath, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            return InstantiatePrefabResourceForComponent<T>(resourcePath, position, rotation, parentTransform, new object[0]);
        }

        public T InstantiatePrefabResourceForComponent<T>(
            string resourcePath, Vector3 position, Quaternion rotation, Transform parentTransform, IEnumerable<object> extraArgs)
        {
            var argsList = InjectUtil.CreateArgList(extraArgs);
            var creationParameters = new GameObjectCreationParameters
            {
                ParentTransform = parentTransform,
                Position = position,
                Rotation = rotation
            };
            return (T)InstantiatePrefabResourceForComponentExplicit(
                typeof(T), resourcePath, argsList, creationParameters);
        }

        // Same as InstantiatePrefabResource but returns a component after it's initialized
        // and optionally allows extra arguments for the given component type
        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public object InstantiatePrefabResourceForComponent(
            Type concreteType, string resourcePath, Transform parentTransform,
            IEnumerable<object> extraArgs)
        {
            Assert.That(!extraArgs.ContainsItem(null),
                "Null value given to factory constructor arguments when instantiating object with type '{0}'. In order to use null use InstantiatePrefabForComponentExplicit", concreteType);

            return InstantiatePrefabResourceForComponentExplicit(
                concreteType, resourcePath,
                InjectUtil.CreateArgList(extraArgs),
                new GameObjectCreationParameters { ParentTransform = parentTransform });
        }

        public T InstantiateScriptableObjectResource<T>(string resourcePath)
            where T : ScriptableObject
        {
            return InstantiateScriptableObjectResource<T>(resourcePath, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public T InstantiateScriptableObjectResource<T>(
            string resourcePath, IEnumerable<object> extraArgs)
            where T : ScriptableObject
        {
            return (T)InstantiateScriptableObjectResource(
                typeof(T), resourcePath, extraArgs);
        }

        public object InstantiateScriptableObjectResource(
            Type scriptableObjectType, string resourcePath)
        {
            return InstantiateScriptableObjectResource(
                scriptableObjectType, resourcePath, new object[0]);
        }

        // Note: For IL2CPP platforms make sure to use new object[] instead of new [] when creating
        // the argument list to avoid errors converting to IEnumerable<object>
        public object InstantiateScriptableObjectResource(
            Type scriptableObjectType, string resourcePath, IEnumerable<object> extraArgs)
        {
            Assert.DerivesFromOrEqual<ScriptableObject>(scriptableObjectType);
            return InstantiateScriptableObjectResourceExplicit(
                scriptableObjectType, resourcePath, InjectUtil.CreateArgList(extraArgs));
        }
#endif
    }
}