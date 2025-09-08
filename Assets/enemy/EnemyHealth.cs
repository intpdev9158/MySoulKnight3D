using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{

    public int maxHp = 20;
    public int currentHp;

    [Header("UI")]
    public Slider healthSlider;
    public float hpLerpSpeed = 8f;

    [Header("Hit Flash")]
    public Renderer[] renderers; // 색을 바꿔줄 렌더러들
    public Color flashColor = Color.red;
    public float flashDuration = 0.08f;

    private Color[] _baseColors;
    private bool _flashing;
    private float _targetFill; // 체력 비율


    void Awake()
    {
        currentHp = maxHp;
        if (healthSlider)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            _targetFill = 1f;
            healthSlider.value = 1f;
        }

        if (renderers != null && renderers.Length > 0) // 렌더 추가했냐?
        {
            _baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_Color")) // 이 material은 color라는 속성을 가지고 있냐?
                    _baseColors[i] = renderers[i].material.color;
                else
                    _baseColors[i] = Color.white; // 지원안하면 기본 흰색으로
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (healthSlider)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, _targetFill, Time.deltaTime * hpLerpSpeed);
        }
    }

    public void TakeDamage(int dmg)
    {
        currentHp = Mathf.Max(0, currentHp - dmg); // 0 이하로 내려가지 않게 클램프
        _targetFill = (float)currentHp / maxHp;

        if (!_flashing) StartCoroutine(Flash());

        if (currentHp <= 0)
        {
            Die();
        }

    }

    IEnumerator Flash()
    {
        _flashing = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = flashColor;
        }
        yield return new WaitForSeconds(flashDuration);

        //원복
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = _baseColors[i];
        }
        _flashing = false;
    }

    void Die()
    {
        Destroy(gameObject);
    }

}
