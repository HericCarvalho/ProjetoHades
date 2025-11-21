using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    public Text textoQuest;

    public void SetDescription(string desc)
    {
        textoQuest.text = desc;
    }

    public void RiscarQuest()
    {
        textoQuest.text = $"<s>{textoQuest.text}</s>";
    }
}
