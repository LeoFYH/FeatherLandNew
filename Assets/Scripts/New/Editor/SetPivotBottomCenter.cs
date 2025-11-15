using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public class SetPivotBottomCenter
    {
        [MenuItem("Assets/Set Pivot to Bottom Center", true)]
        private static bool ValidateSetPivot()
        {
            // 确保只在编辑模式下运行
            if (Application.isPlaying)
                return false;

            if (Selection.objects.Length == 0)
                return false;

            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    return true;
            }

            return false;
        }

        [MenuItem("Assets/Set Pivot to Bottom Center")]
        private static void SetPivot()
        {
            // 确保只在编辑模式下运行
            if (Application.isPlaying)
            {
                Debug.LogError("此功能只能在编辑模式下使用，不能在游戏运行时调用！");
                return;
            }

            int processedCount = 0;
            int errorCount = 0;

            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"跳过无效路径的对象: {obj.name}");
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    Debug.LogWarning($"无法获取 {path} 的 TextureImporter，跳过。");
                    errorCount++;
                    continue;
                }

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    Debug.LogWarning($"忽略 {path}，不是 Sprite 类型。");
                    continue;
                }

                try
                {
                    if (importer.spriteImportMode == SpriteImportMode.Single)
                    {
                        // 设置单个Sprite的Pivot为Bottom Center (0.5, 0.0)
                        importer.spritePivot = new Vector2(0.5f, 0.0f);
                        importer.SaveAndReimport();
                        processedCount++;
                        Debug.Log($"[Single] 成功设置 {path} 的 Pivot 为 Bottom Center");
                    }
                    else if (importer.spriteImportMode == SpriteImportMode.Multiple)
                    {
                        // 设置图集中所有Sprite的Pivot
                        SpriteMetaData[] metas = importer.spritesheet;
                        if (metas != null && metas.Length > 0)
                        {
                            for (int i = 0; i < metas.Length; i++)
                            {
                                metas[i].pivot = new Vector2(0.5f, 0.0f);
                            }
                            importer.spritesheet = metas;
                            importer.SaveAndReimport();
                            processedCount++;
                            Debug.Log($"[Multiple] 成功设置 {path} 的所有 {metas.Length} 个 Sprite Pivot 为 Bottom Center");
                        }
                        else
                        {
                            Debug.LogWarning($"[Multiple] {path} 没有任何 Sprite，跳过。");
                            errorCount++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"不支持的 Sprite 导入模式: {importer.spriteImportMode} for {path}");
                        errorCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"处理 {path} 时发生错误: {e.Message}");
                    errorCount++;
                }
            }

            // 显示处理结果
            if (processedCount > 0)
            {
                Debug.Log($"✅ 成功处理了 {processedCount} 个文件");
            }
            if (errorCount > 0)
            {
                Debug.LogWarning($"⚠️ 处理过程中有 {errorCount} 个错误");
            }
            if (processedCount == 0 && errorCount == 0)
            {
                Debug.LogWarning("没有找到可处理的Sprite文件");
            }
        }

        // 添加一个工具菜单项，方便测试
        [MenuItem("Tools/Sprite/Set All Selected Sprites Pivot to Bottom Center")]
        private static void SetPivotFromTools()
        {
            SetPivot();
        }
    }
}