using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SafeShaderCleaner : EditorWindow
{
    private List<string> unusedShaders = new List<string>();
    private Vector2 scrollPosition;
    private bool scanCompleted = false;
    private string statusMessage = "";
    private int selectedShaderIndex = -1;
    private bool confirmDelete = false;
    
    [MenuItem("Tools/Shader/安全Shader清理工具")]
    public static void ShowWindow()
    {
        GetWindow<SafeShaderCleaner>("安全Shader清理工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("安全Shader清理工具", EditorStyles.boldLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("1. 扫描未使用的Shader"))
        {
            ScanUnusedShaders();
        }

        if (scanCompleted)
        {
            GUILayout.Space(10);
            GUILayout.Label($"发现 {unusedShaders.Count} 个未使用的Shader", EditorStyles.boldLabel);
            
            if (unusedShaders.Count > 0)
            {
                GUILayout.Space(10);
                
                if (GUILayout.Button($"2. 预览未使用的Shader (点击查看详情)"))
                {
                    // 展示预览
                }
                
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                
                for (int i = 0; i < unusedShaders.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    bool isSelected = selectedShaderIndex == i;
                    if (GUILayout.Toggle(isSelected, $"", GUILayout.Width(20)))
                    {
                        selectedShaderIndex = i;
                    }
                    
                    GUILayout.Label(unusedShaders[i], GUILayout.ExpandWidth(true));
                    
                    if (GUILayout.Button("查看", GUILayout.Width(50)))
                    {
                        PreviewShader(unusedShaders[i]);
                    }
                    
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        if (EditorUtility.DisplayDialog("确认删除", 
                            $"确定要删除Shader '{unusedShaders[i]}' 吗？\n" +
                            $"路径: {unusedShaders[i]}\n" +
                            $"此操作不可撤销！", 
                            "确定删除", "取消"))
                        {
                            DeleteSingleShader(unusedShaders[i]);
                            unusedShaders.RemoveAt(i);
                            i--; // 修正索引
                        }
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndScrollView();
                
                GUILayout.Space(10);
                
                if (GUILayout.Button($"3. 删除所有 ({unusedShaders.Count}) 个未使用的Shader"))
                {
                    if (confirmDelete)
                    {
                        PerformBulkDelete();
                        confirmDelete = false;
                    }
                    else
                    {
                        confirmDelete = true;
                        statusMessage = "请再次点击按钮以确认批量删除操作";
                    }
                }
                
                if (GUILayout.Button("4. 导出清单到文本文件"))
                {
                    ExportShaderList();
                }
            }
            else
            {
                GUILayout.Label("未发现未使用的Shader", EditorStyles.helpBox);
            }
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(10);
            GUILayout.Label(statusMessage, EditorStyles.helpBox);
        }
    }

    private void ScanUnusedShaders()
    {
        statusMessage = "正在扫描项目中的未使用Shader...";
        Repaint();
        
        // 重用之前创建的分析器
        var analyzer = new ShaderUsageAnalyzer();
        analyzer.Analyze();
        
        // 重新获取未使用的Shader列表
        unusedShaders.Clear();
        string[] allShaderGUIDs = AssetDatabase.FindAssets("t:Shader");
        List<string> allShaders = new List<string>();
        
        foreach (string guid in allShaderGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allShaders.Add(path);
        }
        
        // 获取使用中的Shader
        List<string> usedShaders = new List<string>();
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        
        for (int i = 0; i < materialGUIDs.Length; i++)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGUIDs[i]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null && material.shader != null)
            {
                string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                if (!usedShaders.Contains(shaderPath))
                {
                    usedShaders.Add(shaderPath);
                }
            }
            
            if (i % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("扫描中", 
                    $"检查材质球 {i}/{materialGUIDs.Length}", (float)i / materialGUIDs.Length);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // 计算未使用的Shader
        foreach (string shader in allShaders)
        {
            if (!usedShaders.Contains(shader))
            {
                unusedShaders.Add(shader);
            }
        }
        
        statusMessage = $"扫描完成！共发现 {unusedShaders.Count} 个未使用的Shader";
        scanCompleted = true;
    }

    private void PreviewShader(string shaderPath)
    {
        Object shaderObj = AssetDatabase.LoadAssetAtPath<Object>(shaderPath);
        if (shaderObj != null)
        {
            Selection.activeObject = shaderObj;
            EditorGUIUtility.PingObject(shaderObj);
            statusMessage = $"已选中Shader: {shaderPath}";
        }
        else
        {
            statusMessage = $"无法找到Shader文件: {shaderPath}";
        }
    }

    private void DeleteSingleShader(string shaderPath)
    {
        if (AssetDatabase.DeleteAsset(shaderPath))
        {
            statusMessage = $"已删除Shader: {shaderPath}";
            AssetDatabase.Refresh();
        }
        else
        {
            statusMessage = $"删除失败: {shaderPath}";
        }
    }

    private void PerformBulkDelete()
    {
        int deletedCount = 0;
        
        for (int i = unusedShaders.Count - 1; i >= 0; i--)
        {
            if (AssetDatabase.DeleteAsset(unusedShaders[i]))
            {
                deletedCount++;
            }
            else
            {
                statusMessage = $"删除失败: {unusedShaders[i]}";
            }
            
            if (i % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("批量删除中", 
                    $"删除文件 {i}/{unusedShaders.Count}", (float)(unusedShaders.Count - i) / unusedShaders.Count);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        statusMessage = $"批量删除完成！删除了 {deletedCount} 个Shader文件";
        AssetDatabase.Refresh();
        unusedShaders.Clear();
        scanCompleted = false;
    }

    private void ExportShaderList()
    {
        string path = EditorUtility.SaveFilePanel("导出未使用Shader列表", "", "unused_shaders_list.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("项目中未使用的Shader列表");
                writer.WriteLine("===========================");
                writer.WriteLine($"生成时间: {DateTime.Now}");
                writer.WriteLine($"总计: {unusedShaders.Count} 个未使用的Shader");
                writer.WriteLine("");
                
                foreach (string shaderPath in unusedShaders)
                {
                    writer.WriteLine(shaderPath);
                }
            }
            
            statusMessage = $"列表已导出到: {path}";
            Debug.Log($"Shader列表已导出到: {path}");
        }
    }
}