using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

[CustomEditor(typeof(EntityBlackboard))]
public class EntityBlackboardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to view live Blackboard states.", MessageType.Info);
            return;
        }

        EntityBlackboard blackboard = (EntityBlackboard)target;
        var storages = blackboard.Editor_GetStorages();

        if (storages.Count == 0)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Live Runtime States", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        foreach (var keyValuePair in storages)
        {
            Type type = keyValuePair.Key;
            var storage = keyValuePair.Value;

            FieldInfo valuesField = storage.GetType().GetField("Values");
            if (valuesField != null)
            {
                IDictionary dict = (IDictionary)valuesField.GetValue(storage);

                foreach (DictionaryEntry entry in dict)
                {
                    int hash = (int)entry.Key;

                    string keyName = BlackboardKey.EditorNameRegistry.TryGetValue(hash, out string name)
                        ? name
                        : $"Hash: {hash}";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(keyName, GUILayout.Width(150));

                    GUI.enabled = false;

                    if (type == typeof(bool))
                        EditorGUILayout.Toggle((bool)entry.Value);
                    else if (type == typeof(int))
                        EditorGUILayout.IntField((int)entry.Value);
                    else if (type == typeof(float))
                        EditorGUILayout.FloatField((float)entry.Value);
                    else
                        EditorGUILayout.TextField(entry.Value?.ToString() ?? "null");

                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        EditorGUILayout.EndVertical();
        Repaint();
    }
}
