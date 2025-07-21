using System.IO;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public class GameSetting
    {
        [MenuItem("Tools/清理游戏存档")]        
        private static void ClearSave()
        {
            string path = Application.persistentDataPath + "/GameData/AccountData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/BirdInfoData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/MusicSettingData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/SettingData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Debug.Log("存档已清理！");
        }
    }
}