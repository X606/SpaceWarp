﻿using System;
using JetBrains.Annotations;

namespace SpaceWarp.API.Configuration;

public class SaveConfigEntry : IConfigEntry
{
    public IConfigEntry BoundEntry;

    internal object InMemoryValue;
    internal object DefaultValue;
    public event Action<object, object> Callbacks;

    public SaveConfigEntry(string section, string key, string description, Type valueType, object defaultValue, IValueConstraint constraint)
    {
        Section = section;
        Key = key;
        Description = description;
        ValueType = valueType;
        DefaultValue = defaultValue;
        Constraint = constraint;
        InMemoryValue = defaultValue;
    }

    public object Value
    {
        get
        {
            return BoundEntry != null ? BoundEntry.Value : InMemoryValue;
        }
        set
        {
            if (BoundEntry != null)
            {
                Callbacks?.Invoke(BoundEntry.Value, value);
                BoundEntry.Value = value;
            }
            else
            {
                Callbacks?.Invoke(InMemoryValue, value);
                InMemoryValue = value;
            }
        }
    }

    public Type ValueType { get; }
    public T Get<T>() where T : class
    {
        
        if (!typeof(T).IsAssignableFrom(ValueType))
        {
            throw new InvalidCastException($"Cannot cast {ValueType} to {typeof(T)}");
        }

        return Value as T;
    }

    public void Set<T>(T value)
    {
        if (!ValueType.IsAssignableFrom(typeof(T)))
        {
            throw new InvalidCastException($"Cannot cast {ValueType} to {typeof(T)}");
        }
        if (Constraint != null)
        {
            if (!Constraint.IsConstrained(value)) return;
        }

        Value = Convert.ChangeType(value, ValueType);
    }

    public string Section { get; }
    public string Key { get; }

    public string Description { get; }
    public IValueConstraint Constraint { get; }
    public void RegisterCallback(Action<object, object> valueChangedCallback)
    {
        Callbacks += valueChangedCallback;
    }

    // The moment we bind to a new config file, reset the defaults
    internal void Bind(JsonConfigFile newConfigFile)
    {
        BoundEntry = newConfigFile.Bind(ValueType, Section, Key, InMemoryValue, Description, Constraint);
        InMemoryValue = DefaultValue;
    }

    internal void Reset()
    {
        BoundEntry = null;
        InMemoryValue = DefaultValue;
    }
}