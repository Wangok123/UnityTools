using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ShapeFactory : ScriptableObject
{
  [SerializeField]
  Shape[] prefabs;

  public Shape Get(int ShapeID)
  {
    return Instantiate(prefabs[ShapeID]);
  }

  public Shape GetRandom()
  {
    return Get(Random.Range(0, prefabs.Length));
  }
}
