using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class EnvironmentViewController : ViewControllerBase
    {
        public Slider[] environmentVolumes;
        private bool isUpdatingSlider = false; // 防止循环更新的标志
        
        private void Start()
        {
            this.GetSystem<IAudioSystem>().InitEnvironments();
            var radioModel = this.GetModel<IRadioModel>();
            
            for (int i = 0; i < environmentVolumes.Length; i++)
            {
                InitVolume(i);
                
                // 监听 EnvironmentVolumes 的变化，自动更新 slider
                int index = i;
                if (index < radioModel.EnvironmentVolumes.Count)
                {
                    radioModel.EnvironmentVolumes[index].Register(v =>
                    {
                        if (!isUpdatingSlider && index < environmentVolumes.Length)
                        {
                            isUpdatingSlider = true;
                            environmentVolumes[index].value = v;
                            isUpdatingSlider = false;
                        }
                    }).UnRegisterWhenGameObjectDestroyed(gameObject);
                }
            }
        }

        private void InitVolume(int index)
        {
            var radioModel = this.GetModel<IRadioModel>();
            if (index < radioModel.EnvironmentVolumes.Count)
            {
                environmentVolumes[index].value = radioModel.EnvironmentVolumes[index].Value;
            }
            
            environmentVolumes[index].onValueChanged.AddListener(v =>
            {
                if (!isUpdatingSlider && index < radioModel.EnvironmentVolumes.Count)
                {
                    radioModel.EnvironmentVolumes[index].Value = v;
                }
            });
        }
    }
}