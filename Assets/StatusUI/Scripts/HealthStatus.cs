using UnityEngine;

public class HealthStatus : MonoBehaviour
{
    // Percentage of health
    public enum Enemy
    {
        Monster = 7,
        Default = Monster
    }

    public Transform indicator;
    public TextMesh percentage;

    float _value = 1f;
    Vector3 _position;
    Vector3 _scale;

    void Start()
    {
        _position = indicator.localPosition;
        _scale = indicator.localScale;
    }

    public float value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = Mathf.Max(0f, Mathf.Min(1f, value));
            percentage.text = $"{(_value * 100f).ToString("0")}%";
            indicator.localScale = new Vector3(
                _scale.x * _value,
                _scale.y,
                _scale.z);
            indicator.localPosition = new Vector3(
                _position.x - (1f - _value) / 2,
                _position.y, 
                _position.z);
        }
    }
    
    public float loose(Enemy enemy = Enemy.Default)
    {
        value -= ((float)enemy)/100f;
        return value;
    }
}
