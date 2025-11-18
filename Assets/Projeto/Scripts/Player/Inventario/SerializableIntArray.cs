using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableIntArray
{
    public List<int> values = new List<int>();

    // Converte para array fácil
    public int[] ToArray()
    {
        if (values == null) return new int[0];
        return values.ToArray();
    }
}
