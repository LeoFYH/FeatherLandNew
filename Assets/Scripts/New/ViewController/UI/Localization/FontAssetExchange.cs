using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FontAssetExchange : ViewControllerBase
    {
        private TextMeshProUGUI thisText;

        private void Start()
        {
            thisText = GetComponent<TextMeshProUGUI>();
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                ChangeFont();          
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            ChangeFont();
        }

        private void ChangeFont()
        {
            thisText.font = this.GetSystem<ILocalizationSystem>().GetFontAsset();
            thisText.ForceMeshUpdate();
        }
    }
}