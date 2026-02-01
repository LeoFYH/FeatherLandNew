using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    /// <summary>
    /// 纹理报告生成器 - 生成详细的纹理分析报告
    /// </summary>
    public class TextureReportGenerator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<TextureAnalysisData> analysisResults = new List<TextureAnalysisData>();  // 使用与TextureAnalysisTool中相同的类型
        private bool isGenerating = false;
        private string statusMessage = "准备就绪";
        private string reportContent = "";
        private Vector2 reportScrollPosition;
        
        // 报告选项
        private bool includeDetailedInfo = true;
        private bool sortBySize = true;
        private bool includeCompressibility = true;
        private bool includeRecommendations = true; // 添加建议部分

        [MenuItem("Tools/纹理报告生成器 (Texture Report Generator)")]
        public static void ShowWindow()
        {
            GetWindow<TextureReportGenerator>("纹理报告生成器");
        }

        private void OnGUI()
        {
            GUILayout.Label("纹理报告生成器", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // 选项
            includeDetailedInfo = EditorGUILayout.Toggle("包含详细信息", includeDetailedInfo);
            sortBySize = EditorGUILayout.Toggle("按大小排序", sortBySize);
            includeCompressibility = EditorGUILayout.Toggle("包含压缩建议", includeCompressibility);
            includeRecommendations = EditorGUILayout.Toggle("包含优化建议", includeRecommendations);
            
            EditorGUILayout.Space();
            
            // 生成按钮
            EditorGUI.BeginDisabledGroup(isGenerating);
            if (GUILayout.Button(isGenerating ? "正在生成报告..." : "生成纹理报告"))
            {
                EditorApplication.delayCall += GenerateReport; // 使用delayCall避免UI冻结
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 导出按钮
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(reportContent));
            if (GUILayout.Button("导出报告到文件"))
            {
                ExportReportToFile();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 状态信息
            GUILayout.Label(statusMessage, EditorStyles.helpBox);
            
            // 报告预览
            if (!string.IsNullOrEmpty(reportContent))
            {
                GUILayout.Label("报告预览:", EditorStyles.boldLabel);
                reportScrollPosition = EditorGUILayout.BeginScrollView(reportScrollPosition, GUILayout.Height(300));
                EditorGUILayout.SelectableLabel(reportContent, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void GenerateReport()
        {
            isGenerating = true;
            statusMessage = "正在生成纹理报告...";
            Repaint();
            
            try
            {
                analysisResults.Clear();
                
                // 获取所有纹理资源
                var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
                
                int processed = 0;
                int total = Math.Min(textureGUIDs.Length, 300); // 限制处理数量以提高性能
                
                // 只处理前300个纹理以避免长时间运行
                for (int i = 0; i < total; i++)
                {
                    var guid = textureGUIDs[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    
                    if (texture != null)
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        var data = AnalyzeTextureForReport(texture, path, guid, importer);
                        if (data != null)
                        {
                            analysisResults.Add(data);
                        }
                    }
                    
                    processed++;
                    if (processed % 20 == 0 || processed == total)
                    {
                        statusMessage = $"正在分析纹理... ({processed}/{total})";
                        EditorUtility.DisplayProgressBar("生成报告", statusMessage, (float)processed / total);
                        Repaint(); // 强制刷新UI
                        
                        // 让出控制权给Unity主线程
                        if (processed % 100 == 0)
                        {
                            EditorApplication.delayCall += () => {};
                        }
                    }
                }
                
                // 生成报告内容
                reportContent = BuildReportContent();
                
                statusMessage = $"报告生成完成! 分析了 {analysisResults.Count} 个纹理";
            }
            catch (Exception e)
            {
                statusMessage = $"生成报告出错: {e.Message}";
                Debug.LogError($"纹理报告生成出错: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isGenerating = false;
                Repaint();
            }
        }

        private TextureAnalysisData AnalyzeTextureForReport(Texture2D texture, string path, string guid, TextureImporter importer)
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

        private string BuildReportContent()
        {
            if (!analysisResults.Any()) return "没有找到纹理资源";
            
            var report = new System.Text.StringBuilder();
            
            // 报告头部
            report.AppendLine("# 纹理分析报告");
            report.AppendLine();
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"项目路径: {Application.dataPath}");
            report.AppendLine($"纹理总数: {analysisResults.Count}");
            report.AppendLine($"总内存占用: {TextureAnalysisTool.FormatFileSize(analysisResults.Sum(r => r.memoryUsage))}");
            report.AppendLine();
            
            // 统计摘要
            report.AppendLine("## 统计摘要");
            var largestTexture = analysisResults.OrderByDescending(r => r.memoryUsage).FirstOrDefault();
            if (largestTexture != null)
            {
                report.AppendLine($"- 最大纹理: {largestTexture.name} ({largestTexture.width}x{largestTexture.height}, {TextureAnalysisTool.FormatFileSize(largestTexture.memoryUsage)})");
            }
            
            var uncompressedTextures = analysisResults.Count(r => r.compressionType == "Uncompressed");
            report.AppendLine($"- 未压缩纹理: {uncompressedTextures} 个");
            
            var largeTextures = analysisResults.Count(r => r.width > 2048 || r.height > 2048);
            report.AppendLine($"- 超大纹理 (2048+): {largeTextures} 个");
            
            var texturesWithAlpha = analysisResults.Count(r => 
                r.format == TextureFormat.RGBA32 || r.format == TextureFormat.ARGB32 || 
                r.format == TextureFormat.RGBAFloat);
            report.AppendLine($"- 包含Alpha通道: {texturesWithAlpha} 个");
            
            report.AppendLine();
            
            // 排序
            var sortedResults = sortBySize ? 
                analysisResults.OrderByDescending(r => r.memoryUsage) : 
                analysisResults.OrderBy(r => r.name);
            
            // 详细列表 - 限制显示数量以提高性能
            report.AppendLine("## 纹理详细列表 (前50个)");
            report.AppendLine("| 名称 | 尺寸 | 格式 | 内存占用 | 压缩 | Mipmap | Alpha | 路径 | 优化建议 |");
            report.AppendLine("|------|------|------|----------|------|--------|-------|------|--------|");
            
            foreach (var data in sortedResults.Take(50)) // 限制显示前50个
            {
                var compressibility = includeCompressibility ? 
                    GetCompressibilityEstimate(data) : "";
                
                // 生成优化建议
                var recommendations = GenerateRecommendations(data);
                var recommendationText = string.Join("<br>", recommendations);
                
                report.AppendLine($"| {data.name} | {data.width}×{data.height} | {data.format} | {TextureAnalysisTool.FormatFileSize(data.memoryUsage)} | {data.compressionType} | {(data.mipmapEnabled ? "✓" : "✗")} | {(data.alphaIsTransparency ? "✓" : "✗")} | {data.path.Replace("Assets/", "")} | {recommendationText} |");
            }
            
            if (analysisResults.Count > 50)
            {
                report.AppendLine($"\\*\\*注:\\*\\* 仅显示前50个纹理，完整列表请参考导出文件。");
            }
            
            report.AppendLine();
            
            // 压缩建议
            if (includeCompressibility)
            {
                report.AppendLine("## 压缩建议");
                
                var uncompressibleTextures = analysisResults.Where(r => r.compressionType == "Uncompressed").ToList();
                if (uncompressibleTextures.Any())
                {
                    report.AppendLine("### 未压缩纹理 (前10个)");
                    report.AppendLine("以下纹理当前未启用压缩，建议启用以节省内存：");
                    
                    foreach (var tex in uncompressibleTextures.Take(10))
                    {
                        report.AppendLine($"- **{tex.name}**: {TextureAnalysisTool.FormatFileSize(tex.memoryUsage)} ({tex.width}×{tex.height}) - 路径: {tex.path}");
                    }
                    
                    if (uncompressibleTextures.Count > 10)
                    {
                        report.AppendLine($"\\*\\*注:\\*\\* 还有 {uncompressibleTextures.Count - 10} 个未压缩纹理。");
                    }
                    
                    var potentialSavings = uncompressibleTextures.Sum(r => r.memoryUsage) / 2; // 假设压缩可节省50%
                    report.AppendLine($"\\*\\*预估总节省: {TextureAnalysisTool.FormatFileSize(potentialSavings)}\\*\\*");
                    report.AppendLine();
                }
                
                var largeTexturesList = analysisResults.Where(r => r.width > 2048 || r.height > 2048).OrderByDescending(r => r.memoryUsage).ToList();
                if (largeTexturesList.Any())
                {
                    report.AppendLine("### 大尺寸纹理 (前10个)");
                    report.AppendLine("以下纹理尺寸过大，考虑缩小尺寸以节省内存：");
                    
                    foreach (var tex in largeTexturesList.Take(10))
                    {
                        report.AppendLine($"- **{tex.name}**: {TextureAnalysisTool.FormatFileSize(tex.memoryUsage)} ({tex.width}×{tex.height}) - 路径: {tex.path}");
                    }
                    
                    if (largeTexturesList.Count > 10)
                    {
                        report.AppendLine($"\\*\\*注:\\*\\* 还有 {largeTexturesList.Count - 10} 个大尺寸纹理。");
                    }
                    
                    report.AppendLine();
                }
            }
            
            // 优化建议
            if (includeRecommendations)
            {
                report.AppendLine("## 详细优化建议");
                
                // 统计各类问题
                var uncompressedCount = analysisResults.Count(r => r.compressionType == "Uncompressed");
                var largeTextureCount = analysisResults.Count(r => r.width > 2048 || r.height > 2048);
                var alphaWithoutNeedCount = analysisResults.Count(r => 
                    (r.format == TextureFormat.RGBA32 || r.format == TextureFormat.ARGB32) && 
                    !r.alphaIsTransparency);
                var mipmapIssueCount = analysisResults.Count(r => 
                    (r.path.ToLower().Contains("ui") || r.path.ToLower().Contains("gui")) && 
                    r.mipmapEnabled); // UI纹理启用了Mipmap
                
                report.AppendLine($"- **压缩优化**: {uncompressedCount} 个纹理未启用压缩，建议启用以节省内存");
                report.AppendLine($"- **尺寸优化**: {largeTextureCount} 个纹理尺寸过大，考虑缩小或使用图集");
                report.AppendLine($"- **格式优化**: {alphaWithoutNeedCount} 个纹理使用了带透明通道的格式但实际不需要");
                report.AppendLine($"- **Mipmap优化**: {mipmapIssueCount} 个UI纹理启用了Mipmap，建议关闭以节省内存");
                
                report.AppendLine();
                
                // 按严重程度列出问题纹理
                report.AppendLine("### 高优先级优化目标 (内存占用 > 1MB)");
                var highPriorityTextures = analysisResults.Where(r => r.memoryUsage > 1024 * 1024).OrderByDescending(r => r.memoryUsage).ToList();
                
                if (highPriorityTextures.Any())
                {
                    foreach (var tex in highPriorityTextures.Take(10))
                    {
                        var recs = GenerateRecommendations(tex);
                        report.AppendLine($"1. **{tex.name}** - {TextureAnalysisTool.FormatFileSize(tex.memoryUsage)}");
                        foreach (var rec in recs)
                        {
                            report.AppendLine($"   - {rec}");
                        }
                    }
                }
                else
                {
                    report.AppendLine("没有高内存占用的纹理。");
                }
                
                report.AppendLine();
            }
            
            // 通用优化建议
            report.AppendLine("## 通用优化建议");
            report.AppendLine("- 对于UI纹理，考虑使用纹理图集减少Draw Call");
            report.AppendLine("- 对于3D模型纹理，启用Mipmap以提高渲染性能");
            report.AppendLine("- 对于不需要Alpha通道的纹理，使用RGB格式而非RGBA");
            report.AppendLine("- 考虑为移动端使用更高效的压缩格式（如ASTC、ETC2）");
            report.AppendLine("- 定期审查未使用的纹理资源");
            report.AppendLine("- 使用Addressables系统管理纹理资源生命周期");
            
            return report.ToString();
        }

        private List<string> GenerateRecommendations(TextureAnalysisData data)
        {
            var recommendations = new List<string>();
            
            // 检查是否未压缩
            if (data.compressionType == "Uncompressed")
            {
                recommendations.Add("启用纹理压缩");
            }
            
            // 检查是否过大
            if (data.width > 2048 || data.height > 2048)
            {
                recommendations.Add("纹理尺寸过大，考虑缩小或使用图集");
            }
            
            // 检查格式是否合适
            if ((data.format == TextureFormat.RGBA32 || data.format == TextureFormat.ARGB32) && !data.alphaIsTransparency)
            {
                recommendations.Add("使用带透明通道格式但实际不需要，考虑使用RGB格式");
            }
            
            // 检查Mipmap设置是否合适
            if ((data.path.ToLower().Contains("ui") || data.path.ToLower().Contains("gui")) && data.mipmapEnabled)
            {
                recommendations.Add("UI纹理启用了Mipmap，建议关闭以节省内存");
            }
            
            // 检查是否为3D纹理但未启用Mipmap
            if (!data.path.ToLower().Contains("ui") && !data.path.ToLower().Contains("gui") && !data.mipmapEnabled)
            {
                recommendations.Add("3D纹理建议启用Mipmap以提高渲染质量");
            }
            
            return recommendations.Count > 0 ? recommendations : new List<string> { "无明显优化建议" };
        }

        private string GetCompressibilityEstimate(TextureAnalysisData data)
        {
            var estimates = new List<string>();
            
            if (data.compressionType == "Uncompressed")
            {
                estimates.Add("可压缩(50-75%)");
            }
            
            if (data.width > 2048 || data.height > 2048)
            {
                estimates.Add("可缩小尺寸");
            }
            
            return string.Join(", ", estimates);
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

        private void ExportReportToFile()
        {
            var path = EditorUtility.SaveFilePanel(
                "保存纹理分析报告",
                "",
                $"TextureAnalysisReport_{DateTime.Now:yyyyMMdd_HHmmss}.md",
                "md");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    File.WriteAllText(path, reportContent);
                    statusMessage = $"报告已保存到: {path}";
                    
                    // 在资源管理器中高亮文件
                    EditorUtility.RevealInFinder(path);
                }
                catch (Exception e)
                {
                    statusMessage = $"保存失败: {e.Message}";
                    Debug.LogError($"保存纹理报告失败: {e}");
                }
            }
        }
    }
}