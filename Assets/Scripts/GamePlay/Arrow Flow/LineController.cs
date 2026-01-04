using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineController : Singleton<LineController>
{
    [SerializeField] private LayerMask _cubeLayer;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
        if(GameManagerInGame.Instance.CurrentGameStateInGame != GameStateInGame.Playing)
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
        if (Physics.Raycast(ray, out RaycastHit hit, 100, _cubeLayer))
        {
            if (hit.transform.TryGetComponent(out CubeLine cube))
            {
                if (cube.Line != null)
                {
                    cube.Line.MoveLine();
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
