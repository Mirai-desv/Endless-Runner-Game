using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Button))]
public class KeyboardButtonVisual : MonoBehaviour
{
    [Header("Keyboard")]
    public Key[] keys;

    private Button button;
    private Image buttonImage;

    private Sprite normalSprite;
    private Sprite pressedSprite;

    private bool wasHeld;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = button.targetGraphic as Image;

        if (buttonImage == null)
        {
            Debug.LogError(
                "[KeyboardButtonVisual] Button cần có Image!",
                this
            );
            enabled = false;
            return;
        }

        normalSprite = buttonImage.sprite;

        // Lấy Pressed Sprite đã thiết lập trong Inspector
        pressedSprite = button.spriteState.pressedSprite;
    }

    void Update()
    {
        if (button == null || buttonImage == null)
            return;

        if (!button.interactable)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool isHeld = false;

        if (keys != null)
        {
            foreach (Key key in keys)
            {
                if (key != Key.None && keyboard[key].isPressed)
                {
                    isHeld = true;
                    break;
                }
            }
        }

        if (isHeld == wasHeld)
            return;

        wasHeld = isHeld;

        if (isHeld)
        {
            // Nhấn bàn phím -> đổi sprite
            if (pressedSprite != null)
                buttonImage.overrideSprite = pressedSprite;
        }
        else
        {
            // Nhả bàn phím -> trở lại sprite bình thường
            buttonImage.overrideSprite = null;
        }
    }

    void OnDisable()
    {
        wasHeld = false;

        if (buttonImage != null)
            buttonImage.overrideSprite = null;
    }
}