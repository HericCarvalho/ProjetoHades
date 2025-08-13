using UnityEngine;
using UnityEngine.UI;

public class UI_Estamina : MonoBehaviour
{
    public Movimenta��oPlayer player; // Refer�ncia ao script do jogador
    public Image fillImage;       // A parte que enche/esvazia

    void Update()
    {
        float staminaPercent = player.GetCurrentStamina() / player.MaxEstamina;
        fillImage.fillAmount = staminaPercent;
    }
}
