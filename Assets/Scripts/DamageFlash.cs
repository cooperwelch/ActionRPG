using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private Color _secondFlashColor = new Color(1f, 0.64705884f, 0f, 1f);
    [SerializeField] private float _flashTime;
    [SerializeField] private float _flashAlpha = 0.7f;
    [SerializeField] private int _flashCount;

    private SpriteRenderer _spriteRenderer;
    private Material _material;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _material = _spriteRenderer.material;
    }

    public void Flash()
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            _material.SetColor("_FlashColor", _flashColor);
            _material.SetFloat("_FlashAmount", _flashAlpha);
            yield return new WaitForSeconds(_flashTime);

            _material.SetColor("_FlashColor", _secondFlashColor);
            _material.SetFloat("_FlashAmount", _flashAlpha);
            yield return new WaitForSeconds(_flashTime);

            _material.SetFloat("_FlashAmount", 0f);
            yield return new WaitForSeconds(_flashTime);
        }

        _flashRoutine = null;
    }
}
