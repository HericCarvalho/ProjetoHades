using UnityEngine;
using System.Collections.Generic;

public class MannequinManager : MonoBehaviour
{
    public QuestSO quest;
    public string objectiveSpawnId = "spawn_parts";
    public string objectiveCollectPrefix = "collect_part_"; // ex: collect_part_1, collect_part_2
    public Transform[] spawnPoints;
    public GameObject partPrefab;
    public int partsToSpawn = 2;

    public void StartMannequinQuest()
    {
        // spawn parts
        for (int i = 0; i < partsToSpawn && i < spawnPoints.Length; i++)
        {
            Instantiate(partPrefab, spawnPoints[i].position, Quaternion.identity);
        }
        // opcional: marca que spawn ocorreu
        QuestManager.Instance?.MarkObjective(quest, objectiveSpawnId, 1);
    }
}
