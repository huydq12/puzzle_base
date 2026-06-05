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

        // Clicking the injected fallback rainbow shooter activates it (enables shooting).
        if (Board.Instance != null && Board.Instance.CurrentBooster == BoosterType.None)
        {
            if (TryActivateFallbackShooterOnGate(ray))
                return;
        }

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
                    Gate singleGate = tr.GetComponentInParent<Gate>();
                    if (singleGate != null)
                    {
                        gate = singleGate;
                        break;
                    }

                    GateDouble doubleGate = tr.GetComponentInParent<GateDouble>();
                    if (doubleGate != null)
                    {
                        gate = doubleGate;
                        break;
                    }
                }

                if (gate == null) return;

                if (ShooterController.Instance == null) return;
                if (!ShooterController.Instance.CanShuffleShootersOnGate(gate))
                {
                    if (UIManager.Instance != null)
                    {
                        var toast = UIManager.Instance.Get<UINotification>();
                        if (toast != null)
                        {
                            toast.ShowToast("This gate must have at least 2 shooters.");
                        }
                    }
                    return;
                }

                bool isFreeUse = Board.Instance.CurrentBoosterUsesFreeCharge;
                if (!isFreeUse)
                {
                    if (InventoryManager.Instance == null) return;
                    bool used = InventoryManager.Instance.UseBoosterType2();
                    if (!used)
                    {
                        if (UIManager.Instance != null)
                        {
                            var buy = UIManager.Instance.Get<UIBuyBooster>();
                            if (buy != null) buy.ShowForBooster(2);
                        }

                        Board.Instance.CurrentBooster = BoosterType.None;
                        Board.Instance.ResetBooster();
                        var ui = UIManager.Instance != null ? UIManager.Instance.Get<UIBottomInGame>() : null;
                        if (ui != null) ui.RefreshBoosterUIImmediate();
                        return;
                    }
                }

                TutorialManager.Instance?.CompleteBoosterUseTutorial(BoosterType.Shuffle);

                var shuffle = Instantiate(_shufflePrefab);

                StartCoroutine(shuffle.Hit(gate.RootTransform, onHit: () =>
                {
                    bool success = ShooterController.Instance.ShuffleShootersOnGate(gate);
                    if (!success)
                    {
                        InventoryManager.Instance.AddBoosterType2(1);
                        return;
                    }

                    AudioManager.Instance?.PlaySFX(SFXType.BoosterWand);
                    VibrateManager.Instance.PlayHaptic(HapticType.BoosterWand);
                    Board.Instance.CurrentBooster = BoosterType.None;
                    Board.Instance.ResetBooster();
                    var bottomUi = UIManager.Instance != null ? UIManager.Instance.Get<UIBottomInGame>() : null;
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
                            TutorialManager.Instance?.CompleteBoosterUseTutorial(BoosterType.Hammer);
                            Board.Instance.CurrentBooster = BoosterType.None;
                            var hammer = Instantiate(_hammerPrefab);
                            StartCoroutine(hammer.Hit(cube, onHit: () =>
                            {
                                AudioManager.Instance?.PlaySFX(SFXType.BoosterHammer);
                                VibrateManager.Instance.PlayHaptic(HapticType.BoosterHammer);
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
                                AudioManager.Instance?.PlaySFX(SFXType.BoosterDropper);
                                VibrateManager.Instance.PlayHaptic(HapticType.BoosterDropper);
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

    private bool TryActivateFallbackShooterOnGate(Ray ray)
    {
        if (Common.IsPointerOverUI()) return false;

        int mask = _gateLayer.value != 0 ? _gateLayer.value : ~0;
        if (mask != ~0)
        {
            int top = LayerMask.NameToLayer("Top");
            if (top >= 0) mask |= 1 << top;
            int gateLayer = LayerMask.NameToLayer("Gate");
            if (gateLayer >= 0) mask |= 1 << gateLayer;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, mask);
        if (hits == null || hits.Length == 0) return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform tr = hits[i].transform;
            if (tr == null) continue;

            Shooter shooter = tr.GetComponentInParent<Shooter>(includeInactive: true);
            if (shooter != null && shooter.Gate is Gate shooterGate)
            {
                if (shooterGate.TryActivateFallbackShooter(shooter))
                    return true;
            }

            Gate hitGate = tr.GetComponentInParent<Gate>(includeInactive: true);
            if (hitGate == null) continue;
            if (hitGate.TryActivateFallbackShooter(hitGate.CurrentShooter))
                return true;
        }

        return false;
    }
    private void OnTouchMoved()
    {

    }

    private void OnTouchEnded()
    {

    }

}
