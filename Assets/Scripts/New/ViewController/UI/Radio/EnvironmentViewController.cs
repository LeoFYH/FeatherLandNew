using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class EnvironmentViewController : ViewControllerBase
    {
        public Slider[] environmentVolumes;
        
        private void Start()
        {
            this.GetSystem<IAudioSystem>().InitEnvironments();
            for (int i = 0; i < environmentVolumes.Length; i++)
            {
                InitVolume(i);
            }
        }

        private void InitVolume(int index)
        {
            environmentVolumes[index].value = this.GetModel<IRadioModel>().EnvironmentVolumes[index].Value;
            environmentVolumes[index].onValueChanged.AddListener(v =>
            {
                this.GetModel<IRadioModel>().EnvironmentVolumes[index].Value = v;
            });
        }
    }
}