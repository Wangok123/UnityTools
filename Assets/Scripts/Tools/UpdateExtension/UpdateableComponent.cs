using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableComponent : MonoBehaviour, IUpdateable
{
    public virtual void OnUpdate(float dt)
    {
        
    }
}
