using UnityEngine;

public interface IService
{
    bool Constructed { get; }
    void Construct();
}
