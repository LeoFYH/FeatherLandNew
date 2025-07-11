using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BirdGame.Editor
{

    public class SpriteAnchorEditor : MonoBehaviour
    {
        [MenuItem("Assets/Set Sprite Anchor/Bottom-Center", true)]
        private static bool ValidateSetAnchor()
        {
            return Selection.objects.Any(obj =>
                obj is Texture2D ||
                (obj is Sprite && AssetDatabase.GetAssetPath(obj) != null));
        }

        [MenuItem("Assets/Set Sprite Anchor/Bottom-Center")]
        private static void SetAnchorToBottomCenter()
        {
            SetSpriteAnchor(new Vector2(0.5f, 0f));
        }

        private static void SetSpriteAnchor(Vector2 pivot)
        {
            // 收集所有选中的纹理路径（去重）
            var pathsToProcess = new HashSet<string>();

            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(path)) continue;

                // 如果是精灵，获取其纹理路径
                if (obj is Sprite)
                {
                    pathsToProcess.Add(path);
                }
                // 如果是纹理，直接添加路径
                else if (obj is Texture2D)
                {
                    pathsToProcess.Add(path);
                }
            }

            if (pathsToProcess.Count == 0)
            {
                Debug.LogWarning("No valid textures or sprites selected.");
                return;
            }

            int processedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing(); // 开始批量资产编辑

                int index = 0;
                foreach (string path in pathsToProcess)
                {
                    index++;

                    // 显示进度条
                    float progress = (float)index / pathsToProcess.Count;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Setting Sprite Anchors",
                            $"Processing {Path.GetFileName(path)} ({index}/{pathsToProcess.Count})",
                            progress))
                    {
                        break;
                    }

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null)
                    {
                        Debug.LogWarning($"Skipped {path}: Not a texture asset");
                        continue;
                    }

                    // 确保是精灵类型
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        Debug.LogWarning($"Skipped {path}: Not imported as Sprite");
                        continue;
                    }

                    // 读取当前设置
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);

                    // 处理多精灵纹理
                    if (importer.spriteImportMode == SpriteImportMode.Multiple)
                    {
                        SpriteMetaData[] spritesheet = importer.spritesheet;

                        if (spritesheet.Length == 0)
                        {
                            Debug.LogWarning($"Skipped {path}: Sprite sheet has no sprites");
                            continue;
                        }

                        // 修改第一个精灵的锚点
                        spritesheet[0].pivot = pivot;
                        importer.spritesheet = spritesheet;

                        processedCount++;
                    }
                    // 处理单精灵纹理
                    else
                    {
                        // 设置新的锚点
                        settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        settings.spritePivot = pivot;

                        // 应用设置
                        importer.SetTextureSettings(settings);
                        processedCount++;
                    }

                    // 保存并重新导入
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting sprite anchor: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing(); // 结束批量资产编辑
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Successfully set anchor for {processedCount} sprite(s).");
        }
    }
}