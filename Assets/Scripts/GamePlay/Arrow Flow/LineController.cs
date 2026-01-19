using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineController : Singleton<LineController>
{
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private Hammer _hammerPrefab;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }
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
        if (Physics.Raycast(ray, out RaycastHit hit, 100, _cubeLayer))
        {
            if (hit.transform.TryGetComponent(out CubeLine cube))
            {
                switch (Board.Instance.CurrentBooster)
                {
                    case BoosterType.None:
                        {
                            if (cube.Line != null)
                            {
                                cube.Line.MoveLine();
                            }
                            break;
                        }
                    case BoosterType.Hammer:
                        {
                            Board.Instance.CurrentBooster = BoosterType.None;
                            var hammer = Instantiate(_hammerPrefab);
                            StartCoroutine(hammer.Hit(cube, onHit: () =>
                            {
                                cube.Line.DestroyLine();
                                Board.Instance.ResetBooster();
                            }));
                            break;
                        }
                    case BoosterType.Conveyor:
                        {
                            var consecutiveCubes = ConveyorController.Instance.GetConsecutiveCubesByColor(cube);
                            if (consecutiveCubes.Count > 0)
                            {
                                Board.Instance.CurrentBooster = BoosterType.None;
                                var destroyedByColor = ConveyorController.Instance.DestroyConsecutiveCubes(consecutiveCubes);
                                foreach (var kvp in destroyedByColor)
                                {
                                    ShooterController.Instance.ReduceShooterTotalByColor(kvp.Key, kvp.Value);
                                }
                                ConveyorController.Instance.SetAllCubesBringToTop(false);
                                ConveyorController.Instance.BringToTop = false;
                                ConveyorController.Instance.StartConveyor();
                                Board.Instance.ResetBooster();
                            }
                            break;
                        }
                    case BoosterType.Rainbow:
                        {
                            break;
                        }
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
