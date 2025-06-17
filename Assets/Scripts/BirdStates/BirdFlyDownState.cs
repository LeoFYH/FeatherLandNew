using DG.Tweening;
using FSM;
using UnityEngine;

public class BirdFlyDownState : StateBase
{
    private Brid _brid;
    
    public BirdFlyDownState(StateMachine machine) : base(machine)
    {
        _brid = machine.currObj.GetComponent<Brid>();
    }

    public override void OnEnter()
    {
        _brid.anim.SetBool("Fly", true);
        _brid.anim.Play("FlyStart");

        // 获取降落点
        Vector2 landingPoint = GetLandingPoint();
        var target = new Vector3(landingPoint.x, landingPoint.y, 0);

        // 如果之前在巢穴中，释放巢穴位置
        if (_brid.nestTrans != null)
        {
            GameManager.Instance.flyPositions.Add(_brid.nestTrans);
            _brid.nestTrans = null;
        }

        _brid.sr.flipX = target.x > _brid.transform.position.x;
        float distance = Vector3.Distance(target, _brid.transform.position);
        float time = distance / _brid.flySpeed;

        DOTween.Sequence().AppendCallback(() =>
        {
            _brid.transform.DOScale(_brid.AdultBirdSize, time).SetEase(Ease.Linear);
            _brid.sr.sortingOrder = 0;

            var anim = DOTween.Sequence();
            anim.Append(_brid.transform.DOMove(target, time).SetEase(Ease.Linear));
            anim.AppendCallback(() =>
            {
                _brid.anim.SetBool("Fly", false);
            });
            anim.AppendInterval(2f);
            anim.OnComplete(() =>
            {
                currMachine.ChangeState<BirdIdleState>();
            });
        }).SetDelay(0.2f);
    }

    // 新增：获取有效的降落点
    private Vector2 GetLandingPoint()
    {
        WalkableArea walkableArea = _brid.walkableArea;
        if (walkableArea == null)
        {
            walkableArea = Object.FindObjectOfType<WalkableArea>();
            if (walkableArea == null)
            {
                Debug.LogWarning("没有找到WalkableArea，使用默认范围！");
                return new Vector2(Random.Range(-8f, 8f), Random.Range(-5f, -3.5f));
            }
        }

        // 尝试最多10次获取有效点
        for (int i = 0; i < 10; i++)
        {
            // 获取WalkableArea的边界
            Bounds bounds = walkableArea.GetComponent<PolygonCollider2D>().bounds;
            
            // 在边界范围内随机取点
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 point = new Vector2(x, y);

            // 检查点是否在WalkableArea内
            if (walkableArea.IsPointInside(point))
            {
                return point;
            }
        }

        // 如果10次都没找到合适的点，返回WalkableArea的中心点
        return (Vector2)walkableArea.transform.position;
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnExit()
    {
        
    }
}
