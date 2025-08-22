using System.Collections;
using UnityEngine;

public class FlashColorOnce : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_BaseColor"); // o "_Color" según tu shader
    [SerializeField] Renderer targetRenderer;
    [SerializeField] Color flashColor = Color.red;
    [SerializeField] float seconds = 0.2f;

    MaterialPropertyBlock _mpb;
    Color _originalColor;
    bool _cached;

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_mpb);
        if (!_mpb.HasVector(ColorId))
        {
            _originalColor = targetRenderer.sharedMaterial.HasProperty(ColorId)
                ? targetRenderer.sharedMaterial.GetColor(ColorId)
                : Color.white;
        }
        else
        {
            _originalColor = _mpb.GetColor(ColorId);
        }
        _cached = true;
    }

    [ContextMenu("Test Flash")]
    public void Flash() => StartCoroutine(FlashRoutine());

    IEnumerator FlashRoutine()
    {
        if (!_cached) Awake();

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorId, flashColor);
        targetRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForSeconds(seconds);

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorId, _originalColor);
        targetRenderer.SetPropertyBlock(_mpb);
    }
}