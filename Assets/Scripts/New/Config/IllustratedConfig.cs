using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BirdGame
{
    public class IllustratedConfig : ScriptableObject
    {
        public BirdClass[] birdClasses;
    }

    [Serializable]
    public class BirdClass
    {
        [LabelText("名称")]
        public string birdName;
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public SkinItem[] birdSkins;
    }

    [Serializable]
    public struct SkinItem
    {
        [HorizontalGroup("content", Width = 30), PreviewField(ObjectFieldAlignment.Left, Height = 30), ReadOnly, HideLabel, ShowInInspector]
        private Texture2D preview;
        [HorizontalGroup("content"), ValueDropdown("GetBirdList"), HideLabel, OnValueChanged("OnBirdIndexValueChanged"), OnInspectorGUI("Refresh")]
        public int birdIndex;
        
        private ValueDropdownList<int> GetBirdList()
        {
#if UNITY_EDITOR
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");

            var list = new ValueDropdownList<int>();
            for (int i = 0; i < config.birds.Length; i++)
            {
                list.Add(config.birds[i].birdId, i);
            }

            return list;
#else
            return null;
#endif
        }

        private void OnBirdIndexValueChanged()
        {
#if UNITY_EDITOR
            var config = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            preview = config.birds[birdIndex].preview.texture;
#endif
        }

        private void Refresh()
        {
            if(preview != null)
                return;
            OnBirdIndexValueChanged();
        }
    }
}