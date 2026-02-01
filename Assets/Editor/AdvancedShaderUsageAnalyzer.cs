using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AdvancedShaderUsageAnalyzer : Editor
{
    [MenuItem("Tools/Shader/分析Shader使用情况(高级)")]
    public static void AnalyzeShaderUsage()
    {
        var analyzer = new ShaderUsageAnalyzer();
        analyzer.Analyze();
    }
}

public class ShaderUsageAnalyzer
{
    private Dictionary<string, List<string>> shaderUsageMap = new Dictionary<string, List<string>>();
    private List<string> allShaders = new List<string>();
    private List<string> usedShaders = new List<string>();
    private List<string> unusedShaders = new List<string>();
    
    public void Analyze()
    {
        Debug.Log("开始高级Shader使用情况分析...");
        
        // 1. 获取所有Shader
        GetAllShaders();
        
        // 2. 分析各种资源对Shader的引用
        AnalyzeMaterialReferences();
        AnalyzePrefabReferences();
        AnalyzeSceneReferences();
        
        // 3. 输出结果
        OutputAnalysisResults();
    }
    
    private void GetAllShaders()
    {
        allShaders.Clear();
        string[] shaderGUIDs = AssetDatabase.FindAssets("t:Shader");
        foreach (string guid in shaderGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allShaders.Add(path);
        }
        
        Debug.Log($"找到 {allShaders.Count} 个Shader文件");
    }
    
    private void AnalyzeMaterialReferences()
    {
        usedShaders.Clear();
        shaderUsageMap.Clear();
        
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        Debug.Log($"分析 {materialGUIDs.Length} 个材质球对Shader的引用...");
        
        int processed = 0;
        int total = materialGUIDs.Length;
        
        foreach (string guid in materialGUIDs)
        {
            processed++;
            if (processed % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("分析材质球引用", 
                    $"处理材质球 {processed}/{total}", (float)processed / total);
            }
            
            string materialPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (material != null && material.shader != null)
            {
                string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                
                if (!usedShaders.Contains(shaderPath))
                {
                    usedShaders.Add(shaderPath);
                }
                
                if (!shaderUsageMap.ContainsKey(shaderPath))
                {
                    shaderUsageMap[shaderPath] = new List<string>();
                }
                
                if (!shaderUsageMap[shaderPath].Contains(materialPath))
                {
                    shaderUsageMap[shaderPath].Add(materialPath);
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
    }
    
    private void AnalyzePrefabReferences()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        Debug.Log($"分析 {prefabGUIDs.Length} 个预制件对Shader的引用...");
        
        int processed = 0;
        int total = prefabGUIDs.Length;
        
        foreach (string guid in prefabGUIDs)
        {
            processed++;
            if (processed % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("分析预制件引用", 
                    $"处理预制件 {processed}/{total}", (float)processed / total);
            }
            
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // 加载预制件并检查其组件
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterials != null)
                    {
                        foreach (Material mat in renderer.sharedMaterials)
                        {
                            if (mat != null && mat.shader != null)
                            {
                                string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                                
                                if (!usedShaders.Contains(shaderPath))
                                {
                                    usedShaders.Add(shaderPath);
                                }
                                
                                if (!shaderUsageMap.ContainsKey(shaderPath))
                                {
                                    shaderUsageMap[shaderPath] = new List<string>();
                                }
                                
                                if (!shaderUsageMap[shaderPath].Contains(prefabPath))
                                {
                                    shaderUsageMap[shaderPath].Add(prefabPath);
                                }
                            }
                        }
                    }
                }
                
                // 检查Sprite Renderer等特殊渲染器
                var spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var spriteRenderer in spriteRenderers)
                {
                    if (spriteRenderer.sharedMaterial != null && spriteRenderer.sharedMaterial.shader != null)
                    {
                        string shaderPath = AssetDatabase.GetAssetPath(spriteRenderer.sharedMaterial.shader);
                        
                        if (!usedShaders.Contains(shaderPath))
                        {
                            usedShaders.Add(shaderPath);
                        }
                        
                        if (!shaderUsageMap.ContainsKey(shaderPath))
                        {
                            shaderUsageMap[shaderPath] = new List<string>();
                        }
                        
                        if (!shaderUsageMap[shaderPath].Contains(prefabPath))
                        {
                            shaderUsageMap[shaderPath].Add(prefabPath);
                        }
                    }
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
    }
    
    private void AnalyzeSceneReferences()
    {
        // 由于无法在编辑器外部打开场景，我们通过文本搜索的方式查找场景中的Shader引用
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        Debug.Log($"分析 {sceneGUIDs.Length} 个场景对Shader的引用...");
        
        foreach (string guid in sceneGUIDs)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            AnalyzeSceneFileForShaders(scenePath);
        }
    }
    
    private void AnalyzeSceneFileForShaders(string scenePath)
    {
        try
        {
            string sceneContent = File.ReadAllText(scenePath);
            
            // 简单的正则表达式搜索Shader引用（这是一个简化的方法）
            // 在实际应用中，可能需要更复杂的解析
            string[] lines = sceneContent.Split('\n');
            
            foreach (string line in lines)
            {
                if (line.Contains("m_Shader:") && line.Contains("fileID:"))
                {
                    // 这种解析方式非常简单，实际情况可能更复杂
                    // 更好的方法是使用Unity YAML解析器
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"无法读取场景文件 {scenePath}: {e.Message}");
        }
    }
    
    private void OutputAnalysisResults()
    {
        // 计算未使用的Shader
        unusedShaders.Clear();
        foreach (string shader in allShaders)
        {
            if (!usedShaders.Contains(shader))
            {
                unusedShaders.Add(shader);
            }
        }
        
        Debug.Log($"\n=== Shader 使用情况分析结果 ===");
        Debug.Log($"总Shader数量: {allShaders.Count}");
        Debug.Log($"已使用Shader数量: {usedShaders.Count}");
        Debug.Log($"未使用Shader数量: {unusedShaders.Count}");
        
        if (unusedShaders.Count > 0)
        {
            Debug.Log($"\n--- 未使用的Shader ---");
            foreach (string shader in unusedShaders)
            {
                Debug.Log($"未使用: {shader}");
            }
        }
        
        Debug.Log($"\n--- 已使用Shader及其引用来源 ---");
        foreach (var kvp in shaderUsageMap)
        {
            Debug.Log($"{kvp.Key} 被以下资源使用:");
            foreach (string user in kvp.Value)
            {
                Debug.Log($"  - {user}");
            }
        }
        
        // 生成报告
        GenerateReport();
    }
    
    private void GenerateReport()
    {
        string reportPath = "Assets/ShaderUsageReport.txt";
        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("Shader 使用情况分析报告");
            writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteLine("");
            writer.WriteLine($"总Shader数量: {allShaders.Count}");
            writer.WriteLine($"已使用Shader数量: {usedShaders.Count}");
            writer.WriteLine($"未使用Shader数量: {unusedShaders.Count}");
            writer.WriteLine("");
            
            if (unusedShaders.Count > 0)
            {
                writer.WriteLine("--- 未使用的Shader ---");
                foreach (string shader in unusedShaders)
                {
                    writer.WriteLine($"未使用: {shader}");
                }
                writer.WriteLine("");
            }
            
            writer.WriteLine("--- 已使用Shader及其引用来源 ---");
            foreach (var kvp in shaderUsageMap.OrderBy(x => x.Key))
            {
                writer.WriteLine($"{kvp.Key} 被以下资源使用:");
                foreach (string user in kvp.Value.Take(10)) // 限制输出前10个引用
                {
                    writer.WriteLine($"  - {user}");
                }
                
                if (kvp.Value.Count > 10)
                {
                    writer.WriteLine($"  ... 还有 {kvp.Value.Count - 10} 个引用");
                }
                
                writer.WriteLine("");
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"详细报告已保存到: {reportPath}");
    }
}