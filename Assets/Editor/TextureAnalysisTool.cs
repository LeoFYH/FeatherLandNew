using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    /// <summary>
    /// 纹理分析工具 - 分析项目中的纹理资源使用情况
    /// </summary>
    public class TextureAnalysisTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<TextureAnalysisData> analysisResults = new List<TextureAnalysisData>();
        private bool isAnalyzing = false;
        private string statusMessage = "准备就绪";
        
        // 过滤选项
        private bool showOnlyLargeTextures = false;
        private int largeTextureThreshold = 1024; // 大纹理阈值，默认1024x1024
        private bool showOnlyUncompressed = false; // 仅显示未压缩纹理
        private bool showOnlyHighRes = false; // 仅显示高分辨率纹理
        
        // 分析统计
        private int totalTextures = 0;
        private long totalMemoryUsage = 0;
        private int texturesOverThreshold = 0;

        [MenuItem("Tools/纹理分析工具 (Texture Analysis Tool)")]
        public static void ShowWindow()
        {
            GetWindow<TextureAnalysisTool>("纹理分析工具");
        }

        private void OnGUI()
        {
            GUILayout.Label("纹理分析工具", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // 分析按钮
            EditorGUI.BeginDisabledGroup(isAnalyzing);
            if (GUILayout.Button(isAnalyzing ? "正在分析..." : "开始分析纹理"))
            {
                EditorApplication.delayCall += AnalyzeTextures; // 使用delayCall避免UI冻结
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 过滤选项
            showOnlyLargeTextures = EditorGUILayout.Toggle("仅显示大纹理", showOnlyLargeTextures);
            if (showOnlyLargeTextures)
            {
                largeTextureThreshold = EditorGUILayout.IntField($"大纹理阈值 (像素)", largeTextureThreshold);
                largeTextureThreshold = Mathf.Max(1, largeTextureThreshold);
            }
            
            showOnlyUncompressed = EditorGUILayout.Toggle("仅显示未压缩纹理", showOnlyUncompressed);
            showOnlyHighRes = EditorGUILayout.Toggle("仅显示高分辨率纹理 (>2048)", showOnlyHighRes);
            
            EditorGUILayout.Space();
            
            // 状态信息
            GUILayout.Label(statusMessage, EditorStyles.helpBox);
            
            // 统计信息
            if (analysisResults.Any())
            {
                GUILayout.Label($"总计: {totalTextures} 个纹理", EditorStyles.boldLabel);
                GUILayout.Label($"总内存占用: {FormatFileSize(totalMemoryUsage)}", EditorStyles.boldLabel);
                GUILayout.Label($"超过阈值纹理: {texturesOverThreshold} 个", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.Space();
            
            // 结果列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var displayResults = analysisResults.AsEnumerable();
            
            if (showOnlyLargeTextures)
            {
                displayResults = displayResults.Where(t => t.width >= largeTextureThreshold || t.height >= largeTextureThreshold);
            }
            
            if (showOnlyUncompressed)
            {
                displayResults = displayResults.Where(t => t.compressionType == "Uncompressed");
            }
            
            if (showOnlyHighRes)
            {
                displayResults = displayResults.Where(t => t.width > 2048 || t.height > 2048);
            }
            
            // 只显示前100个结果以提高性能
            foreach (var data in displayResults.Take(100))
            {
                DrawTextureInfo(data);
            }
            
            if (displayResults.Count() > 100)
            {
                GUILayout.Label($"还有 {displayResults.Count() - 100} 个纹理未显示，使用过滤器查看特定纹理");
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawTextureInfo(TextureAnalysisData data)
        {
            var backgroundColor = GUI.backgroundColor;
            
            // 根据内存使用情况设置背景色
            if (data.memoryUsage > 2 * 1024 * 1024) // 超过2MB
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.3f); // 红色背景 - 高优先级优化
            }
            else if (data.memoryUsage > 1024 * 1024) // 超过1MB
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.5f, 0.3f); // 黄色背景 - 中优先级优化
            }
            else if (data.memoryUsage > 512 * 1024) // 超过512KB
            {
                GUI.backgroundColor = new Color(1f, 0.9f, 0.7f, 0.3f); // 橙色背景 - 低优先级优化
            }
            else
            {
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f, 0.3f); // 绿色背景 - 无需优化
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = backgroundColor;
            
            GUILayout.Label(data.name, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"尺寸: {data.width} x {data.height}");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"格式: {data.format}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"内存: {FormatFileSize(data.memoryUsage)}");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"路径: {data.path}");
            EditorGUILayout.EndHorizontal();
            
            // 显示纹理导入设置
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"压缩: {data.compressionType}");
            GUILayout.Space(10);
            GUILayout.Label($"Mipmap: {(data.mipmapEnabled ? "开启" : "关闭")}");
            GUILayout.Space(10);
            GUILayout.Label($"Alpha: {(data.alphaIsTransparency ? "透明" : "不透明")}");
            EditorGUILayout.EndHorizontal();
            
            // 优化建议
            var suggestions = GetDetailedCompressionSuggestions(data);
            if (suggestions.Any())
            {
                foreach (var suggestion in suggestions)
                {
                    EditorGUILayout.HelpBox(suggestion, MessageType.Warning);
                }
                
                // 添加应用建议按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("应用推荐设置", GUILayout.Width(120)))
                {
                    ApplyRecommendedSettings(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("在项目中高亮", GUILayout.Width(120)))
            {
                HighlightTextureInProject(data.guid);
            }
            
            if (GUILayout.Button("纹理信息", GUILayout.Width(90)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(data.path);
            }
            
            if (GUILayout.Button("编辑导入设置", GUILayout.Width(100)))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(data.path);
                Selection.activeObject = texture;
                EditorGUIUtility.PingObject(texture);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private List<string> GetDetailedCompressionSuggestions(TextureAnalysisData data)
        {
            var suggestions = new List<string>();
            
            // 检查是否是大纹理
            if (data.width > 2048 || data.height > 2048)
            {
                suggestions.Add($"⚠️ 纹理过大: {data.width}x{data.height}，建议缩小至2048x2048或使用纹理图集");
            }
            
            // 检查纹理格式
            if (data.format.ToString().Contains("RGBA32") || data.format.ToString().Contains("ARGB32"))
            {
                if (!data.path.ToLower().Contains("normal")) // 排除法线贴图
                {
                    // 检查是否真的需要alpha通道
                    if (!data.alphaIsTransparency)
                    {
                        suggestions.Add($"💡 使用高精度格式但无透明通道，建议改为RGB24格式");
                    }
                    else
                    {
                        suggestions.Add($"💡 考虑使用更高效的压缩格式");
                    }
                }
            }
            
            // 检查是否未压缩
            if (data.compressionType == "Uncompressed")
            {
                suggestions.Add($"🔥 未启用压缩，强烈建议启用纹理压缩以节省内存");
            }
            
            // 检查Mipmap设置
            if (!data.mipmapEnabled && !data.path.ToLower().Contains("ui") && !data.path.ToLower().Contains("gui"))
            {
                suggestions.Add($"ℹ️ 非UI纹理建议启用Mipmap以提高渲染性能");
            }
            
            return suggestions;
        }

        private void ApplyRecommendedSettings(TextureAnalysisData data)
        {
            var importer = AssetImporter.GetAtPath(data.path) as TextureImporter;
            if (importer == null) return;
            
            bool needsReimport = false;
            
            // 根据纹理类型应用不同设置
            if (data.path.ToLower().Contains("ui") || data.path.ToLower().Contains("gui"))
            {
                // UI纹理设置
                if (importer.textureType != TextureImporterType.Sprite && importer.textureType != TextureImporterType.GUI)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsReimport = true;
                }
                
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false; // UI纹理通常不需要Mipmap
                    needsReimport = true;
                }
            }
            else
            {
                // 普通纹理设置
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true; // 3D纹理建议启用Mipmap
                    needsReimport = true;
                }
            }
            
            // 应用压缩设置
            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                needsReimport = true;
            }
            
            // 根据是否需要alpha通道设置格式
            if (!data.alphaIsTransparency)
            {
                if (importer.textureType == TextureImporterType.Default)
                {
                    importer.alphaIsTransparency = false;
                    needsReimport = true;
                }
            }
            
            if (needsReimport)
            {
                importer.SaveAndReimport();
                statusMessage = $"已应用推荐设置到: {data.name}";
                Debug.Log($"已为纹理 {data.name} 应用推荐设置: {data.path}");
            }
            else
            {
                statusMessage = $"纹理 {data.name} 已经使用推荐设置";
            }
        }

        private void HighlightTextureInProject(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorGUIUtility.PingObject(texture);
        }

        private void AnalyzeTextures()
        {
            isAnalyzing = true;
            statusMessage = "正在扫描纹理...";
            Repaint();
            
            try
            {
                analysisResults.Clear();
                
                // 仅扫描纹理资源，避免扫描Prefab和Material以提高性能
                var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
                
                int processed = 0;
                int total = textureGUIDs.Length;
                
                // 分批处理以避免UI冻结
                for (int i = 0; i < textureGUIDs.Length; i++)
                {
                    var guid = textureGUIDs[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    
                    if (texture != null)
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        var analysisData = AnalyzeTexture(texture, path, guid, importer);
                        if (analysisData != null)
                        {
                            analysisResults.Add(analysisData);
                        }
                    }
                    
                    processed++;
                    
                    // 每处理50个纹理更新一次UI和进度条
                    if (processed % 50 == 0 || processed == total)
                    {
                        statusMessage = $"正在分析纹理... ({processed}/{total})";
                        EditorUtility.DisplayProgressBar("纹理分析", statusMessage, (float)processed / total);
                        Repaint(); // 强制刷新UI
                        
                        // 让出控制权给Unity主线程，防止UI冻结
                        if (processed % 200 == 0)
                        {
                            EditorApplication.delayCall += () => {};
                        }
                    }
                }
                
                // 计算统计数据
                CalculateStatistics();
                
                statusMessage = $"分析完成! 找到 {analysisResults.Count} 个纹理";
            }
            catch (Exception e)
            {
                statusMessage = $"分析出错: {e.Message}";
                Debug.LogError($"纹理分析出错: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isAnalyzing = false;
                Repaint();
            }
        }

        private TextureAnalysisData AnalyzeTexture(Texture2D texture, string path, string guid, TextureImporter importer = null)
        {
            if (texture == null) return null;
            
            var data = new TextureAnalysisData
            {
                name = texture.name,
                path = path,
                guid = guid,
                width = texture.width,
                height = texture.height,
                format = texture.format,
                memoryUsage = CalculateTextureMemory(texture)
            };
            
            // 添加额外信息
            if (importer != null)
            {
                data.compressionType = importer.textureCompression.ToString();
                data.mipmapEnabled = importer.mipmapEnabled;
                data.alphaIsTransparency = importer.alphaIsTransparency;
            }
            
            return data;
        }

        private long CalculateTextureMemory(Texture2D texture)
        {
            // 根据纹理格式估算内存使用
            int bpp = GetBitsPerPixel(texture.format);
            long sizeInBytes = (long)texture.width * texture.height * bpp / 8;
            
            // 考虑Mipmap（如果有）
            if (texture.mipmapCount > 1)
            {
                long mipmapSize = sizeInBytes;
                int width = texture.width;
                int height = texture.height;
                
                while (width > 1 || height > 1)
                {
                    width = Mathf.Max(1, width / 2);
                    height = Mathf.Max(1, height / 2);
                    mipmapSize += (long)width * height * bpp / 8;
                }
                
                sizeInBytes = mipmapSize;
            }
            
            return sizeInBytes;
        }

        private int GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                    return 8;
                case TextureFormat.ARGB4444:
                case TextureFormat.RGBA4444:
                case TextureFormat.BGRA32:
                    return 16;
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                    return 32;
                case TextureFormat.RGB565:
                    return 16;
                case TextureFormat.R16:
                    return 16;
                case TextureFormat.DXT1:
                    return 4; // 压缩格式
                case TextureFormat.DXT5:
                    return 8; // 压缩格式
                case TextureFormat.RGBAFloat:
                    return 128; // 每像素16字节 * 4通道
                case TextureFormat.RGBAHalf:
                case TextureFormat.BC6H:
                    return 64; // 每像素8字节 * 4通道
                case TextureFormat.BC7:
                    return 8; // 压缩格式
                default:
                    return 32; // 默认按32位计算
            }
        }

        private void CalculateStatistics()
        {
            totalTextures = analysisResults.Count;
            totalMemoryUsage = analysisResults.Sum(r => r.memoryUsage);
            texturesOverThreshold = analysisResults.Count(r => r.width >= largeTextureThreshold || r.height >= largeTextureThreshold);
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// 纹理分析数据
    /// </summary>
    [Serializable]
    public class TextureAnalysisData
    {
        public string name;
        public string path;
        public string guid;
        public int width;
        public int height;
        public TextureFormat format;
        public long memoryUsage;
        
        // 额外信息
        public string compressionType = "Unknown";
        public bool mipmapEnabled = false;
        public bool alphaIsTransparency = false;
    }
}