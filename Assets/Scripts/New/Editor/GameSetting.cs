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
            path = Application.persistentDataPath + "/GameData/NoteData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/ScheduleData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/DecorationData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            path = Application.persistentDataPath + "/GameData/IllustratedData.save";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            PlayerPrefs.DeleteAll();
            EditorUtility.DisplayDialog("提示", "存档已清理！", "ok");
        }
    }
}