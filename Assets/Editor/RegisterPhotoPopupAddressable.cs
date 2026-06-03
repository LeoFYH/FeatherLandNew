#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace BirdGameEditor
{
    /// <summary>
    /// 一键把 PhotoPopup.prefab 注册到 Addressables 的 UI_Popups 组。
    /// 菜单：Tools/Addressables/Register PhotoPopup
    /// </summary>
    public static class RegisterPhotoPopupAddressable
    {
        private const string AssetPath = "Assets/Prefabs/UI/Popups/PhotoPopup.prefab";
        private const string GroupName = "UI_Popups";
        private const string Address = "PhotoPopup";

        [MenuItem("Tools/Addressables/Register PhotoPopup")]
        public static void Register()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[RegisterPhotoPopup] 找不到 Addressable Settings，请先初始化 Addressables。");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(AssetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[RegisterPhotoPopup] 资源不存在：{AssetPath}");
                return;
            }

            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                Debug.LogError($"[RegisterPhotoPopup] 找不到 Addressable 组：{GroupName}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Address;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RegisterPhotoPopup] 已把 {AssetPath} 注册到 {GroupName}，Address={Address}。" +
                      $"现在去 Window/Asset Management/Addressables/Groups 点击 Build/New Build/Default Build Script。");
        }
    }
}
#endif
