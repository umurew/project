using Cysharp.Threading.Tasks;
using UnityEngine;

public class SampleSequence : MonoBehaviour, ISequence
{
    public async UniTask ExecuteAsync()
    {
        SequenceBlackboard sequenceBlackboard = ScriptableObject.CreateInstance<SequenceBlackboard>();
        Debug.Log("SampleSequence started executing.");
        
        await UniTask.Yield();
    }
}