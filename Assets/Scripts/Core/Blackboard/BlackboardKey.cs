using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct BlackboardKey : IEquatable<BlackboardKey>
{
    [SerializeField] private string _keyName;
    private int _hash;

#if UNITY_EDITOR
    // A global registry to translate hashes back to strings, ONLY in the Unity Editor
    public static readonly Dictionary<int, string> EditorNameRegistry = new();
#endif

    public int Hash
    {
        get
        {
            if (_hash == 0 && !string.IsNullOrEmpty(_keyName))
            {
                _hash = Animator.StringToHash(_keyName);
#if UNITY_EDITOR
                EditorNameRegistry[_hash] = _keyName;
#endif
            }
            return _hash;
        }
    }

    public BlackboardKey(string keyName)
    {
        _keyName = keyName;
        _hash = Animator.StringToHash(keyName);
#if UNITY_EDITOR
        EditorNameRegistry[_hash] = keyName;
#endif
    }

    public bool Equals(BlackboardKey other) => Hash == other.Hash;
    public override int GetHashCode() => Hash;
}