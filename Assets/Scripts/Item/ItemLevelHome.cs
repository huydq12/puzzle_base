using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ItemLevelHome : MonoBehaviour
{
    [SerializeField] 
    private RectTransform rectTransform;

    [SerializeField]
    private TextMeshProUGUI txtLevelCurrent;

    [SerializeField]
    private TextMeshProUGUI txtLevelNormal;

    [SerializeField]
    private TextMeshProUGUI txtLevelHard;

    [SerializeField]
    private TextMeshProUGUI txtLevelVeryHard;

    [SerializeField]
    private GameObject stateCurrent;

    [SerializeField]
    private GameObject stateNormal;

    [SerializeField]
    private GameObject stateHard;

    [SerializeField]
    private GameObject stateVeryHard;

    [SerializeField]
    private GameObject objHighlightNormal;

    [SerializeField]
    private GameObject objHighlightHard;

    [SerializeField]
    private GameObject objHighlightVeryHard;

    private Transform _tranTop;

    private Transform _tranBot;

    [SerializeField]
    private CanvasGroup _canvasGroup;

  
 }
