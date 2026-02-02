using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TapController : Singleton<TapController>
{
    [SerializeField] private LayerMask _shooterLayer;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
#endif
        if (GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Playing)
        {
            return;
        }
        HandleDefaultTouch();
    }

    private void HandleDefaultTouch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTouchBegan();
        }
        else if (Input.GetMouseButton(0))
        {
            OnTouchMoved();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchEnded();
        }
    }
    private void OnTouchBegan()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, _shooterLayer))
        {
            if (hit.transform.TryGetComponent(out Shooter shooter))
            {
                if (shooter.OnHolder || shooter.IsMoving)
                {
                    return;
                }
                if (!Board.Instance.CurrentMap.CheckCanMove(shooter.GridPosition))
                {
                    shooter.Shake();
                    return;
                }
                var emptyHolder = Board.Instance.CurrentMap.Holders.FirstOrDefault(hol => !hol.IsOccupied);
                if (emptyHolder != null)
                {
                    emptyHolder.AssignShooter(shooter);
                    Board.Instance.CurrentMap.Grid[shooter.GridPosition.x, shooter.GridPosition.y].Shooter = null;
                    Board.Instance.CurrentMap.CheckAllShootersCanMove();
                }
            }
        }
    }
    private void OnTouchMoved()
    {

    }

    private void OnTouchEnded()
    {

    }

}