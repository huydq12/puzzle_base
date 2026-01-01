using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITutorial : UIPopup
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Sprite _iconIce;
    [SerializeField] private Sprite _iconGate;
    [SerializeField] private Sprite _iconLockItem;
    [SerializeField] private Sprite _iconLockItemColor;
    [SerializeField] private Sprite _iconScrew;

    void Start()
    {
        _btnClose.onClick.AddListener(() =>
        {
            Hide();
        });
        switch (TutorialManager.Instance.CurrentTutorialType)
        {
            case TutorialType.Ice:
                {
                    _icon.sprite = _iconIce;
                    _title.text = "Ice";
                    _description.text = "Clear the tiles underneath to melt the ice and free the frozen cell.";
                    break;
                }
            case TutorialType.Gate:
                {
                    _icon.sprite = _iconGate;
                    _title.text = "Gate";
                    _description.text = "Clear the blocking tiles to open the gate and move forward.";
                    break;
                }
            case TutorialType.LockItem:
                {
                    _icon.sprite = _iconLockItem;
                    _title.text = "Locked Hexagon";
                    _description.text = "Clear the related tiles to unlock the Hexagon.";
                    break;
                }
            case TutorialType.LockItemColor:
                {
                    _icon.sprite = _iconLockItemColor;
                    _title.text = "Color Lock";
                    _description.text = "Clear the tiles with the correct color to unlock the Hexagon.";
                    break;
                }
            case TutorialType.Screw:
                {
                    _icon.sprite = _iconScrew;
                    _title.text = "Screw";
                    _description.text = "Clear the tiles to remove the screw and free the obstacle.";
                    break;
                }
        }
    }
}
