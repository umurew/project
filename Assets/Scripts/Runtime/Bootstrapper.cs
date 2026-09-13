using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[RequireComponent(typeof(SampleSequence))]
public class Bootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _player;

    [Space(10)]
    [SerializeField] private InputProvider _inputProviderPrefab;

    private InputProvider _inputProvider;

    private async UniTaskVoid Awake()
    {
        _inputProvider = Instantiate(_inputProviderPrefab, transform);
        _inputProvider.Construct();
        _inputProvider.EnablePlayerControls();

        if (_player.TryGetComponent<PlayerController>(out PlayerController playerController))
            playerController.Construct(_inputProvider, _mainCamera);
        else
            throw new NullReferenceException("Couldn't get PlayerController from Player.");

        if (_player.TryGetComponent<InteractionController>(out InteractionController interactionController))
            interactionController.Construct(_inputProvider, _mainCamera);
        else
            throw new NullReferenceException("Couldn't get InteractionController from Player.");

        SampleSequence sampleSequence = GetComponent<SampleSequence>();
        await sampleSequence.ExecuteAsync();
    }
}