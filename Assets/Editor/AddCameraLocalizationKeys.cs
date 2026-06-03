#if UNITY_EDITOR
using System.Collections.Generic;
using BirdGame;
using UnityEditor;
using UnityEngine;

namespace BirdGameEditor
{
    /// <summary>
    /// 一键把相机/望远镜/照片弹窗用到的本地化 key 写进 LocalizationConfig。
    /// 直接操作真实类型（Pattern / LocalizationLanguage），由 Odin 负责序列化，
    /// 不手改 .asset 里的引用 ID，避免污染已有的 625 个 key。
    /// 菜单：Tools/本地化/添加相机望远镜Key
    /// </summary>
    public static class AddCameraLocalizationKeys
    {
        private const string AssetPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        // key -> (语言 -> 翻译文本)
        private static readonly Dictionary<string, Dictionary<SystemLanguage, string>> Translations =
            new Dictionary<string, Dictionary<SystemLanguage, string>>
            {
                ["Copy"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Copy",
                    [SystemLanguage.ChineseSimplified] = "复制",
                    [SystemLanguage.ChineseTraditional] = "複製",
                    [SystemLanguage.German] = "Kopieren",
                    [SystemLanguage.Portuguese] = "Copiar",
                    [SystemLanguage.French] = "Copier",
                    [SystemLanguage.Spanish] = "Copiar",
                    [SystemLanguage.Russian] = "Копировать",
                },
                ["Save"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Save",
                    [SystemLanguage.ChineseSimplified] = "保存",
                    [SystemLanguage.ChineseTraditional] = "儲存",
                    [SystemLanguage.German] = "Speichern",
                    [SystemLanguage.Portuguese] = "Salvar",
                    [SystemLanguage.French] = "Enregistrer",
                    [SystemLanguage.Spanish] = "Guardar",
                    [SystemLanguage.Russian] = "Сохранить",
                },
                ["Choose Folder"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Choose Folder",
                    [SystemLanguage.ChineseSimplified] = "选择文件夹",
                    [SystemLanguage.ChineseTraditional] = "選擇資料夾",
                    [SystemLanguage.German] = "Ordner wählen",
                    [SystemLanguage.Portuguese] = "Escolher pasta",
                    [SystemLanguage.French] = "Choisir un dossier",
                    [SystemLanguage.Spanish] = "Elegir carpeta",
                    [SystemLanguage.Russian] = "Выбрать папку",
                },
                ["Choose save folder"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Choose save folder",
                    [SystemLanguage.ChineseSimplified] = "选择保存文件夹",
                    [SystemLanguage.ChineseTraditional] = "選擇儲存資料夾",
                    [SystemLanguage.German] = "Speicherordner wählen",
                    [SystemLanguage.Portuguese] = "Escolher pasta de salvamento",
                    [SystemLanguage.French] = "Choisir le dossier d'enregistrement",
                    [SystemLanguage.Spanish] = "Elegir carpeta de guardado",
                    [SystemLanguage.Russian] = "Выберите папку для сохранения",
                },
                ["Copied to clipboard"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Copied to clipboard",
                    [SystemLanguage.ChineseSimplified] = "已复制到剪贴板",
                    [SystemLanguage.ChineseTraditional] = "已複製到剪貼簿",
                    [SystemLanguage.German] = "In Zwischenablage kopiert",
                    [SystemLanguage.Portuguese] = "Copiado para a área de transferência",
                    [SystemLanguage.French] = "Copié dans le presse-papiers",
                    [SystemLanguage.Spanish] = "Copiado al portapapeles",
                    [SystemLanguage.Russian] = "Скопировано в буфер обмена",
                },
                ["Copy failed"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Copy failed",
                    [SystemLanguage.ChineseSimplified] = "复制失败",
                    [SystemLanguage.ChineseTraditional] = "複製失敗",
                    [SystemLanguage.German] = "Kopieren fehlgeschlagen",
                    [SystemLanguage.Portuguese] = "Falha ao copiar",
                    [SystemLanguage.French] = "Échec de la copie",
                    [SystemLanguage.Spanish] = "Error al copiar",
                    [SystemLanguage.Russian] = "Не удалось скопировать",
                },
                ["Saved"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Saved",
                    [SystemLanguage.ChineseSimplified] = "已保存",
                    [SystemLanguage.ChineseTraditional] = "已儲存",
                    [SystemLanguage.German] = "Gespeichert",
                    [SystemLanguage.Portuguese] = "Salvo",
                    [SystemLanguage.French] = "Enregistré",
                    [SystemLanguage.Spanish] = "Guardado",
                    [SystemLanguage.Russian] = "Сохранено",
                },
                ["Save failed"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Save failed",
                    [SystemLanguage.ChineseSimplified] = "保存失败",
                    [SystemLanguage.ChineseTraditional] = "儲存失敗",
                    [SystemLanguage.German] = "Speichern fehlgeschlagen",
                    [SystemLanguage.Portuguese] = "Falha ao salvar",
                    [SystemLanguage.French] = "Échec de l'enregistrement",
                    [SystemLanguage.Spanish] = "Error al guardar",
                    [SystemLanguage.Russian] = "Не удалось сохранить",
                },
                ["Storage location changed"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Storage location changed",
                    [SystemLanguage.ChineseSimplified] = "已变更储存位置",
                    [SystemLanguage.ChineseTraditional] = "已變更儲存位置",
                    [SystemLanguage.German] = "Speicherort geändert",
                    [SystemLanguage.Portuguese] = "Local de armazenamento alterado",
                    [SystemLanguage.French] = "Emplacement de stockage modifié",
                    [SystemLanguage.Spanish] = "Ubicación de almacenamiento cambiada",
                    [SystemLanguage.Russian] = "Папка сохранения изменена",
                },
                ["Telescope"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Telescope",
                    [SystemLanguage.ChineseSimplified] = "望远镜",
                    [SystemLanguage.ChineseTraditional] = "望遠鏡",
                    [SystemLanguage.German] = "Teleskop",
                    [SystemLanguage.Portuguese] = "Telescópio",
                    [SystemLanguage.French] = "Télescope",
                    [SystemLanguage.Spanish] = "Telescopio",
                    [SystemLanguage.Russian] = "Телескоп",
                },
                ["Camera"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Camera",
                    [SystemLanguage.ChineseSimplified] = "相机",
                    [SystemLanguage.ChineseTraditional] = "相機",
                    [SystemLanguage.German] = "Kamera",
                    [SystemLanguage.Portuguese] = "Câmera",
                    [SystemLanguage.French] = "Appareil photo",
                    [SystemLanguage.Spanish] = "Cámara",
                    [SystemLanguage.Russian] = "Камера",
                },
            };

        [MenuItem("Tools/Localization/Add Camera Keys")]
        public static void AddKeys()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetPath);
            if (config == null)
            {
                // 兜底：按类型搜索
                string[] guids = AssetDatabase.FindAssets("t:LocalizationConfig");
                if (guids.Length > 0)
                    config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (config == null)
            {
                Debug.LogError($"[AddCameraLocalizationKeys] 找不到 LocalizationConfig（{AssetPath}）");
                return;
            }

            int added = 0, updated = 0;
            foreach (var langPair in config.languageDic)
            {
                SystemLanguage lang = langPair.Key;
                LocalizationLanguage langData = langPair.Value;
                if (langData.words == null)
                    langData.words = new Dictionary<string, Pattern>();

                foreach (var keyPair in Translations)
                {
                    string key = keyPair.Key;
                    if (!keyPair.Value.TryGetValue(lang, out string text))
                        continue; // 该语言没提供翻译就跳过（理论上不会发生）

                    if (langData.words.TryGetValue(key, out var existing) && existing != null)
                    {
                        existing.text = text;
                        updated++;
                    }
                    else
                    {
                        langData.words[key] = new Pattern { text = text };
                        added++;
                    }
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AddCameraLocalizationKeys] 完成：新增 {added} 条，覆盖 {updated} 条，" +
                      $"覆盖 {config.languageDic.Count} 种语言 × {Translations.Count} 个 key。");
        }
    }
}
#endif
