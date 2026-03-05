using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2f;

    private float elapsed;
    private Color startColor;

    public void Initialize(float damage)
    {
        text.text = Mathf.RoundToInt(damage).ToString();
        startColor = text.color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        transform.forward = Camera.main.transform.forward;
        
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}