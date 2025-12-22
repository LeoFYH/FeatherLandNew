using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public class SpriteRendererMaterialReplacer : EditorWindow
    {
        private Material targetMaterial;
        private DefaultAsset targetFolder;
        private Vector2 scrollPosition;
        private List<string> processedPrefabs = new List<string>();
        private bool showResults = false;
        private int successCount = 0;
        private int errorCount = 0;

        [MenuItem("Tools/材质工具/批量替换SpriteRenderer材质")]
        public static void ShowWindow()
        {
            GetWindow<SpriteRendererMaterialReplacer>("材质替换工具");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // 标题
            EditorGUILayout.LabelField("SpriteRenderer材质批量替换工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 材质选择
            targetMaterial = (Material)EditorGUILayout.ObjectField("目标材质", targetMaterial, typeof(Material), false);

            // 文件夹选择
            targetFolder =
                (DefaultAsset)EditorGUILayout.ObjectField("目标文件夹", targetFolder, typeof(DefaultAsset), false);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("将替换指定文件夹内所有预制体（包括子文件夹）中的SpriteRenderer组件的材质", MessageType.Info);
            EditorGUILayout.Space();

            // 操作按钮
            GUI.enabled = targetMaterial != null && targetFolder != null;
            if (GUILayout.Button("开始替换", GUILayout.Height(30)))
            {
                StartReplacement();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            // 显示结果
            if (showResults)
            {
                DisplayResults();
            }
        }

        private void StartReplacement()
        {
            processedPrefabs.Clear();
            successCount = 0;
            errorCount = 0;

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择有效的文件夹！", "确定");
                return;
            }

            // 查找所有预制体
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            if (prefabGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "在指定文件夹中未找到预制体！", "确定");
                return;
            }

            // 显示进度条
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string guid = prefabGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar("处理中", $"正在处理: {Path.GetFileName(assetPath)}",
                    (float)i / prefabGuids.Length);

                ProcessPrefab(assetPath);
            }

            EditorUtility.ClearProgressBar();
            showResults = true;

            // 刷新资源数据库
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成",
                $"处理完成！\n成功: {successCount} 个\n失败: {errorCount} 个", "确定");

            Repaint();
        }

        private void ProcessPrefab(string assetPath)
        {
            try
            {
                // 加载预制体
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    errorCount++;
                    processedPrefabs.Add($"{Path.GetFileName(assetPath)} - 加载失败");
                    return;
                }

                bool modified = false;

                // 获取预制体中的所有SpriteRenderer组件（包括子对象）
                SpriteRenderer[] spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);

                foreach (SpriteRenderer renderer in spriteRenderers)
                {
                    // 如果材质不是目标材质，则进行替换
                    if (renderer.sharedMaterial != targetMaterial)
                    {
                        renderer.sharedMaterial = targetMaterial;
                        modified = true;
                    }
                }

                if (modified)
                {
                    // 保存预制体修改
                    PrefabUtility.SavePrefabAsset(prefab);
                    successCount++;
                    processedPrefabs.Add($"{Path.GetFileName(assetPath)} - ✓ 成功替换");
                }
                else
                {
                    processedPrefabs.Add($"{Path.GetFileName(assetPath)} - ○ 无需修改");
                }
            }
            catch (System.Exception e)
            {
                errorCount++;
                processedPrefabs.Add($"{Path.GetFileName(assetPath)} - ✗ 错误: {e.Message}");
                Debug.LogError($"处理预制体 {assetPath} 时出错: {e}");
            }
        }

        private void DisplayResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("处理结果:", EditorStyles.boldLabel);

            GUILayout.BeginVertical("box", GUILayout.Height(200));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (string result in processedPrefabs)
            {
                if (result.Contains("✓"))
                {
                    GUI.contentColor = Color.green;
                }
                else if (result.Contains("✗"))
                {
                    GUI.contentColor = Color.red;
                }
                else
                {
                    GUI.contentColor = Color.gray;
                }

                EditorGUILayout.LabelField(result);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // 统计信息
            GUILayout.BeginHorizontal();
            GUILayout.Label($"成功: {successCount}", GUILayout.Width(80));
            GUILayout.Label($"失败: {errorCount}", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            // 导出结果按钮
            if (GUILayout.Button("导出处理结果"))
            {
                ExportResults();
            }
        }

        private void ExportResults()
        {
            string filePath = EditorUtility.SaveFilePanel("导出处理结果", "", "材质替换结果", "txt");

            if (!string.IsNullOrEmpty(filePath))
            {
                List<string> lines = new List<string>
                {
                    $"SpriteRenderer材质替换结果 - {System.DateTime.Now}",
                    $"目标材质: {targetMaterial?.name ?? "None"}",
                    $"目标文件夹: {AssetDatabase.GetAssetPath(targetFolder)}",
                    "======================================"
                };

                lines.AddRange(processedPrefabs);
                lines.Add("======================================");
                lines.Add($"总计: 成功 {successCount} 个, 失败 {errorCount} 个");

                File.WriteAllLines(filePath, lines);
                EditorUtility.RevealInFinder(filePath);
            }
        }
    }
}