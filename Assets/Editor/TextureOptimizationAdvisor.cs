using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    /// <summary>
    /// 纹理优化建议工具 - 基于分析结果提供具体的优化建议
    /// </summary>
    public class TextureOptimizationAdvisor : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<TextureOptimizationSuggestion> suggestions = new List<TextureOptimizationSuggestion>();
        private bool isAnalyzing = false;
        private string statusMessage = "准备就绪";
        private int selectedCategory = 0;
        
        private string[] categories = { "全部", "可压缩", "尺寸过大", "格式优化", "Mipmap优化" }; // 增加更多类别

        [MenuItem("Tools/纹理优化建议 (Texture Optimization Advisor)")]
        public static void ShowWindow()
        {
            GetWindow<TextureOptimizationAdvisor>("纹理优化建议");
        }

        private void OnGUI()
        {
            GUILayout.Label("纹理优化建议", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // 分析按钮
            EditorGUI.BeginDisabledGroup(isAnalyzing);
            if (GUILayout.Button(isAnalyzing ? "正在分析..." : "生成优化建议"))
            {
                EditorApplication.delayCall += GenerateSuggestions;
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 类别筛选
            selectedCategory = EditorGUILayout.Popup("建议类别", selectedCategory, categories);
            
            EditorGUILayout.Space();
            
            // 批量操作
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用所有建议", GUILayout.Width(120)))
            {
                ApplyAllSuggestions();
            }
            if (GUILayout.Button("应用当前类别建议", GUILayout.Width(150)))
            {
                ApplyCategorySuggestions();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 状态信息
            GUILayout.Label(statusMessage, EditorStyles.helpBox);
            
            // 统计信息
            if (suggestions.Any())
            {
                var filteredSuggestions = GetFilteredSuggestions();
                GUILayout.Label($"优化建议: {filteredSuggestions.Count} 条", EditorStyles.boldLabel);
                
                int savings = filteredSuggestions.Sum(s => s.estimatedSavingsMB);
                if (savings > 0)
                {
                    GUILayout.Label($"预计节省空间: {savings} MB", EditorStyles.boldLabel);
                }
            }
            
            EditorGUILayout.Space();
            
            // 结果列表 - 只显示前50个以提高性能
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var displaySuggestions = GetFilteredSuggestions().Take(50).ToList();
            
            foreach (var suggestion in displaySuggestions)
            {
                DrawSuggestion(suggestion);
            }
            
            if (GetFilteredSuggestions().Count > 50)
            {
                GUILayout.Label($"还有 {GetFilteredSuggestions().Count - 50} 条建议未显示");
            }
            
            EditorGUILayout.EndScrollView();
        }

        private List<TextureOptimizationSuggestion> GetFilteredSuggestions()
        {
            return selectedCategory switch
            {
                0 => suggestions, // 全部
                1 => suggestions.Where(s => s.category == SuggestionCategory.Compressible).ToList(), // 可压缩
                2 => suggestions.Where(s => s.category == SuggestionCategory.TooLarge).ToList(), // 尺寸过大
                3 => suggestions.Where(s => s.category == SuggestionCategory.FormatOptimization).ToList(), // 格式优化
                4 => suggestions.Where(s => s.category == SuggestionCategory.MipmapOptimization).ToList(), // Mipmap优化
                _ => suggestions
            };
        }

        private void DrawSuggestion(TextureOptimizationSuggestion suggestion)
        {
            var backgroundColor = GUI.backgroundColor;
            
            // 根据重要性设置背景色
            GUI.backgroundColor = suggestion.importance switch
            {
                Importance.High => new Color(1f, 0.7f, 0.7f, 0.3f), // 高重要性 - 红色
                Importance.Medium => new Color(1f, 0.9f, 0.7f, 0.3f), // 中重要性 - 黄色
                _ => new Color(0.7f, 1f, 0.7f, 0.3f) // 低重要性 - 绿色
            };
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = backgroundColor;
            
            GUILayout.Label($"{suggestion.title} [{suggestion.category}]", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(suggestion.description, MessageType.Info);
            
            if (suggestion.actionable)
            {
                EditorGUILayout.HelpBox(suggestion.recommendation, MessageType.Warning);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"当前大小: {TextureAnalysisTool.FormatFileSize(suggestion.currentSize)}");
            if (suggestion.estimatedSavingsMB > 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"预计节省: {suggestion.estimatedSavingsMB} MB", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("在项目中高亮", GUILayout.Width(100)))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(suggestion.texturePath);
                EditorGUIUtility.PingObject(texture);
            }
            
            if (GUILayout.Button("显示详情", GUILayout.Width(80)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(suggestion.texturePath);
            }
            
            if (suggestion.actionable && GUILayout.Button("应用建议", GUILayout.Width(80)))
            {
                ApplySuggestion(suggestion);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void ApplySuggestion(TextureOptimizationSuggestion suggestion)
        {
            // 应用优化建议的逻辑
            switch (suggestion.category)
            {
                case SuggestionCategory.Compressible:
                    ApplyCompressionSettings(suggestion.texturePath);
                    break;
                case SuggestionCategory.TooLarge:
                    // 提示用户手动调整纹理尺寸
                    EditorUtility.DisplayDialog("提示", 
                        "纹理尺寸过大，建议手动调整纹理尺寸或使用纹理图集。\n\n" +
                        "可以通过纹理导入设置调整Max Size参数。", 
                        "确定");
                    break;
                case SuggestionCategory.FormatOptimization:
                    ApplyFormatOptimization(suggestion.texturePath);
                    break;
                case SuggestionCategory.MipmapOptimization:
                    ApplyMipmapSettings(suggestion.texturePath);
                    break;
            }
        }

        private void ApplyCompressionSettings(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.compressionQuality = 50;
                importer.SaveAndReimport();
                Debug.Log($"已为纹理 {Path.GetFileName(texturePath)} 启用压缩: {texturePath}");
                statusMessage = $"已为纹理 {Path.GetFileName(texturePath)} 启用压缩";
            }
        }

        private void ApplyFormatOptimization(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                // 检查是否真的需要alpha通道
                if (!importer.alphaIsTransparency)
                {
                    // 如果不需要alpha，设置为RGB格式
                    importer.alphaIsTransparency = false;
                    if (importer.textureType == TextureImporterType.Default)
                    {
                        // 对于不需要透明度的纹理，可以考虑使用RGB格式
                    }
                }
                importer.SaveAndReimport();
                Debug.Log($"已为纹理 {Path.GetFileName(texturePath)} 优化格式: {texturePath}");
                statusMessage = $"已为纹理 {Path.GetFileName(texturePath)} 优化格式";
            }
        }

        private void ApplyMipmapSettings(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                // 对于3D纹理启用Mipmap，UI纹理禁用Mipmap
                bool isUITexture = texturePath.ToLower().Contains("ui") || texturePath.ToLower().Contains("gui");
                importer.mipmapEnabled = !isUITexture;
                importer.SaveAndReimport();
                Debug.Log($"已为纹理 {Path.GetFileName(texturePath)} 调整Mipmap设置: {texturePath}");
                statusMessage = $"已为纹理 {Path.GetFileName(texturePath)} 调整Mipmap设置";
            }
        }

        private void ApplyAllSuggestions()
        {
            if (suggestions.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有建议可供应用", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("确认", 
                $"确定要应用所有 {suggestions.Count} 条建议吗？\n这将修改多个纹理的导入设置。", 
                "应用", "取消"))
            {
                int appliedCount = 0;
                foreach (var suggestion in suggestions)
                {
                    ApplySuggestion(suggestion);
                    appliedCount++;
                    
                    if (appliedCount % 10 == 0) // 每应用10个显示一次进度
                    {
                        EditorUtility.DisplayProgressBar("应用建议", 
                            $"正在应用建议... ({appliedCount}/{suggestions.Count})", 
                            (float)appliedCount / suggestions.Count);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                statusMessage = $"已应用 {appliedCount} 条建议";
                Debug.Log($"纹理优化: 已应用 {appliedCount} 条建议");
            }
        }

        private void ApplyCategorySuggestions()
        {
            var filteredSuggestions = GetFilteredSuggestions();
            if (filteredSuggestions.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前类别没有建议可供应用", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("确认", 
                $"确定要应用当前类别 {categories[selectedCategory]} 的 {filteredSuggestions.Count} 条建议吗？\n这将修改多个纹理的导入设置。", 
                "应用", "取消"))
            {
                int appliedCount = 0;
                foreach (var suggestion in filteredSuggestions)
                {
                    ApplySuggestion(suggestion);
                    appliedCount++;
                    
                    if (appliedCount % 10 == 0) // 每应用10个显示一次进度
                    {
                        EditorUtility.DisplayProgressBar("应用建议", 
                            $"正在应用建议... ({appliedCount}/{filteredSuggestions.Count})", 
                            (float)appliedCount / filteredSuggestions.Count);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                statusMessage = $"已应用 {appliedCount} 条{categories[selectedCategory]}建议";
                Debug.Log($"纹理优化: 已应用 {appliedCount} 条{categories[selectedCategory]}建议");
            }
        }

        private void GenerateSuggestions()
        {
            isAnalyzing = true;
            statusMessage = "正在生成优化建议...";
            Repaint();
            
            try
            {
                suggestions.Clear();
                
                // 获取所有纹理
                var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
                
                int processed = 0;
                int total = Math.Min(textureGUIDs.Length, 500); // 限制处理数量以提高性能
                
                // 只处理前500个纹理以避免长时间运行
                for (int i = 0; i < total; i++)
                {
                    var guid = textureGUIDs[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    
                    if (texture != null)
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        var analysisData = AnalyzeTextureForSuggestions(texture, path, importer);
                        suggestions.AddRange(analysisData);
                    }
                    
                    processed++;
                    if (processed % 20 == 0 || processed == total)
                    {
                        statusMessage = $"正在分析... ({processed}/{total})";
                        EditorUtility.DisplayProgressBar("优化建议", statusMessage, (float)processed / total);
                        Repaint();
                        
                        // 让出控制权给Unity主线程
                        if (processed % 100 == 0)
                        {
                            EditorApplication.delayCall += () => {};
                        }
                    }
                }
                
                // 去重
                suggestions = suggestions.Distinct(new SuggestionEqualityComparer()).ToList();
                
                statusMessage = $"建议生成完成! 共 {suggestions.Count} 条建议";
            }
            catch (Exception e)
            {
                statusMessage = $"生成建议出错: {e.Message}";
                Debug.LogError($"纹理优化建议生成出错: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isAnalyzing = false;
                Repaint();
            }
        }

        private List<TextureOptimizationSuggestion> AnalyzeTextureForSuggestions(Texture2D texture, string path, TextureImporter importer)
        {
            var suggestions = new List<TextureOptimizationSuggestion>();
            
            if (texture == null) return suggestions;
            
            // 检查纹理大小
            if (texture.width > 2048 || texture.height > 2048)
            {
                var suggestion = new TextureOptimizationSuggestion
                {
                    title = "纹理尺寸过大",
                    description = $"纹理 '{texture.name}' 尺寸为 {texture.width}x{texture.height}，远超必要大小。",
                    recommendation = "考虑将纹理尺寸缩小至2048x2048或更低，或者使用纹理图集。",
                    category = SuggestionCategory.TooLarge,
                    importance = Importance.High,
                    actionable = true,
                    texturePath = path,
                    currentSize = CalculateTextureMemory(texture),
                    estimatedSavingsMB = CalculatePotentialSavings(texture, SuggestionCategory.TooLarge)
                };
                suggestions.Add(suggestion);
            }
            
            // 检查纹理格式
            if (texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.ARGB32)
            {
                // 检查是否真的需要透明度
                bool hasAlphaChannel = HasAlphaChannel(texture);
                
                if (!hasAlphaChannel)
                {
                    var suggestion = new TextureOptimizationSuggestion
                    {
                        title = "格式优化建议",
                        description = $"纹理 '{texture.name}' 使用了RGBA32格式，但似乎不需要alpha通道。",
                        recommendation = "将纹理格式改为RGB24或使用适当的压缩格式，可以节省约25%的空间。",
                        category = SuggestionCategory.FormatOptimization,
                        importance = Importance.Medium,
                        actionable = true,
                        texturePath = path,
                        currentSize = CalculateTextureMemory(texture),
                        estimatedSavingsMB = CalculatePotentialSavings(texture, SuggestionCategory.FormatOptimization)
                    };
                    suggestions.Add(suggestion);
                }
            }
            
            // 检查是否可以使用压缩
            if (importer != null && importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                var suggestion = new TextureOptimizationSuggestion
                {
                    title = "启用纹理压缩",
                    description = $"纹理 '{texture.name}' 当前未启用压缩，占用了较多内存。",
                    recommendation = "启用纹理压缩（如ASTC、ETC2、DXT等），通常可节省50%-75%的空间。",
                    category = SuggestionCategory.Compressible,
                    importance = Importance.High,
                    actionable = true,
                    texturePath = path,
                    currentSize = CalculateTextureMemory(texture),
                    estimatedSavingsMB = CalculatePotentialSavings(texture, SuggestionCategory.Compressible)
                };
                suggestions.Add(suggestion);
            }
            
            // 检查Mipmap设置
            if (importer != null)
            {
                bool isUITexture = path.ToLower().Contains("ui") || path.ToLower().Contains("gui");
                if (isUITexture && importer.mipmapEnabled)
                {
                    // UI纹理通常不需要Mipmap
                    var suggestion = new TextureOptimizationSuggestion
                    {
                        title = "Mipmap优化建议",
                        description = $"UI纹理 '{texture.name}' 启用了Mipmap，这会增加内存使用。",
                        recommendation = "对于UI纹理，建议禁用Mipmap以节省内存。",
                        category = SuggestionCategory.MipmapOptimization,
                        importance = Importance.Medium,
                        actionable = true,
                        texturePath = path,
                        currentSize = CalculateTextureMemory(texture),
                        estimatedSavingsMB = CalculatePotentialSavings(texture, SuggestionCategory.MipmapOptimization)
                    };
                    suggestions.Add(suggestion);
                }
                else if (!isUITexture && !importer.mipmapEnabled)
                {
                    // 3D纹理建议启用Mipmap
                    var suggestion = new TextureOptimizationSuggestion
                    {
                        title = "Mipmap优化建议",
                        description = $"3D纹理 '{texture.name}' 未启用Mipmap，可能影响渲染质量。",
                        recommendation = "对于3D纹理，建议启用Mipmap以提高渲染质量和性能。",
                        category = SuggestionCategory.MipmapOptimization,
                        importance = Importance.Medium,
                        actionable = true,
                        texturePath = path,
                        currentSize = CalculateTextureMemory(texture),
                        estimatedSavingsMB = CalculatePotentialSavings(texture, SuggestionCategory.MipmapOptimization)
                    };
                    suggestions.Add(suggestion);
                }
            }
            
            return suggestions;
        }

        private bool HasAlphaChannel(Texture2D texture)
        {
            // 简单检测是否包含alpha信息
            // 在实际应用中，可以更深入地分析纹理数据
            return texture.format == TextureFormat.RGBA32 || 
                   texture.format == TextureFormat.ARGB32 ||
                   texture.format == TextureFormat.RGBAFloat;
        }

        private long CalculateTextureMemory(Texture2D texture)
        {
            int bpp = GetBitsPerPixel(texture.format);
            long sizeInBytes = (long)texture.width * texture.height * bpp / 8;
            
            // 考虑Mipmap
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
                case TextureFormat.Alpha8: return 8;
                case TextureFormat.ARGB4444:
                case TextureFormat.RGBA4444:
                case TextureFormat.BGRA32: return 16;
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32: return 32;
                case TextureFormat.RGB565: return 16;
                case TextureFormat.R16: return 16;
                case TextureFormat.DXT1: return 4;
                case TextureFormat.DXT5: return 8;
                case TextureFormat.RGBAFloat:
                    return 128;
                case TextureFormat.RGBAHalf:
                case TextureFormat.BC6H: return 64;
                case TextureFormat.BC7: return 8;
                default: return 32;
            }
        }

        private int CalculatePotentialSavings(Texture2D texture, SuggestionCategory category)
        {
            long currentSize = CalculateTextureMemory(texture);
            double compressionRatio = 0;
            
            switch (category)
            {
                case SuggestionCategory.Compressible:
                    compressionRatio = 0.5; // 压缩可节省约50%
                    break;
                case SuggestionCategory.TooLarge:
                    // 假设将纹理缩小到一半大小
                    compressionRatio = 0.75; // 例如从4096x4096到2048x2048节省75%
                    break;
                case SuggestionCategory.FormatOptimization:
                    compressionRatio = 0.25; // 格式优化节省约25%
                    break;
                case SuggestionCategory.MipmapOptimization:
                    // Mipmap优化通常节省少量内存
                    compressionRatio = 0.05; // 约5%的节省
                    break;
                default:
                    return 0;
            }
            
            long estimatedSavings = (long)(currentSize * compressionRatio);
            return (int)(estimatedSavings / (1024 * 1024)); // 转换为MB
        }
    }

    public enum SuggestionCategory
    {
        Compressible,
        TooLarge,
        FormatOptimization,
        MipmapOptimization
    }

    public enum Importance
    {
        Low,
        Medium,
        High
    }

    [Serializable]
    public class TextureOptimizationSuggestion
    {
        public string title;
        public string description;
        public string recommendation;
        public SuggestionCategory category;
        public Importance importance;
        public bool actionable;
        public string texturePath;
        public long currentSize;
        public int estimatedSavingsMB;
    }

    public class SuggestionEqualityComparer : IEqualityComparer<TextureOptimizationSuggestion>
    {
        public bool Equals(TextureOptimizationSuggestion x, TextureOptimizationSuggestion y)
        {
            if (x == null || y == null) return x == y;
            return x.title == y.title && x.texturePath == y.texturePath && x.category == y.category;
        }

        public int GetHashCode(TextureOptimizationSuggestion obj)
        {
            return HashCode.Combine(obj.title, obj.texturePath, obj.category);
        }
    }
}