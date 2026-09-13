using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ISequence
{
    UniTask ExecuteAsync();
}