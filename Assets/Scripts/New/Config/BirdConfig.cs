using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

namespace BirdGame
{
    public class BirdConfig : SerializedScriptableObject
    {
        [Title("鸟的配置"), Space(10)] 
        [LabelText("显示鸟走路的路线"), BoxGroup("信息")]
        public bool isDrawPathLine;
        [LabelText("鸟的最大数量"), BoxGroup("信息")]
        public int maxBirdCount = 35;
        [LabelText("稀有度颜色配置"), OdinSerialize, DictionaryDrawerSettings(KeyLabel = "稀有度", ValueLabel = "颜色"), BoxGroup("信息")]
        public Dictionary<string, Color32> colorSettings = new Dictionary<string, Color32>()
        {
            {"Common", Color.white},
            {"Rare", Color.white},
            {"Endangered", Color.white},
            {"Extinct", Color.white},
            {"Unknown", Color.white},
        };

        [HideInInspector]
        public List<SceneBird> sceneBirds = new List<SceneBird>();
#if UNITY_EDITOR
        
        [ShowInInspector, HideLabel, BoxGroup("场景鸟列表"), OnValueChanged("OnSelectSceneChanged"), ValueDropdown("GetScenes", DropdownTitle = "选择地图")]
        private int sceneIndex;

        [ShowInInspector, BoxGroup("场景鸟列表"), HideLabel]
        private SceneBird currentSceneBird;
        
        [OnInspectorInit]
        private void OnInit()
        {
            if (sceneBirds == null)
                sceneBirds = new List<SceneBird>();
        }

        private ValueDropdownList<int> GetScenes()
        {
            var list = new ValueDropdownList<int>();
            var config = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Prefabs/Config/MapConfig.asset");
            for (int i = 0; i < config.maps.Length; i++)
            {
                list.Add(new ValueDropdownItem<int>(config.maps[i].mapName, i));
            }

            return list;
        }

        private void OnSelectSceneChanged()
        {
            while (sceneIndex >= sceneBirds.Count)
            {
                sceneBirds.Add(new SceneBird()
                {
                    birdClasses = new BirdClassItem[]{}
                });
            }

            currentSceneBird = sceneBirds[sceneIndex];
        }
#endif

        public string GetBirdName(int birdId, int mapIndex)
        {
            for (int i = 0; i < sceneBirds[mapIndex].birdClasses.Length; i++)
            {
                foreach (var bird in sceneBirds[mapIndex].birdClasses[i].birds)
                {
                    if (bird.id == birdId)
                        return sceneBirds[mapIndex].birdClasses[i].birdName;
                }
            }

            Debug.LogError($"没有找到id为{birdId}的鸟的配置!");
            return "";
        }
        
        /// <summary>
        /// 获取鸟类名称的本地化key
        /// </summary>
        /// <param name="birdId">鸟类ID</param>
        /// <returns>本地化key</returns>
        public string GetBirdNameKey(int birdId, int mapIndex)
        {
            return GetBirdName(birdId, mapIndex);
        }
        
        /// <summary>
        /// 根据鸟类类别索引获取鸟类名称的本地化key
        /// </summary>
        /// <param name="classIndex">鸟类类别索引</param>
        /// <returns>本地化key</returns>
        public string GetBirdNameKeyByClassIndex(int classIndex, int mapIndex)
        {
            if (classIndex >= 0 && classIndex < sceneBirds[mapIndex].birdClasses.Length)
            {
                return sceneBirds[mapIndex].birdClasses[classIndex].birdName;
            }
            
            Debug.LogError($"鸟类类别索引{classIndex}超出范围!");
            return "";
        }

        public BirdItem GetBird(int birdId, int mapIndex)
        {
            for (int i = 0; i < sceneBirds[mapIndex].birdClasses.Length; i++)
            {
                foreach (var bird in sceneBirds[mapIndex].birdClasses[i].birds)
                {
                    if (bird == null)
                    {
                        Debug.Log(sceneBirds[mapIndex].birdClasses[i].birdName + "有空项！");
                        continue;
                    }
                    if (bird.id == birdId)
                        return bird;
                }
            }
            Debug.Log($"没有找到id为{birdId}的鸟的配置!");
            return null;
        }

        public BirdItem GetBird(int birdId, int mapIndex, out int classIndex)
        {
            for (int i = 0; i < sceneBirds[mapIndex].birdClasses.Length; i++)
            {
                foreach (var bird in sceneBirds[mapIndex].birdClasses[i].birds)
                {
                    if (bird.id == birdId)
                    {
                        classIndex = i;
                        return bird;
                    }
                }
            }
            Debug.LogError($"没有找到id为{birdId}的鸟的配置!");
            classIndex = 0;
            return null;
        }
    }

    [Serializable]
    public class SceneBird
    {
        [LabelText("鸟的种类"), TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public BirdClassItem[] birdClasses;
    }

    [Serializable]
    public class BirdClassItem
    {
        [LabelText("名称"), VerticalGroup("Info")]
        public string birdName;
        [OdinSerialize, TableList(ShowIndexLabels = true), VerticalGroup("Info")]
        public List<BirdItem> birds = new List<BirdItem>();
        
        [LabelText("选择的鸟"), ValueDropdown("GetBirdList"), BoxGroup("Info/标准信息设置"), ShowInInspector]
        private int birdIndex;
        [Button("同步"), BoxGroup("Info/标准信息设置")]
        private void OnLoadClick()
        {
            if(birds == null || birds.Count == 0)
                return;
            if(birdIndex >= birds.Count || birdIndex < 0)
                return;
            var conf = birds[birdIndex];
            for (int i = 0; i < birds.Count; i++)
            {
                if(i == birdIndex)
                    continue;
                birds[i].description = conf.description;
                birds[i].habitat = conf.habitat;
                birds[i].reality = conf.reality;
                birds[i].autoExp = conf.autoExp;
                birds[i].canFly = conf.canFly;
                birds[i].clickEarning = conf.clickEarning;
                birds[i].eatExp = conf.eatExp;
                birds[i].scenePreview = conf.scenePreview;
                birds[i].totalExp = conf.totalExp;
                birds[i].canFlyWait = conf.canFlyWait;
                birds[i].eraningForBig = conf.eraningForBig;
                birds[i].eraningForSmall = conf.eraningForSmall;
                birds[i].priceForBig = conf.priceForBig;
                birds[i].priceForSmall = conf.priceForSmall;
                birds[i].clickEarningForFiveTimes = conf.clickEarningForFiveTimes;
            }
        }

       

        private ValueDropdownList<int> GetBirdList()
        {
            var list = new ValueDropdownList<int>();
            for (int i = 0; i < birds.Count; i++)
            {
                list.Add(new ValueDropdownItem<int>($"{birdName} {i}", i));
            }

            return list;
        }
    }
    
}