#if !ODIN_INSPECTOR

using UnityEditor;

namespace DInject
{
    [CustomEditor(typeof(ProjectContext))]
    [NoReflectionBaking]
    public class ProjectContextEditor : ContextEditor
    {
        SerializedProperty _settingsProperty;
        SerializedProperty _parentNewObjectsUnderContextProperty;

        public override void OnEnable()
        {
            base.OnEnable();

            _settingsProperty = serializedObject.FindProperty("_settings");
            _parentNewObjectsUnderContextProperty = serializedObject.FindProperty("_parentNewObjectsUnderContext");
        }

        protected override void OnGui()
        {
            base.OnGui();

            EditorGUILayout.PropertyField(_settingsProperty, true);
            EditorGUILayout.PropertyField(_parentNewObjectsUnderContextProperty);
        }
    }
}

#endif
