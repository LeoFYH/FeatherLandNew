using DG.Tweening;
using UnityEngine;
using System.Collections;

namespace BirdGame
{
    public class BirdFlyWaitState : StateBase
    {
        private Brid _brid;

        public BirdFlyWaitState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            _brid.anim.SetBool("IsTakeOff", false);
            _brid.anim.SetBool("Fly", false);
            //_brid.anim.Play("FlyWait");

            // 确保初始状态为呼吸（不张望）
            // 不需要设置Trigger的初始值

            if (_brid.nestTrans != null)
            {
                // 完全使用 target 的位置
                Vector3 alignedPosition = new Vector3(
                    _brid.nestTrans.position.x, // 使用 target 的 X
                    _brid.nestTrans.position.y, // 使用 target 的 Y
                    _brid.transform.position.z
                );

                _brid.transform.position = alignedPosition;
            }

            float waitTime = Random.Range(3f, 8f);
            
            // 立即播放张望动画，然后等待
            DOTween.Sequence()
                .AppendCallback(() => { PlayLookAroundAnimation(); }) 
                .SetDelay(0.1f) 
                .AppendCallback(() => { currMachine.ChangeState<BirdFlyDownState>(); })
                .SetDelay(waitTime);
        }

        public override void OnUpdate()
        {

        }

        public override void OnExit()
        {

        }

        /// <summary>
        /// 播放张望动画
        /// </summary>
        private void PlayLookAroundAnimation()
        {
          
            Debug.Log("开始张望动画");
            _brid.anim.SetTrigger("LookAround");
            
           
            DOTween.Sequence()
                .SetDelay(1f)
                .AppendCallback(() => {
                   
                    Debug.Log("切换回呼吸状态");
                    
                });
        }

    }
}