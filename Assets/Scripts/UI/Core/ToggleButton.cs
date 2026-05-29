using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToggleButton : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;
    [SerializeField] private TextMeshProUGUI stateLabel;

    [Header("Visual - Sprite")]
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Visual - Color")]
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Visual - Text")]
    [SerializeField] private string onText = "ON";
    [SerializeField] private string offText = "OFF";

    [Header("State")]
    [SerializeField] private bool isOn;

    public UnityEvent<bool> OnValueChanged;

    public bool IsOn => isOn;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
        {
            return;
        }

        Toggle();
    }

    public void Toggle()
    {
        SetState(!isOn, true);
    }

    public void SetState(bool value, bool notify = true)
    {
        if (isOn == value)
        {
            RefreshVisual();
            return;
        }

        isOn = value;
        RefreshVisual();

        if (notify)
        {
            OnValueChanged?.Invoke(isOn);
        }
    }

    private void RefreshVisual()
    {
        if (targetImage != null)
        {
            targetImage.color = isOn ? onColor : offColor;

            Sprite sprite = isOn ? onSprite : offSprite;
            if (sprite != null)
            {
                targetImage.sprite = sprite;
            }
        }

        if (stateLabel != null)
        {
            stateLabel.text = isOn ? onText : offText;
        }
    }
}
