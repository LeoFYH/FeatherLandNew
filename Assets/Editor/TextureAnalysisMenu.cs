using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    /// <summary>
    /// 纹理分析工具菜单 - 集成所有纹理分析功能
    /// </summary>
    public class TextureAnalysisMenu : MonoBehaviour
    {
        [MenuItem("Tools/纹理分析套件/_Main")]
        public static void Separator() { }

        [MenuItem("Tools/纹理分析套件/1. 纹理分析工具", false, 1)]
        public static void OpenTextureAnalysisTool()
        {
            TextureAnalysisTool.ShowWindow();
        }

        [MenuItem("Tools/纹理分析套件/2. 纹理优化建议", false, 2)]
        public static void OpenTextureOptimizationAdvisor()
        {
            TextureOptimizationAdvisor.ShowWindow();
        }

        [MenuItem("Tools/纹理分析套件/3. 纹理报告生成器", false, 3)]
        public static void OpenTextureReportGenerator()
        {
            TextureReportGenerator.ShowWindow();
        }

        [MenuItem("Tools/纹理分析套件/4. 纹理优化最佳实践", false, 4)]
        public static void ShowBestPractices()
        {
            ShowOptimizationBestPractices();
        }

        private static void ShowOptimizationBestPractices()
        {
            var practices = @"
# Unity纹理优化最佳实践

## 1. 纹理压缩
- 使用平台推荐的压缩格式（ASTC, ETC2, DXT等）
- 非透明纹理使用RGB格式，透明纹理使用RGBA格式
- 避免在移动平台上使用未压缩的纹理

## 2. 纹理尺寸
- 遵循2的幂次方原则（如1024x1024, 2048x2048）
- UI纹理使用合适的尺寸，避免过度放大
- 使用纹理图集减少Draw Call

## 3. Mipmap设置
- 3D模型纹理启用Mipmap以提高渲染性能
- UI纹理通常禁用Mipmap以节省内存

## 4. Alpha通道优化
- 不需要透明度的纹理使用RGB格式
- 仅需要纯黑/纯白透明度的纹理使用Alpha8格式

## 5. 纹理类型设置
- UI纹理：设置为Sprite或Advanced类型
- 3D模型纹理：设置为Default类型
- 法线贴图：设置为Normal Map类型

## 6. 内存管理
- 使用Addressables系统管理纹理资源
- 实现纹理加载和卸载的引用计数机制
- 避免纹理常驻内存

## 7. 纹理图集
- 将相关UI元素合并到纹理图集
- 使用Unity的Sprite Atlas功能
- 合理安排图集大小（通常2048x2048或4096x4096）

## 8. 平台差异化
- 为不同平台配置不同的纹理压缩设置
- 在构建时自动应用最优纹理设置
- 考虑设备性能差异调整纹理质量

## 9. 性能提示
- 本工具在处理大型项目时会显示进度条
- 为提高性能，工具会限制分析的纹理数量
- 如需分析全部纹理，请多次运行或分目录分析

## 10. 一键优化
- 使用纹理分析工具的""应用推荐设置""按钮快速应用建议
- 使用纹理优化建议工具的批量应用功能
- 生成报告后可按建议逐步优化纹理资源";
            EditorUtility.DisplayDialog("纹理优化最佳实践", practices, "确定");
        }
    }
}