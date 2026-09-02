using System;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _player;

    [Space(10)]
    [SerializeField] private InputProvider _inputProviderPrefab;

    private SceneBlackboard _sceneBlackboard;
    private InputProvider _inputProvider;

    private void Awake()
    {
        _sceneBlackboard = ScriptableObject.CreateInstance<SceneBlackboard>();

        _inputProvider = Instantiate(_inputProviderPrefab, transform);
        _inputProvider.Construct();
        _inputProvider.EnablePlayerControls();

        if (!_player.TryGetComponent<PlayerMovement>(out PlayerMovement playerMovement))
            throw new NullReferenceException("Couldn't get PlayerMovement from Player.");
        else
            playerMovement.Construct(_inputProvider, _mainCamera);

        if (!_player.TryGetComponent<PlayerInteraction>(out PlayerInteraction playerInteraction))
            throw new NullReferenceException("Couldn't get PlayerInteraction from Player.");
        else
            playerInteraction.Construct(_inputProvider, _mainCamera);
    }
}