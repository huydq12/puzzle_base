using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AZUR;

public class LineController : Singleton<LineController>
{
    [SerializeField] private LayerMask _cubeLayer;
    [SerializeField] private LayerMask _gateLayer;
    [SerializeField] private Hammer _hammerPrefab;
    [SerializeField] private Shuffle _shufflePrefab;
    private bool _isShowingFallbackRainbowReward;
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
        if (IsGameplayTouchBlocked())
        {
            return;
        }

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

    private bool IsGameplayTouchBlocked()
    {
        if (UIManager.Instance != null && UIManager.Instance.HasActivePopup)
        {
            return true;
        }

        return IsPointerOverBlockingUI();
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
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, mask);
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

        if (TryRaycastCube(ray, out RaycastHit hit))
        {
            CubeLine cube = hit.transform != null ? hit.transform.GetComponentInParent<CubeLine>() : null;
            if (cube != null)
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

    private bool TryRaycastCube(Ray ray, out RaycastHit hit)
    {
        int mask = GetCubeTouchMask();
        if (Physics.Raycast(ray, out hit, 1000f, mask))
        {
            if (hit.transform != null && hit.transform.GetComponentInParent<CubeLine>() != null)
                return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, mask);
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Transform tr = hits[i].transform;
            if (tr == null) continue;
            if (tr.GetComponentInParent<CubeLine>() == null) continue;

            hit = hits[i];
            return true;
        }

        hit = default;
        return false;
    }

    private int GetCubeTouchMask()
    {
        int mask = _cubeLayer.value != 0 ? _cubeLayer.value : ~0;
        if (mask == ~0)
            return mask;

        int cubeLayer = LayerMask.NameToLayer("Cube");
        if (cubeLayer >= 0)
            mask |= 1 << cubeLayer;

        int topLayer = LayerMask.NameToLayer("Top");
        if (topLayer >= 0)
            mask |= 1 << topLayer;

        return mask;
    }

    private bool TryActivateFallbackShooterOnGate(Ray ray)
    {
        if (IsPointerOverBlockingUI()) return false;
        if (_isShowingFallbackRainbowReward) return true;

        int mask = _gateLayer.value != 0 ? _gateLayer.value : ~0;
        if (mask != ~0)
        {
            int top = LayerMask.NameToLayer("Top");
            if (top >= 0) mask |= 1 << top;
            int gateLayer = LayerMask.NameToLayer("Gate");
            if (gateLayer >= 0) mask |= 1 << gateLayer;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, mask);
        if (hits == null || hits.Length == 0) return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform tr = hits[i].transform;
            if (tr == null) continue;

            Shooter shooter = tr.GetComponentInParent<Shooter>(includeInactive: true);
            if (shooter != null && shooter.Gate is Gate shooterGate)
            {
                if (shooterGate.CanActivateFallbackShooter(shooter))
                {
                    ShowFallbackRainbowRewarded(shooterGate, shooter);
                    return true;
                }
            }

            Gate hitGate = tr.GetComponentInParent<Gate>(includeInactive: true);
            if (hitGate == null) continue;
            if (hitGate.CanActivateFallbackShooter(hitGate.CurrentShooter))
            {
                ShowFallbackRainbowRewarded(hitGate, hitGate.CurrentShooter);
                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        if (results.Count == 0)
            return false;

        for (int i = 0; i < results.Count; i++)
        {
            GameObject go = results[i].gameObject;
            if (go == null) continue;

            if (go.GetComponentInParent<Selectable>() != null)
                return true;

            if (go.GetComponentInParent<ScrollRect>() != null)
                return true;

            if (HasBlockingUiHandler(go))
                return true;
        }

        return false;
    }

    private static bool HasBlockingUiHandler(GameObject go)
    {
        MonoBehaviour[] behaviours = go.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null) continue;

            if (behaviour is IPointerClickHandler ||
                behaviour is IPointerDownHandler ||
                behaviour is IBeginDragHandler ||
                behaviour is IDragHandler)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowFallbackRainbowRewarded(Gate gate, Shooter shooter)
    {
        if (gate == null || shooter == null) return;

        const string placement = "fallback_rainbow_unlock";
        var notification = UIManager.Instance != null ? UIManager.Instance.Get<UINotification>() : null;
        notification?.HideNow();
        _isShowingFallbackRainbowReward = true;
        AnalyticsBridge.OnRewardedAdRequested(placement);

        bool shown = AzurAds.ShowRewarded(
            onRewardGranted: () =>
            {
                _isShowingFallbackRainbowReward = false;
                AnalyticsBridge.OnRewardedAdRewardGranted(placement);
                gate.TryActivateFallbackShooter(shooter);
            },
            placement: placement,
            onClosedWithoutGrant: () =>
            {
                _isShowingFallbackRainbowReward = false;
                AnalyticsBridge.OnRewardedAdClosedWithoutGrant(placement);
                notification?.ShowRewardNotGrantedToast();
            });

        if (!shown)
        {
            _isShowingFallbackRainbowReward = false;
            AnalyticsBridge.OnRewardedAdUnavailable(placement);
            notification?.ShowRewardedUnavailableToast();
        }
    }
    private void OnTouchMoved()
    {

    }

    private void OnTouchEnded()
    {

    }

}
