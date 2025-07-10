using System.Collections;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{

    public class BirdIdleState : StateBase
    {
        private Brid _brid;
        private Coroutine coroutine;
        private int random;

        public BirdIdleState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            float time = Random.Range(5f, 10f);
            random = Random.Range(1, 10);
            float lickingTime = Random.Range(2f, time - 0.5f);

            coroutine = _brid.StartCoroutine(WaitForNext(time));
            DOTween.Sequence().AppendCallback(() => { _brid.anim.SetTrigger("Licking"); }).SetDelay(lickingTime);
        }

        public override void OnUpdate()
        {
            if (_brid.isSmall && !_brid.isAte)
            {
                if (_brid.currFood == null)
                {
                    Food food;
                    if (this.GetSystem<IGameSystem>().TryGetUntargetedFood(_brid.transform.position, out food))
                    {
                        // if(random == 1) // 10个数中随机到1时去吃食物
                        // {
                        _brid.currFood = food;
                        currMachine.ChangeState<BirdEatState>();
                        //}
                    }
                }
            }
        }

        public override void OnExit()
        {
            if (coroutine == null)
                return;
            _brid.StopCoroutine(coroutine);
        }

        private void DONext()
        {
            if (_brid.isSmall)
            {
                currMachine.ChangeState<BirdRunState>();
                return;
            }
            else if (this.GetModel<IBirdModel>().FlyPositions.Count > 0)
            {
                int index = Random.Range(0, 3);
                if (index == 0)
                {
                    currMachine.ChangeState<BirdRunState>();
                }
                else if (index == 1)
                {
                    currMachine.ChangeState<BirdFlyState>();
                }
                else
                {
                    currMachine.ChangeState<BirdFlyHorizontalState>();
                }
            }
            else //基本不会触发
            {
                int index = Random.Range(0, 3);
                if (index == 0)
                {
                    currMachine.ChangeState<BirdRunState>();
                }
                else
                {
                    currMachine.ChangeState<BirdFlyHorizontalState>();
                }
            }
        }

        private IEnumerator WaitForNext(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            DONext();
        }
    }
}
