using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    public class NumPanel : MonoBehaviour, IController
    {
        public CanvasGroup cg;
        float y;
        Text contentTxt;

        private void Awake()
        {
            contentTxt = GetComponent<Text>();
        }

        public void Init(string s)
        {
            // Reset state for pool reuse
            cg.alpha = 1f;
            y = transform.localPosition.y;
            contentTxt.text = s;
            transform.DOLocalMoveY(y, 0.2f).OnComplete(delegate
            {
                cg.DOFade(0, 1);
                transform.DOLocalMoveY(y + 25f, 1).OnComplete(delegate
                {
                    // Memory optimization: recycle to pool instead of Destroy
                    var pooledObj = GetComponent<PooledObject>();
                    if (pooledObj != null && !string.IsNullOrEmpty(pooledObj.poolName))
                    {
                        this.GetSystem<IObjectPoolSystem>().Recycle(pooledObj.poolName, gameObject);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                });
            });
        }

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}
