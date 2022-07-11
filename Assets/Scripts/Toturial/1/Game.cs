using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
  public Transform prefab;
  public KeyCode createKey = KeyCode.C;

	void Update()
	{
		if (Input.GetKeyDown(createKey))
		{
			Instantiate(prefab);
		}
	}
}
