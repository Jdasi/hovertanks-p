using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public abstract class FactoredFloat_Drawer : PropertyDrawer
{
	private const string PROPERTY_NAME = "_base";

	private GUIContent _fakeLeftColumn;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		if (_fakeLeftColumn == null)
		{
			_fakeLeftColumn = new GUIContent(" ");
		}

		if (property == null)
		{
			return;
		}

		EditorGUI.LabelField(position, label);
		var floatProperty = property.FindPropertyRelative(PROPERTY_NAME);

		if (floatProperty == null)
		{
			return;
		}

		Draw(position, floatProperty, _fakeLeftColumn);
    }

	protected abstract void Draw(Rect position, SerializedProperty property, GUIContent label);
}

[CustomPropertyDrawer(typeof(FactoredFloat))]
public class FactoredFloat_DrawerBasic : FactoredFloat_Drawer
{
    protected override void Draw(Rect position, SerializedProperty property, GUIContent label)
    {
		EditorGUI.PropertyField(position, property, label);
    }
}

[CustomPropertyDrawer(typeof(FactoredFloatRangeAttribute))]
public class FactoredFloat_DrawerRange : FactoredFloat_Drawer
{
    protected override void Draw(Rect position, SerializedProperty property, GUIContent label)
    {
		if (!(attribute is FactoredFloatRangeAttribute range))
		{
			return;
		}

		EditorGUI.Slider(position, property, range.min, range.max, label);
    }
}
#endif

public class FactoredFloatRangeAttribute : PropertyAttribute
{
	public float min;
	public float max;

	public FactoredFloatRangeAttribute(float min, float max)
	{
		this.min = min;
		this.max = max;
	}
}

[Serializable]
public struct FactoredFloat
{
	public float Base
	{
		get => _base;
		set
		{
			if (_base == value)
			{
				return;
			}

			_base = value;
			RefreshValue();
		}
	}

	public float Value
	{
		get
		{
			// ensure valid value on peek
			if (_value != _base
				&& _factors == null)
			{
				RefreshValue();
			}

			return _value;
		}
	}

	public static implicit operator float(FactoredFloat ff) => ff.Value;

	[SerializeField] float _base;

	private List<float> _factors;
	private float _value;

	public FactoredFloat(float val)
	{
		_base = 0;
		_factors = null;
		_value = 0;

		Base = val;
	}

	public void AddFactor(float factor)
	{
		if (factor == 1)
		{
			return;
		}

		if (_factors == null)
		{
			_factors = new List<float>() { factor };
		}
		else
		{
			_factors.Add(factor);
		}

		RefreshValue();
	}

	public void RemoveFactor(float factor)
    {
        if (_factors == null)
        {
            return;
        }

        if (!_factors.Remove(factor))
        {
            return;
        }

        if (_factors.Count == 0)
        {
            _factors = null;
        }
        else
        {
			RefreshValue();
        }
    }

	private void RefreshValue()
	{
		_value = Base;

		if (_factors == null
			|| _value == 0)
		{
			return;
		}

		for (int i = 0; i < _factors.Count; ++i)
		{
			_value *= _factors[i];
		}
	}
}
