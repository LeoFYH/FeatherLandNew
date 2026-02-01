using UnityEngine;
using UnityEngine.AddressableAssets;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// Prefab图集管理测试脚本
    /// 用于验证PrefabAtlasManager功能是否正常工作
    /// </summary>
    public class PrefabAtlasTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private GameObject testPrefab = null; // 测试用的Prefab
        [SerializeField] private Transform spawnParent = null; // 生成的父级对象

        private void Start()
        {
            // 如果在编辑器中运行，自动执行测试
#if UNITY_EDITOR
            Invoke("RunTest", 2f); // 延迟2秒执行测试
#endif
        }

        public void RunTest()
        {
            Debug.Log("开始 Prefab 图集管理测试...");

            // 测试1: 生成带有图集管理器的Prefab
            if (testPrefab != null)
            {
                GameObject spawnedObj = Instantiate(testPrefab, spawnParent);
                Debug.Log($"已生成测试Prefab: {spawnedObj.name}");

                // 延迟销毁以测试图集释放
                //Invoke("DestroyTestObject", 5f, spawnedObj);
            }
            else
            {
                Debug.LogWarning("请在Inspector中设置测试Prefab");
            }
        }

        private void DestroyTestObject(object obj)
        {
            GameObject spawnedObj = (GameObject)obj;
            if (spawnedObj != null)
            {
                Debug.Log($"销毁测试对象: {spawnedObj.name}");
                DestroyImmediate(spawnedObj);
            }
        }

        [ContextMenu("运行测试")]
        private void ContextMenuRunTest()
        {
            RunTest();
        }
    }
}