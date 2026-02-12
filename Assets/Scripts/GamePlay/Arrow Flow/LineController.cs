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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
#endif
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
                var tutorialManager = TutorialManager.Instance;
                if (tutorialManager == null) return;
                if (tutorialManager.IsInTutorial)
                {
                    switch (tutorialManager.CurrentTutorial.Type)
                    {
                        case TutorialType.Control:
                            {
                                if (!tutorialManager.TutorialControlWaitTapLine)
                                {
                                    return;
                                }
                                else
                                {
                                    tutorialManager.HandleNextStep();
                                }
                                break;
                            }
                    }
                }
                switch (Board.Instance.CurrentBooster)
                {
                    case BoosterType.None:
                        {
                            if (cube.Line != null && !Board.Instance.IsUsingBooster)
                            {
                                cube.Line.MoveLine();
                            }
                            break;
                        }
                    case BoosterType.Hammer:
                        {
                             if(cube.Cell == null) return;
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
                            if(cube.Cell != null) return;
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
