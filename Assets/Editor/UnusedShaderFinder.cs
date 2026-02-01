using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text;

public class UnusedShaderFinder : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> allShaders = new List<string>();
    private List<string> usedShaders = new List<string>();
    private List<string> unusedShaders = new List<string>();
    private bool scanCompleted = false;
    private bool showUnusedOnly = true;
    private string scanStatus = "";
    
    [MenuItem("Tools/Shader/查找未使用的Shader")]
    public static void ShowWindow()
    {
        GetWindow<UnusedShaderFinder>("未使用的Shader查找器");
    }

    private void OnGUI()
    {
        GUILayout.Label("未使用的Shader查找器", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("扫描Shader"))
        {
            ScanForUnusedShaders();
        }

        if (!string.IsNullOrEmpty(scanStatus))
        {
            GUILayout.TextArea(scanStatus, GUILayout.Height(100));
        }

        if (scanCompleted)
        {
            GUILayout.Space(10);
            GUILayout.Label($"所有Shader数量: {allShaders.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"已使用Shader数量: {usedShaders.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"未使用Shader数量: {unusedShaders.Count}", EditorStyles.boldLabel);
            
            showUnusedOnly = GUILayout.Toggle(showUnusedOnly, "只显示未使用的Shader");
            
            if (GUILayout.Button("导出结果到CSV"))
            {
                ExportResultsToCSV();
            }
            
            if (unusedShaders.Count > 0)
            {
                if (GUILayout.Button($"删除所有({unusedShaders.Count})个未使用的Shader (谨慎操作!)"))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除{unusedShaders.Count}个未使用的Shader吗？此操作不可逆！", 
                        "确定删除", "取消"))
                    {
                        DeleteUnusedShaders();
                    }
                }
            }
            
            GUILayout.Space(10);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            
            var shadersToShow = showUnusedOnly ? unusedShaders : allShaders;
            
            for (int i = 0; i < shadersToShow.Count; i++)
            {
                var shaderName = shadersToShow[i];
                bool isUsed = usedShaders.Contains(shaderName);
                bool isUnused = unusedShaders.Contains(shaderName);
                
                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (isUnused)
                {
                    style.normal.textColor = Color.red;
                }
                else if (isUsed)
                {
                    style.normal.textColor = Color.green;
                }
                
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(shaderName, style);
                
                if (isUnused && GUILayout.Button("查看", GUILayout.Width(50)))
                {
                    // 尝试在Project窗口中定位到该Shader
                    FindAndSelectShader(shaderName);
                }
                
                if (isUnused && GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除Shader '{shaderName}' 吗？此操作不可逆！", 
                        "确定", "取消"))
                    {
                        DeleteSpecificShader(shaderName);
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
        }
    }

    private void ScanForUnusedShaders()
    {
        scanStatus = "开始扫描项目中的Shader...\n";
        Repaint();
        
        // 获取所有Shader资源
        allShaders.Clear();
        string[] shaderGUIDs = AssetDatabase.FindAssets("t:Shader");
        foreach (string guid in shaderGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string shaderName = path;
            allShaders.Add(shaderName);
        }
        
        scanStatus += $"找到 {allShaders.Count} 个Shader\n";

        // 扫描所有可能使用Shader的资源
        usedShaders.Clear();
        
        // 扫描材质球
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int materialCount = materialGUIDs.Length;
        scanStatus += $"扫描 {materialCount} 个材质球...\n";
        Repaint();
        
        for (int i = 0; i < materialGUIDs.Length; i++)
        {
            if (i % 50 == 0) // 每50个更新一次进度
            {
                EditorUtility.DisplayProgressBar("扫描材质球", $"正在扫描材质球 ({i}/{materialCount})", (float)i / materialCount);
                scanStatus += $"扫描材质球进度: {i}/{materialCount}\n";
                Repaint();
            }
            
            string path = AssetDatabase.GUIDToAssetPath(materialGUIDs[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null)
            {
                string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                
                if (!usedShaders.Contains(shaderPath))
                {
                    usedShaders.Add(shaderPath);
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // 查找Shader变体集合
        string[] shaderVariantCollectionGUIDs = AssetDatabase.FindAssets("t:ShaderVariantCollection");
        scanStatus += $"扫描 {shaderVariantCollectionGUIDs.Length} 个Shader变体集合...\n";
        Repaint();
        
        foreach (string guid in shaderVariantCollectionGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShaderVariantCollection svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
            if (svc != null)
            {
                // 注意：在编辑器外不能直接访问ShaderVariantCollection中的Shader
                // 这里只是记录文件存在
            }
        }
        
        // 计算未使用的Shader
        unusedShaders.Clear();
        foreach (string shader in allShaders)
        {
            if (!usedShaders.Contains(shader))
            {
                unusedShaders.Add(shader);
            }
        }
        
        scanStatus += $"扫描完成！\n";
        scanStatus += $"已使用Shader: {usedShaders.Count}\n";
        scanStatus += $"未使用Shader: {unusedShaders.Count}\n";
        
        Debug.Log($"扫描完成！已使用Shader: {usedShaders.Count}, 未使用Shader: {unusedShaders.Count}");
        scanCompleted = true;
    }

    private void ExportResultsToCSV()
    {
        string path = EditorUtility.SaveFilePanel("导出结果", "", "unused_shaders.csv", "csv");
        if (!string.IsNullOrEmpty(path))
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine("Shader路径,状态");
                foreach (string shader in allShaders)
                {
                    string status = unusedShaders.Contains(shader) ? "未使用" : "已使用";
                    writer.WriteLine($"\"{shader}\",\"{status}\"");
                }
            }
            Debug.Log($"结果已导出到: {path}");
            EditorUtility.DisplayDialog("导出完成", $"结果已导出到: {path}", "确定");
        }
    }

    private void DeleteUnusedShaders()
    {
        int deletedCount = 0;
        foreach (string shaderPath in unusedShaders.ToList())
        {
            if (DeleteShaderAtPath(shaderPath))
            {
                deletedCount++;
            }
        }
        EditorUtility.DisplayDialog("删除完成", $"成功删除 {deletedCount} 个未使用的Shader", "确定");
        scanCompleted = false; // 重新扫描
    }

    private bool DeleteShaderAtPath(string shaderPath)
    {
        if (File.Exists(Application.dataPath + shaderPath.Substring(6))) // 移除"Assets"前缀并添加正确的路径
        {
            AssetDatabase.DeleteAsset(shaderPath);
            Debug.Log($"已删除: {shaderPath}");
            return true;
        }
        else
        {
            // 尝试找到对应的.meta文件并删除
            string metaPath = shaderPath + ".meta";
            if (File.Exists(Application.dataPath + metaPath.Substring(6)))
            {
                AssetDatabase.DeleteAsset(metaPath);
            }
            Debug.LogWarning($"文件不存在: {shaderPath}");
        }
        return false;
    }

    private void DeleteSpecificShader(string shaderPath)
    {
        if (unusedShaders.Contains(shaderPath))
        {
            if (DeleteShaderAtPath(shaderPath))
            {
                unusedShaders.Remove(shaderPath);
                Debug.Log($"已删除: {shaderPath}");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "这个Shader仍在使用中，无法删除！", "确定");
        }
    }

    private void FindAndSelectShader(string shaderPath)
    {
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shaderPath);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
