using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LineController : Singleton<LineController>
{
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private LayerMask _gateLayer;
    [SerializeField] private Hammer _hammerPrefab;
    [SerializeField] private Shuffle _shufflePrefab;
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

        if (Board.Instance.CurrentBooster == BoosterType.Shuffle)
        {
            int mask = _gateLayer.value != 0 ? _gateLayer.value : ~0;
            if (mask != ~0)
            {
                int top = LayerMask.NameToLayer("Top");
                if (top >= 0) mask |= 1 << top;
                int gateLayer = LayerMask.NameToLayer("Gate");
                if (gateLayer >= 0) mask |= 1 << gateLayer;
            }
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, mask);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                IGate gate = null;
                for (int i = 0; i < hits.Length; i++)
                {
                    var tr = hits[i].transform;
                    if (tr == null) continue;
                    var gateMono = tr.GetComponentInParent<Gate>();
                    if (gateMono != null)
                    {
                        gate = gateMono;
                        break;
                    }
                    var gateDouble = tr.GetComponentInParent<GateDouble>();
                    if (gateDouble != null)
                    {
                        gate = gateDouble;
                        break;
                    }
                }

                if (gate == null) return;

                if (ShooterController.Instance == null || !ShooterController.Instance.CanShuffleShootersOnGate(gate)) return;

                if (InventoryManager.Instance == null) return;
                bool used = InventoryManager.Instance.UseBoosterType2();
                if (!used)
                {
                    if (GameUI.Instance != null)
                    {
                        var buy = GameUI.Instance.Get<UIBuyBooster>();
                        if (buy != null) buy.ShowForBooster(2);
                    }

                    Board.Instance.CurrentBooster = BoosterType.None;
                    Board.Instance.ResetBooster();
                    var ui = GameUI.Instance != null ? GameUI.Instance.Get<UIBottomInGame>() : null;
                    if (ui != null) ui.RefreshBoosterUIImmediate();
                    return;
                }

                var shuffle = Instantiate(_shufflePrefab);

                StartCoroutine(shuffle.Hit(gate, onHit: () =>
                {
                    bool success = ShooterController.Instance.ShuffleShootersOnGate(gate);
                    if (!success)
                    {
                        InventoryManager.Instance.AddBoosterType2(1);
                        return;
                    }

                    Board.Instance.CurrentBooster = BoosterType.None;
                    Board.Instance.ResetBooster();
                    var bottomUi = GameUI.Instance != null ? GameUI.Instance.Get<UIBottomInGame>() : null;
                    if (bottomUi != null) bottomUi.RefreshBoosterUIImmediate();
                }));
            }

            return;
        }

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
