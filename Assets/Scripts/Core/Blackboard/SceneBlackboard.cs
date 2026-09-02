using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneBlackboard", menuName = "Scriptable Objects/Scene Blackboard")]
public class SceneBlackboard : ScriptableObject
{
    public interface IStorage { }

    private class Storage<T> : IStorage
    {
        public readonly Dictionary<int, T> Values = new();
    }

    private readonly Dictionary<Type, IStorage> _storages = new();
    private readonly Dictionary<int, Action> _events = new();

    public event Action<int> OnAnyStateChanged;

    private Storage<T> GetStorage<T>()
    {
        Type type = typeof(T);
        if (!_storages.TryGetValue(type, out IStorage storage))
        {
            storage = new Storage<T>();
            _storages[type] = storage;
        }
        return (Storage<T>)storage;
    }

    public void Set<T>(BlackboardKey key, T value)
    {
        int hash = key.Hash;
        Storage<T> storage = GetStorage<T>();

        if (storage.Values.TryGetValue(hash, out T existingValue))
        {
            if (EqualityComparer<T>.Default.Equals(existingValue, value))
                return;
        }

        storage.Values[hash] = value;

        if (_events.TryGetValue(hash, out Action callback))
        {
            callback?.Invoke();
        }

        OnAnyStateChanged?.Invoke(hash);
    }

    public bool TryGet<T>(BlackboardKey key, out T result)
    {
        Storage<T> storage = GetStorage<T>();
        return storage.Values.TryGetValue(key.Hash, out result);
    }

    public void Listen(BlackboardKey key, Action callback)
    {
        int hash = key.Hash;
        if (!_events.ContainsKey(hash))
        {
            _events[hash] = delegate { };
        }
        _events[hash] += callback;
    }

    public void StopListening(BlackboardKey key, Action callback)
    {
        if (_events.ContainsKey(key.Hash))
        {
            _events[key.Hash] -= callback;
        }
    }

    public void Clear()
    {
        _storages.Clear();
        _events.Clear();
    }

    public async UniTask<T> WaitUntilKeyMatches<T>(BlackboardKey key, T expectedValue, CancellationToken cancellationToken = default)
    {
        if (TryGet(key, out T currentValue) && EqualityComparer<T>.Default.Equals(currentValue, expectedValue))
            return currentValue;

        var utcs = new UniTaskCompletionSource<T>();

        void OnStateChanged()
        {
            if (TryGet(key, out T updatedValue) && EqualityComparer<T>.Default.Equals(updatedValue, expectedValue))
            {
                StopListening(key, OnStateChanged);
                utcs.TrySetResult(updatedValue);
            }
        }

        Listen(key, OnStateChanged);

        using (cancellationToken.Register(() =>
        {
            StopListening(key, OnStateChanged);
            utcs.TrySetCanceled(cancellationToken);
        }))
        {
            return await utcs.Task;
        }
    }

#if UNITY_EDITOR
    public IReadOnlyDictionary<Type, IStorage> Editor_GetStorages() => _storages;
#endif
}