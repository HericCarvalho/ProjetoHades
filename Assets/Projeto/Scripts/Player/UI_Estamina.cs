using UnityEngine;
using UnityEngine.UI;

public class UI_Estamina : MonoBehaviour
{
    public MovimentaçãoPlayer player; // Referência ao script do jogador
    public Image fillImage;       // A parte que enche/esvazia

    void Update()
    {
        float staminaPercent = player.GetCurrentStamina() / player.MaxEstamina;
        fillImage.fillAmount = staminaPercent;
    }
}
