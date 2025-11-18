using System.Collections.Generic;
using UnityEngine;

public class FuseboxVisual : MonoBehaviour
{
    [Header("Referências dos Prefabs dos Fusiveis (3 tipos)")]
    public GameObject fusePrefabTipoA;
    public GameObject fusePrefabTipoB;
    public GameObject fusePrefabTipoC;

    [Header("Posições onde os fusiveis serão colocados na caixa (3 slots)")]
    public Transform[] slotPositions = new Transform[3];

    // Objetos instanciados em cada slot
    private GameObject[] spawnedFuses = new GameObject[3];

    /// <summary>
    /// Atualiza visualmente os fusiveis na caixa.
    /// </summary>
    /// <param name="slotTypes">Lista de 3 inteiros representando os tipos:
    /// 0 = vazio
    /// 1 = Tipo A
    /// 2 = Tipo B
    /// 3 = Tipo C
    /// </param>
    public void UpdateVisual(int[] slotTypes)
    {
        if (slotTypes.Length != 3)
        {
            Debug.LogError("[FuseboxVisual] slotTypes precisa ter exatamente 3 valores!");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            // Remove o que já estiver ali
            if (spawnedFuses[i] != null)
            {
                Destroy(spawnedFuses[i]);
                spawnedFuses[i] = null;
            }

            // 0 = slot vazio
            if (slotTypes[i] == 0)
                continue;

            // Instancia o prefab correto
            GameObject prefab = GetPrefabByType(slotTypes[i]);

            if (prefab == null)
            {
                Debug.LogError("[FuseboxVisual] Tipo de fusível inválido: " + slotTypes[i]);
                continue;
            }

            // Instancia no slot
            GameObject instance = Instantiate(prefab, slotPositions[i]);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            spawnedFuses[i] = instance;
        }
    }

    private GameObject GetPrefabByType(int type)
    {
        switch (type)
        {
            case 1: return fusePrefabTipoA;
            case 2: return fusePrefabTipoB;
            case 3: return fusePrefabTipoC;
            default: return null;
        }
    }
}
