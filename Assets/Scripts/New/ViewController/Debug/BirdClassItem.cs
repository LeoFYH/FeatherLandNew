using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame.DebugMode
{
    public class BirdClassItem : ViewControllerBase
    {
        public TMP_InputField nameInput;
        public GameObject birdPrefab;

        private int sceneIndex;
        private int classId;

        public void Init(int scene, int classIndex)
        {
            sceneIndex = scene;
            classId = sceneIndex;
            var classData = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[scene].birdClasses[classIndex];
            nameInput.text = classData.birdName;
            nameInput.onValueChanged.AddListener(value =>
            {
                classData.birdName = value;
            });

            foreach (var bird in classData.birds)
            {
                var obj = GameObject.Instantiate(birdPrefab, transform);
                obj.SetActive(true);
                var birdItem = obj.GetComponent<BirdItemEditor>();
                birdItem.Init(bird);
            }
        }
    }
}