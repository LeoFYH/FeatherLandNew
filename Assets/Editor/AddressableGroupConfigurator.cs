#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace BirdGame.Editor
{
    /// <summary>
    /// Addressable分组配置工具
    /// 用于批量优化Addressable Groups的设置
    /// </summary>
    public class AddressableGroupConfigurator
    {
        [MenuItem("Tools/优化分组配置 (Optimize Group Settings)")]
        public static void ApplyOptimizedSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("未找到Addressable Settings，请先初始化Addressables系统！");
                return;
            }

            Debug.Log("=== 开始优化Addressable分组配置 ===");

            // 1. Core (核心资源组)
            var coreGroup = GetOrCreateGroup(settings, "Core");
            SetGroupSettings(coreGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "核心启动资源");

            // 2. Configs (配置文件组)
            var configsGroup = GetOrCreateGroup(settings, "Configs");
            SetGroupSettings(configsGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "配置文件");

            // 3. UI_Essential (基础UI组)
            var uiEssentialGroup = GetOrCreateGroup(settings, "UI_Essential");
            SetGroupSettings(uiEssentialGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "基础UI界面");

            // 4. UI_Popups (弹窗UI组)
            var uiPopupsGroup = GetOrCreateGroup(settings, "UI_Popups");
            SetGroupSettings(uiPopupsGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackSeparately, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "弹窗UI（按需加载）");

            // 5. Scenes (场景组)
            var scenesGroup = GetOrCreateGroup(settings, "Scenes");
            SetGroupSettings(scenesGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackSeparately, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "场景资源");

            // 6. Audio_Music (音乐组)
            var audioMusicGroup = GetOrCreateGroup(settings, "Audio_Music");
            SetGroupSettings(audioMusicGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackSeparately, 
                BundledAssetGroupSchema.BundleCompressionMode.Uncompressed,
                "音乐文件（流式加载）");

            // 7. Audio_Effects (音效组)
            var audioEffectsGroup = GetOrCreateGroup(settings, "Audio_Effects");
            SetGroupSettings(audioEffectsGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "音效文件");

            // 8. Prefabs_Common (通用预制体组)
            var prefabsCommonGroup = GetOrCreateGroup(settings, "Prefabs_Common");
            SetGroupSettings(prefabsCommonGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "频繁使用的预制体");

            // 9. Prefabs_Special (特殊预制体组)
            var prefabsSpecialGroup = GetOrCreateGroup(settings, "Prefabs_Special");
            SetGroupSettings(prefabsSpecialGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackSeparately, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "特殊效果预制体");

            // 10. Atlas (图集组)
            var atlasGroup = GetOrCreateGroup(settings, "Atlas");
            SetGroupSettings(atlasGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "Sprite图集");

            // 11. Shared_Dependencies (共享依赖组)
            var sharedDepsGroup = GetOrCreateGroup(settings, "Shared_Dependencies");
            SetGroupSettings(sharedDepsGroup, 
                BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
                BundledAssetGroupSchema.BundleCompressionMode.LZ4,
                "共享材质和Shader");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("=== Addressable分组配置优化完成！===");
            Debug.Log("请根据ADDRESSABLES_OPTIMIZATION_GUIDE.md文档，将资源分配到对应的组中。");
        }

        [MenuItem("Tools/Addressables/分析重复依赖 (Analyze Duplicate Dependencies)")]
        public static void AnalyzeDuplicateDependencies()
        {
            // 打开Addressables Analyze窗口
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Analyze");
            
            Debug.Log("请在Analyze窗口中点击 'Check Duplicate Bundle Dependencies' 来检查重复依赖。");
        }

        [MenuItem("Tools/Addressables/清理未使用的分组 (Clean Unused Groups)")]
        public static void CleanUnusedGroups()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("未找到Addressable Settings！");
                return;
            }

            int removedCount = 0;
            var groupsToRemove = new System.Collections.Generic.List<AddressableAssetGroup>();

            foreach (var group in settings.groups)
            {
                // 跳过内置组
                if (group.ReadOnly) continue;

                // 如果组为空或只包含极少资源，标记为待删除
                if (group.entries.Count == 0)
                {
                    groupsToRemove.Add(group);
                }
            }

            foreach (var group in groupsToRemove)
            {
                Debug.Log($"移除空分组: {group.Name}");
                settings.RemoveGroup(group);
                removedCount++;
            }

            if (removedCount > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"清理完成，共移除 {removedCount} 个空分组。");
            }
            else
            {
                Debug.Log("没有需要清理的空分组。");
            }
        }

        [MenuItem("Tools/Addressables/显示分组统计 (Show Group Statistics)")]
        public static void ShowGroupStatistics()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("未找到Addressable Settings！");
                return;
            }

            Debug.Log("=== Addressable分组统计 ===");
            
            int totalGroups = 0;
            int totalEntries = 0;

            foreach (var group in settings.groups)
            {
                if (group.ReadOnly) continue;
                
                totalGroups++;
                int entryCount = group.entries.Count;
                totalEntries += entryCount;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                string packMode = schema != null ? schema.BundleMode.ToString() : "Unknown";
                string compression = schema != null ? schema.Compression.ToString() : "Unknown";

                Debug.Log($"分组: {group.Name}");
                Debug.Log($"  - 资源数量: {entryCount}");
                Debug.Log($"  - 打包模式: {packMode}");
                Debug.Log($"  - 压缩方式: {compression}");
            }

            Debug.Log($"\n总计: {totalGroups} 个分组, {totalEntries} 个资源条目");
            Debug.Log("======================");
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.Log($"创建新分组: {groupName}");
                group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }
            return group;
        }

        private static void SetGroupSettings(
            AddressableAssetGroup group, 
            BundledAssetGroupSchema.BundlePackingMode packMode,
            BundledAssetGroupSchema.BundleCompressionMode compression,
            string description)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                schema = group.AddSchema<BundledAssetGroupSchema>();
            }

            schema.BundleMode = packMode;
            schema.Compression = compression;
            schema.IncludeInBuild = true;
            schema.UseAssetBundleCache = true;
            schema.UseAssetBundleCrc = false; // 禁用CRC检查以提升性能
            schema.Timeout = 0;
            schema.ChunkedTransfer = false;
            schema.RedirectLimit = -1;
            schema.RetryCount = 0;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash; // 使用文件名哈希

            EditorUtility.SetDirty(group);

            Debug.Log($"配置分组 [{group.Name}]: {description}");
            Debug.Log($"  - 打包模式: {packMode}");
            Debug.Log($"  - 压缩方式: {compression}");
        }
    }
}
#endif
