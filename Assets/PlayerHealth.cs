using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 40;
    int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void ReceberDano(int damage)
    {
        currentHealth -= damage;
        Debug.Log("PLAYER TOMOU DANO: " + damage);

        if (currentHealth <= 0)
            Morrer();
    }

   public void Morrer()
    {
        Debug.Log("PLAYER MORREU");
        // animação, tela de morte, etc
    }
}
