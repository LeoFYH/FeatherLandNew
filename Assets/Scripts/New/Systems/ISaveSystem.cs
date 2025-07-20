using System;
using System.IO;
using System.Security.Cryptography;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface ISaveSystem : ISystem
    {
        void InitData();
        void SaveData();
    }

    public class SaveSystem : AbstractSystem, ISaveSystem
    {
        private string saveDir = Application.persistentDataPath + "/GameData/";
        private string tempDir = Application.persistentDataPath + "TempData";
        private string backupDir = Application.persistentDataPath + "BackupData";

        private ISaveModel _saveModel;

        protected override void OnInit()
        {
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            _saveModel = this.GetModel<ISaveModel>();
            InitData();
        }

        private void SaveData<T>(string fileName, T data) where T : SavableData
        {
            string savePath = Path.Combine(saveDir, fileName + ".save");
            string tempPath = Path.Combine(tempDir, fileName + ".tmp");
            string backupPath = Path.Combine(backupDir, fileName + ".bak");

            try
            {
                string jsonData = JsonUtility.ToJson(data);
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                byte[] hash = ComputeMD5(jsonBytes);
            
                byte[] finalData = new byte[jsonBytes.Length + hash.Length];
                Buffer.BlockCopy(jsonBytes, 0, finalData, 0, jsonBytes.Length);
                Buffer.BlockCopy(hash, 0, finalData, jsonBytes.Length, hash.Length);
            
                // 修复：首次保存处理
                if (!File.Exists(savePath))
                {
                    // 首次保存，直接写入主存档
                    File.WriteAllBytes(savePath, finalData);
                    Debug.Log("首次存档创建成功! " + jsonData);
                }
                else
                {
                    // 正常保存流程
                    File.WriteAllBytes(tempPath, finalData);
                
                    // 创建备份
                    try
                    {
                        if (File.Exists(savePath))
                        {
                            File.Copy(savePath, backupPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"备份创建失败: {ex.Message}");
                    }
                
                    // 原子替换操作
                    File.Replace(tempPath, savePath, null);
                    Debug.Log("存档更新成功! " + jsonData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"保存失败: {e.Message}");
                HandleSaveError(fileName, e);
            }
        }

        private T GetData<T>(string fileName) where T : SavableData, new()
        {
            string savePath = Path.Combine(saveDir, fileName + ".save");
            
            if (!File.Exists(savePath))
            {
                Debug.Log("无存档文件，创建新存档");
                return new T();
            }

            try
            {
                byte[] allBytes = File.ReadAllBytes(savePath);
            
                // 校验文件完整性
                if (!ValidateSaveFile(allBytes))
                {
                    Debug.LogWarning("主存档校验失败，尝试加载备份");
                    return TryLoadBackup<T>(fileName);
                }
            
                // 提取JSON数据
                byte[] jsonBytes = new byte[allBytes.Length - 16];
                Buffer.BlockCopy(allBytes, 0, jsonBytes, 0, jsonBytes.Length);
            
                string jsonData = System.Text.Encoding.UTF8.GetString(jsonBytes);
                T data = JsonUtility.FromJson<T>(jsonData);
            
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载失败: {e.Message}");
                return HandleLoadError<T>(fileName);
            }
        }

        // 计算MD5校验和
        private byte[] ComputeMD5(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        // 验证存档完整性
        private bool ValidateSaveFile(byte[] data)
        {
            if (data.Length < 16) return false; // MD5=16字节

            byte[] jsonBytes = new byte[data.Length - 16];
            byte[] storedHash = new byte[16];

            Buffer.BlockCopy(data, 0, jsonBytes, 0, jsonBytes.Length);
            Buffer.BlockCopy(data, jsonBytes.Length, storedHash, 0, 16);

            byte[] realHash = ComputeMD5(jsonBytes);

            for (int i = 0; i < 16; i++)
            {
                if (storedHash[i] != realHash[i]) return false;
            }

            return true;
        }

        // 尝试加载备份
        private T TryLoadBackup<T>(string fileName) where T : SavableData, new()
        {
            string backupPath = Path.Combine(backupDir, fileName + ".bak");
            string savePath = Path.Combine(saveDir, fileName + ".save");
            
            if (!File.Exists(backupPath)) return HandleLoadError<T>(fileName);

            try
            {
                byte[] backupBytes = File.ReadAllBytes(backupPath);
                if (ValidateSaveFile(backupBytes))
                {
                    // 恢复备份
                    File.Copy(backupPath, savePath, true);
                    Debug.Log("备份存档恢复成功");
                    return GetData<T>(fileName); // 重新加载
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"备份加载失败: {e.Message}");
            }

            return HandleLoadError<T>(fileName);
        }

        // 处理加载错误
        private T HandleLoadError<T>(string fileName) where T : SavableData, new()
        {
            Debug.LogWarning("加载失败，创建新存档");

            // 可选：尝试修复或分析存档文件
            AnalyzeCorruptedFile(fileName);

            // 创建新存档
            T data = new T();
            SaveData(fileName, data);
            return data;
        }

        // 处理保存错误
        private void HandleSaveError(string fileName, Exception e)
        {
            // 检查磁盘空间
            if (IsDiskFull(e))
            {
                Debug.LogError("磁盘空间不足，无法保存！");
                // 提示用户清理空间
                return;
            }

            string tempPath = Path.Combine(tempDir, fileName + ".tmp");
            // 删除临时文件
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    Debug.Log("删除错误");
                }
            }
        }

        // 检查磁盘空间
        private bool IsDiskFull(Exception e)
        {
            return e is IOException ioEx &&
                   ioEx.HResult == -2147024784; // 0x80070070 磁盘空间不足
        }

        // 分析损坏文件（调试用）
        private void AnalyzeCorruptedFile(string fileName)
        {
            string savePath = Path.Combine(saveDir, fileName + ".save");
            if (!File.Exists(savePath)) return;

            try
            {
                FileInfo fi = new FileInfo(savePath);
                Debug.Log($"存档大小: {fi.Length} 字节");

                if (fi.Length == 0)
                {
                    Debug.LogWarning("存档文件为空");
                }
                else
                {
                    // 尝试读取部分内容
                    string content = File.ReadAllText(savePath);
                    Debug.Log($"存档开头内容: {content.Substring(0, Math.Min(100, content.Length))}");
                }
            }
            catch
            {
                /* 忽略分析错误 */
            }
        }

        public void InitData()
        {
            _saveModel.AccountData = GetData<AccountData>("AccountData");
            _saveModel.SettingData = GetData<SettingData>("SettingData");
            _saveModel.MusicSettingData = GetData<MusicSettingData>("MusicSettingData");
            _saveModel.BirdInfoData = GetData<BirdInfoData>("BirdInfoData");
        }

        public void SaveData()
        {
            SaveData("AccountData", _saveModel.AccountData);
            SaveData("SettingData", _saveModel.SettingData);
            SaveData("MusicSettingData", _saveModel.MusicSettingData);
            SaveData("BirdInfoData", _saveModel.BirdInfoData);
        }
    }
}