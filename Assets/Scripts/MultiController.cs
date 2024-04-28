using CustomRP.Examples;
using NaughtyAttributes;
using UnityEngine;

public class MultiController : MonoBehaviour
{
    [Button]
    private void RandomColorSetup()
    {
        PerObjectMaterialProperties[] array = GetComponentsInChildren<PerObjectMaterialProperties>();
        foreach (PerObjectMaterialProperties properties in array)
        {
            properties.RandomColor();
        }
    }
}