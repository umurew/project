using UnityEngine;

public class ServiceBase : MonoBehaviour, IService
{
    public bool Constructed { get; private set; }

    public virtual void Construct()
    {
        Constructed = true;
    }
}
