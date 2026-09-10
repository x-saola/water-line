using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;

public class SpinWheelVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UIParticle _parentParticle;

    [SerializeField]
    private RectTransform _starTransform;

    [SerializeField]
    private RectTransform _arrowTransform;

    [SerializeField]
    private RectTransform _boardTransform;

    [Header("Timing")]
    [SerializeField]
    private float _autoPlayDelay = 5f;

    [Header("Board Animation")]
    [SerializeField]
    private float _boardRotationDuration = 3f;

    [SerializeField]
    private float _boardRotationAmount = -1440f;

    [Header("Star Animation")]
    [SerializeField]
    private float _starShakeDuration = 2f;

    [SerializeField]
    private Vector3 _starShakeStrength = new Vector3(0.1f, 0.1f, 0f);

    [SerializeField]
    private int _starShakeVibrato = 15;

    [SerializeField]
    private float _starShakeRandomness = 90f;

    [Header("Arrow Animation")]
    [SerializeField]
    private float _arrowRotationAmount = 30f;

    [SerializeField]
    private float _arrowRotationDuration = 0.15f;

    [SerializeField]
    private int _arrowLoopCount = 20;

    public void Awake()
    {
        if (_parentParticle != null)
        {
            _parentParticle.Stop();
        }
    }

    public void OnDestroy()
    {
        DOTween.Kill(_arrowTransform);
        DOTween.Kill(_starTransform);
        DOTween.Kill(_boardTransform);
    }

    void Start()
    {
        Invoke("PlayEffect", _autoPlayDelay);
    }

    [ContextMenu("Play")]
    public void PlayEffect()
    {
        _parentParticle?.Play();

        Sequence sequence = DOTween.Sequence().SetDelay(1.5f);

        sequence.Append(
            _boardTransform
                .DORotate(
                    new Vector3(0, 0, _boardRotationAmount),
                    _boardRotationDuration,
                    RotateMode.LocalAxisAdd
                )
                .SetEase(Ease.InOutQuad)
        );

        sequence.Join(
            _starTransform
                .DOShakeScale(
                    _starShakeDuration,
                    _starShakeStrength,
                    _starShakeVibrato,
                    _starShakeRandomness,
                    false
                )
                .SetEase(Ease.OutElastic)
        );
        sequence.Join(
            _arrowTransform
                .DOLocalRotate(new Vector3(0, 0, _arrowRotationAmount), _arrowRotationDuration)
                .SetEase(Ease.OutSine)
                .SetLoops(_arrowLoopCount, LoopType.Yoyo)
        );
        Invoke("PlayEffect", _autoPlayDelay);
    }

#if UNITY_EDITOR
    [ContextMenu("Load Defaults")]
    private void LoadDefaults()
    {
        _autoPlayDelay = 5f;
        _boardRotationDuration = 3f;
        _boardRotationAmount = -1440f;
        _starShakeDuration = 2f;
        _starShakeStrength = new Vector3(0.1f, 0.1f, 0f);
        _starShakeVibrato = 15;
        _starShakeRandomness = 90f;
        _arrowRotationAmount = 30f;
        _arrowRotationDuration = 0.15f;
        _arrowLoopCount = 20;
    }
#endif
}
