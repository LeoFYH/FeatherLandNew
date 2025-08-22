using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace BirdGame
{
    public class BirdConfig : SerializedScriptableObject
    {
        [Title("鸟的配置"), Space(10)] 
        [LabelText("显示鸟走路的路线")]
        public bool isDrawPathLine;
        [LabelText("鸟的种类"), TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public BirdClassItem[] birdClasses;
        [LabelText("稀有度颜色配置"), OdinSerialize, DictionaryDrawerSettings(KeyLabel = "稀有度", ValueLabel = "颜色")]
        public Dictionary<string, Color32> colorSettings = new Dictionary<string, Color32>()
        {
            {"Common", Color.white},
            {"Rare", Color.white},
            {"Endangered", Color.white},
            {"Extinct", Color.white},
            {"Unknown", Color.white},
        };

        public string GetBirdName(int birdId)
        {
            for (int i = 0; i < birdClasses.Length; i++)
            {
                foreach (var bird in birdClasses[i].birds)
                {
                    if (bird.id == birdId)
                        return birdClasses[i].birdName;
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
        public string GetBirdNameKey(int birdId)
        {
            return GetBirdName(birdId);
        }
        
        /// <summary>
        /// 根据鸟类类别索引获取鸟类名称的本地化key
        /// </summary>
        /// <param name="classIndex">鸟类类别索引</param>
        /// <returns>本地化key</returns>
        public string GetBirdNameKeyByClassIndex(int classIndex)
        {
            if (classIndex >= 0 && classIndex < birdClasses.Length)
            {
                return birdClasses[classIndex].birdName;
            }
            
            Debug.LogError($"鸟类类别索引{classIndex}超出范围!");
            return "";
        }

        public BirdItem GetBird(int birdId)
        {
            for (int i = 0; i < birdClasses.Length; i++)
            {
                foreach (var bird in birdClasses[i].birds)
                {
                    if (bird.id == birdId)
                        return bird;
                }
            }
            Debug.LogError($"没有找到id为{birdId}的鸟的配置!");
            return null;
        }

        public BirdItem GetBird(int birdId, out int classIndex)
        {
            for (int i = 0; i < birdClasses.Length; i++)
            {
                foreach (var bird in birdClasses[i].birds)
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
    public class BirdClassItem
    {
        [LabelText("名称"), VerticalGroup("Info")]
        public string birdName;
        [OdinSerialize, TableList(ShowIndexLabels = true), VerticalGroup("Info")]
        public List<BirdItem> birds = new List<BirdItem>();
    }
    
}