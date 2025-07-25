using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public class SetPivotBottomCenter
    {
        [MenuItem("Assets/Set Pivot to Bottom Center", true)]
        private static bool ValidateSetPivot()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    return true;
            }

            return false;
        }

        [MenuItem("Assets/Set Pivot to Bottom Center")]
        private static void SetPivot()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    Debug.LogWarning($"忽略 {path}，不是 Sprite 类型。");
                    continue;
                }

                if (importer.spriteImportMode == SpriteImportMode.Single)
                {
                    importer.spritePivot = new Vector2(0.5f, 0.0f);
                    // 不要设置 importer.spriteAlignment
                    importer.SaveAndReimport();
                    Debug.Log($"[Single] 设置 {path} 的 Pivot 为 Bottom Center");
                }
                else if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    SpriteMetaData[] metas = importer.spritesheet;

                    if (metas.Length > 0)
                    {
                        metas[0].pivot = new Vector2(0.5f, 0.0f);
                        metas[0].alignment = (int)SpriteAlignment.Custom;

                        importer.spritesheet = metas;
                        importer.SaveAndReimport();
                        Debug.Log($"[Multiple] 设置 {path} 的第一个 Sprite Pivot 为 Bottom Center");
                    }
                    else
                    {
                        Debug.LogWarning($"[Multiple] {path} 没有任何 Sprite，跳过。");
                    }
                }
            }
        }
    }
}