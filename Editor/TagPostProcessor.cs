using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace com.darktable.utility {
    public class TagPostProcessor : UnityEditor.AssetModificationProcessor {
        private const string HEADER = "/* AUTO GENERATED FILE DO NOT MODIFY\n (created by TagPostProcessor.cs. File->Save Project will usually trigger an update of this file) */\n\n";
        private const string STANDARD_ASSETS = "Standard Assets";
        private const string DIRECTORY_NAME = "Scripts/Tag Constants";
        private const string FILE_NAME = "TagConstants.cs";
        private static readonly string[] SAVED_ASSETS = new[] { "NavMeshAreas.asset", "TagManager.asset", "EditorBuildSettings.asset" };

        private const string ASM_DEF_FILENAME = "com.darktable.tagconstants.asmdef";
        private const string ASM_DEF_CONTENTS = "{\"name\": \"com.darktable.tagconstants\"}";

        private static readonly Regex NON_WORD_REG_EX = new Regex("[^a-zA-Z0-9]");
        private static readonly Regex PREFIX_NUMBER_REG_EX = new Regex("^[0-9]");

        [InitializeOnLoadMethod]
        private static void ValidateTagConstants() {
            string rootPath = Path.Combine(Application.dataPath, STANDARD_ASSETS, DIRECTORY_NAME);

            if (!Directory.Exists(rootPath)) {
                UpdateTagsConstants();

                return;
            }

            string fileName = Path.Combine(rootPath, FILE_NAME);

            if (!File.Exists(fileName)) {
                UpdateTagsConstants();
            }
        }

        private static string[] OnWillSaveAssets(string[] paths) {
            foreach (string path in paths) {
                string filename = Path.GetFileName(path);

                foreach (string asset in SAVED_ASSETS) {
                    if (filename.Equals(asset, StringComparison.InvariantCultureIgnoreCase)) {
                        UpdateTagsConstants();

                        return paths;
                    }
                }
            }

            return paths;
        }

        private static string SanitizeVariableName(string label) {
            string sanitisedTag = NON_WORD_REG_EX.Replace(label, "_");
            sanitisedTag = PREFIX_NUMBER_REG_EX.Replace(sanitisedTag, match => "_" + match.Value);

            return sanitisedTag;
        }

        private static void UpdateTagList(StringBuilder outfile) {
            var nameCollisions = new Dictionary<string, string>();

            outfile.AppendLine("public static class TagConstant\n{");

            foreach (string tag in InternalEditorUtility.tags) {
                string sanitisedTag = SanitizeVariableName(tag);

                if (nameCollisions.TryGetValue(sanitisedTag, out var collision)) {
                    Debug.LogError($"Two tags with same sanitized name: \"{collision}\", \"{tag}\" = \"{sanitisedTag}\"");

                    continue;
                }

                nameCollisions.Add(sanitisedTag, tag);

                outfile.AppendLine($"    public const string {sanitisedTag} = \"{tag}\";");
            }

            outfile.AppendLine("}\n");
        }

        private static void UpdateLayerList(StringBuilder outfile) {
            var nameCollisions = new Dictionary<string, string>();

            outfile.AppendLine("public static class LayerConstant\n{");

            foreach (string layer in InternalEditorUtility.layers) {
                string sanitizedLayer = SanitizeVariableName(layer);

                if (nameCollisions.TryGetValue(sanitizedLayer, out string collision)) {
                    Debug.LogError($"Two layers with same sanitized name: \"{collision}\", \"{layer}\" = \"{sanitizedLayer}\"");

                    continue;
                }

                nameCollisions.Add(sanitizedLayer, layer);

                outfile.AppendLine($"    public const int {sanitizedLayer} = {LayerMask.NameToLayer(layer)};");
            }

            outfile.AppendLine("}\n");

            outfile.AppendLine("[System.Flags]\npublic enum LayerFlag\n{");

            foreach (var kvp in nameCollisions) {
                outfile.AppendLine($"    {kvp.Key} = 1 << {LayerMask.NameToLayer(kvp.Value)},");
            }

            outfile.AppendLine("}\n");

            outfile.AppendLine("public static class LayerName\n{");

            foreach (var kvp in nameCollisions) {
                outfile.AppendLine($"    public const string {kvp.Key} = \"{kvp.Value}\";");
            }

            outfile.AppendLine("}\n");

        }

        private static void UpdateSortingLayerList(StringBuilder outfile) {
            var nameCollisions = new Dictionary<string, string>();

            outfile.AppendLine("public static class SortingLayerConstant\n{");

            foreach (var sortingLayer in SortingLayer.layers) {
                string name = sortingLayer.name;
                string sanitizedLayer = SanitizeVariableName(name);

                if (nameCollisions.TryGetValue(sanitizedLayer, out string collision)) {
                    Debug.LogError($"Two layers with same sanitized name: \"{collision}\", \"{name}\" = \"{sanitizedLayer}\"");

                    continue;
                }

                nameCollisions.Add(sanitizedLayer, name);

                outfile.AppendLine($"    public const int {sanitizedLayer} = {sortingLayer.id};");
            }

            outfile.AppendLine("}\n");
        }

        private static void UpdateNavAgentsList(StringBuilder outfile) {
            outfile.AppendLine("public static class NavMeshAgentID\n{");
            var nameCollisions = new Dictionary<string, string>();
            int count = NavMesh.GetSettingsCount();
            for (var i = 0; i < count; i++) {
                int agentTypeID = NavMesh.GetSettingsByIndex(i).agentTypeID;
                string agentName = NavMesh.GetSettingsNameFromID(agentTypeID);
                string sanitizedAgentName = SanitizeVariableName(agentName);

                if (nameCollisions.TryGetValue(sanitizedAgentName, out var collision)) {
                    Debug.LogError($"Two layers with same sanitized name: \"{collision}\", \"{agentName}\" = \"{sanitizedAgentName}\"");

                    continue;
                }

                nameCollisions.Add(sanitizedAgentName, agentName);

                outfile.AppendLine($"    public const int {sanitizedAgentName} = {agentTypeID};");
            }

            outfile.AppendLine("}\n");
        }

        private static void UpdateSceneList(StringBuilder outfile) {
            outfile.AppendLine("public static class SceneName\n{");
            var nameCollisions = new Dictionary<string, string>();
            int count = SceneManager.sceneCountInBuildSettings;

            var sceneNames = new string[count];

            for (var i = 0; i < count; i++) {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

                if (string.IsNullOrEmpty(scenePath)) {
                    continue;
                }

                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string sanitizedSceneName = SanitizeVariableName(sceneName);

                if (nameCollisions.TryGetValue(sanitizedSceneName, out var collision)) {
                    Debug.LogError($"Two scenes with same sanitized name: \"{collision}\", \"{sceneName}\" = \"{sanitizedSceneName}\"");

                    continue;
                }

                nameCollisions.Add(sanitizedSceneName, sceneName);

                outfile.AppendLine($"    public const string {sanitizedSceneName} = \"{sceneName}\";");
                sceneNames[i] = sanitizedSceneName;
            }

            outfile.AppendLine("}\n");

            outfile.AppendLine("public static class SceneIndex\n{");

            for (var i = 0; i < count; i++) {
                string name = sceneNames[i];

                if (string.IsNullOrEmpty(name)) {
                    continue;
                }

                outfile.AppendLine($"    public const int {name} = {i};");
            }

            outfile.AppendLine("}\n");
        }

        private static void UpdateTagsConstants() {
            string rootPath = Path.Combine(Application.dataPath, STANDARD_ASSETS, DIRECTORY_NAME);

            if (!Directory.Exists(rootPath)) {
                Directory.CreateDirectory(rootPath);
            }

            string asmDefPath = Path.Combine(rootPath, ASM_DEF_FILENAME);

            if (!File.Exists(asmDefPath)) {
                File.WriteAllText(asmDefPath, ASM_DEF_CONTENTS);
            }

            string filePath = Path.Combine(rootPath, FILE_NAME);
            string current = null;

            if (File.Exists(filePath)) {
                current = File.ReadAllText(filePath);
            }

            var builder = new StringBuilder(HEADER, 4096);
            UpdateTagList(builder);

            UpdateSortingLayerList(builder);

            UpdateLayerList(builder);

            UpdateNavAgentsList(builder);

            UpdateSceneList(builder);

            var updated = builder.ToString();

            if (!string.Equals(current, updated)) {
                Debug.Log("Generating tag constants file");
                File.WriteAllText(filePath, updated);

                AssetDatabase.Refresh();
            }
        }
    }
}
