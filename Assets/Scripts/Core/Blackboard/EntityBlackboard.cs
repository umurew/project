using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class EntityBlackboard : MonoBehaviour
{
    [Header("Initial States")]
    [SerializeField] private List<InitialBool> _initialBools = new();
    [SerializeField] private List<InitialInt> _initialInts = new();
    [SerializeField] private List<InitialFloat> _initialFloats = new();

    [Serializable] public struct InitialBool { public BlackboardKey Key; public bool Value; }
    [Serializable] public struct InitialInt { public BlackboardKey Key; public int Value; }
    [Serializable] public struct InitialFloat { public BlackboardKey Key; public float Value; }

    public interface IStorage { }

    private class Storage<T> : IStorage
    {
        public readonly Dictionary<int, T> Values = new();
    }

    private readonly Dictionary<Type, IStorage> _storages = new();
    private readonly Dictionary<int, Action> _events = new();

    private void Awake()
    {
        foreach (var item in _initialBools)
            Set(item.Key, item.Value);

        foreach (var item in _initialInts)
            Set(item.Key, item.Value);

        foreach (var item in _initialFloats)
            Set(item.Key, item.Value);
    }

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
    }

    public bool TryGet<T>(BlackboardKey key, out T result)
    {
        Storage<T> storage = GetStorage<T>();
        return storage.Values.TryGetValue(key.Hash, out result);
    }

    public void RegisterCallback(BlackboardKey key, Action callback)
    {
        int hash = key.Hash;
        if (!_events.ContainsKey(hash))
        {
            _events[hash] = delegate { };
        }
        _events[hash] += callback;
    }

    public void UnregisterCallback(BlackboardKey key, Action callback)
    {
        if (_events.ContainsKey(key.Hash))
        {
            _events[key.Hash] -= callback;
        }
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
                UnregisterCallback(key, OnStateChanged);
                utcs.TrySetResult(updatedValue);
            }
        }

        RegisterCallback(key, OnStateChanged);

        using (cancellationToken.Register(() =>
        {
            UnregisterCallback(key, OnStateChanged);
            utcs.TrySetCanceled(cancellationToken);
        }))
        {
            return await utcs.Task;
        }
    }

    public async UniTask<T> WaitUntilHasValue<T>(BlackboardKey key, CancellationToken cancellationToken = default)
    {
        if (TryGet(key, out T currentValue))
            return currentValue;

        var utcs = new UniTaskCompletionSource<T>();

        void OnStateChanged()
        {
            if (TryGet(key, out T updatedValue))
            {
                UnregisterCallback(key, OnStateChanged);
                utcs.TrySetResult(updatedValue);
            }
        }

        RegisterCallback(key, OnStateChanged);

        using (cancellationToken.Register(() =>
        {
            UnregisterCallback(key, OnStateChanged);
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