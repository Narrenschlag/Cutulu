namespace Cutulu.Core;

using System.Collections.Generic;
using System.Reflection;
using System;

public partial class ParameterManager
{
    private static readonly Dictionary<CacheKey, ParameterManager> Cache = [];
    public static void ClearCache() => Cache.Clear();

    public readonly ushort PropertyCount;
    public readonly ushort FieldCount;
    public readonly Type BaseType;
    public readonly Type Type;

    private readonly ParameterInfo[] Parameters;
    private readonly string[] NameToIdx;

    private ParameterManager(Type type, Type baseType, Attribute[] include, Attribute[] exclude)
    {
        BaseType = baseType;
        Type = type;

        PropertyInfo[] properties = type.GetSetProperties();
        FieldInfo[] fields = type.GetFields();

        SwapbackArray<(string Name, ParameterInfo Param)> parameters = [];

        // Include GetSet Properties and Fields
        if (baseType != typeof(object) && type.IsSubclassOf(BaseType))
        {
            properties = properties[..(properties.Length - Open(BaseType).PropertyCount)];
            fields = fields[..(fields.Length - Open(BaseType).FieldCount)];
        }

        PropertyCount = 0;
        FieldCount = 0;

        // Setup NameToIdx
        var includeSpan = include.AsSpan();
        var excludeSpan = exclude.AsSpan();

        var propertySpan = properties.AsSpan();
        foreach (ref var property in propertySpan)
        {
            if (PassesFilter(property.GetCustomAttributes(), includeSpan, excludeSpan))
            {
                parameters.Add((PrepareString(property.Name), new ParameterInfo(property)));

                PropertyCount++;
            }
        }

        var fieldSpan = fields.AsSpan();
        foreach (ref var field in fieldSpan)
        {
            if (PassesFilter(field.GetCustomAttributes(), include.AsSpan(), exclude.AsSpan()))
            {
                parameters.Add((PrepareString(field.Name), new ParameterInfo(field)));

                FieldCount++;
            }
        }

        // Seal array by sorting
        NameToIdx = new string[parameters.Count];
        int i = 0;

        var nameSpan = parameters.AsSpan();
        foreach (ref var param in nameSpan)
            NameToIdx[i++] = param.Name;

        NameToIdx.Sort();

        // Apply parameters in sorted order
        Parameters = new ParameterInfo[parameters.Count];

        var paramSpan = parameters.AsSpan();
        foreach (ref var param in paramSpan)
        {
            i = NameToIdx.BinarySearch(param.Name);
            Parameters[i] = param.Param;
        }
    }

    private static bool PassesFilter(
        IEnumerable<Attribute> attributes,
        ReadOnlySpan<Attribute> include,
        ReadOnlySpan<Attribute> exclude)
    {
        // No filters = include everything
        if (include.IsEmpty && exclude.IsEmpty) return true;

        bool included = include.IsEmpty; // if no include filter, default to included
        bool excluded = false;

        foreach (var attr in attributes)
        {
            Type attrType = attr.GetType();

            if (!included)
                foreach (ref readonly var inc in include)
                    if (attrType == inc.GetType()) { included = true; break; }

            if (!excluded)
                foreach (ref readonly var exc in exclude)
                    if (attrType == exc.GetType()) { excluded = true; break; }

            if (included && excluded) break; // can't change anymore, early out
        }

        return included && !excluded;
    }

    public static ParameterManager Open<T, B>() where B : T => Open(typeof(T), typeof(B));

    public static ParameterManager Open<T>(Type baseType = null) => Open(typeof(T), baseType);

    /// <summary>
    /// Optimizes the whole process of getting a PropertyManager for a given type by caching it for repeated calls
    /// </summary>
    public static ParameterManager Open(Type type, Type baseType = null)
    => OpenInternal(type, baseType, null, null);

    public static ParameterManager Open(Type type, Type baseType, params Attribute[] include)
        => OpenInternal(type, baseType, include, null);

    public static ParameterManager Open(Type type, Type baseType, Attribute[] include, params Attribute[] exclude)
        => OpenInternal(type, baseType, include, exclude);

    private static ParameterManager OpenInternal(Type type, Type baseType, Attribute[] include, Attribute[] exclude)
    {
        var key = new CacheKey(type, baseType ?? typeof(object), ComputeFilterHash(include, exclude));

        if (!Cache.TryGetValue(key, out var cached))
            Cache[key] = cached = new(type, baseType ?? typeof(object), include, exclude);

        return cached;
    }

    private static int ComputeFilterHash(Attribute[] include, Attribute[] exclude)
    {
        var hash = new HashCode();

        if (include != null)
        {
            foreach (var a in include)
            {
                hash.Add(a.GetType());
            }

            hash.Add(-1); // separator
        }

        if (exclude != null)
        {
            foreach (var a in exclude)
            {
                hash.Add(a.GetType());
            }
        }

        return hash.ToHashCode();
    }

    public Span<string> GetNames() => NameToIdx.AsSpan();
    public Span<ParameterInfo> GetInfos() => Parameters.AsSpan();

    public bool TryGetParamIndex(string _name, out int _idx)
    {
        _idx = GetIndex(_name);
        return _idx >= 0;
    }

    public int GetIndex(string _name)
    {
        return NameToIdx.BinarySearch(PrepareString(_name));
    }

    public string GetName(int _idx) => Parameters[_idx].GetName();

    public ParameterInfo GetInfo(int _idx) => Parameters[_idx];

    public ParameterInfo GetInfo(string _name) => GetInfo(GetIndex(_name));

    public Type GetType(int _idx) => GetInfo(_idx).GetType();

    public Type GetType(string _name)
    {
        int i = NameToIdx.BinarySearch(PrepareString(_name));
        return i >= 0 ? GetType(i) : null;
    }

    public object GetValue(object _ref, int _idx) => GetInfo(_idx).GetValue(_ref);

    public object GetValue(object _ref, string _name)
    {
        int i = NameToIdx.BinarySearch(PrepareString(_name));
        return i >= 0 ? GetInfo(i).GetValue(_ref) : default;
    }

    public void SetValue(object _ref, int _idx, object _value)
    {
        GetInfo(_idx).SetValue(_ref, _value);
    }

    public void SetValue(object _ref, string _name, object _value)
    {
        int i = NameToIdx.BinarySearch(PrepareString(_name));
        if (i >= 0) SetValue(_ref, i, _value);
    }

    public static string PrepareString(string _str) => _str.Trim().ToLower();

    /// <summary>
    /// Returns every property index with a non equal value (a.Value != b.Value, performs a.Value.IsEqualTo(b.Value)). Returns empty if every value is equal, a or b is null or there's a type mismatch.
    /// </summary>
    public int[] GetNonEqual(object _a, object _b, params int[] blacklist)
    {
        if (_a.IsNull() || _b.IsNull() || _a.GetType() != _b.GetType() || _a == _b) return [];

        var list = new List<int>();

        for (var i = 0; i < Parameters.Length; i++)
        {
            if (GetValue(_a, i).IsEqualTo(GetValue(_b, i)) == false)
                list.Add(i);
        }

        // Handle blacklist
        if (blacklist.NotEmpty())
        {
            foreach (var idx in blacklist)
                list.Remove(idx);
        }

        return [.. list];
    }

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly Type Type;
        public readonly Type BaseType;
        public readonly int FilterHash;

        public CacheKey(Type type, Type baseType, int filterHash)
        {
            Type = type;
            BaseType = baseType;
            FilterHash = filterHash;
        }

        public bool Equals(CacheKey other) =>
            Type == other.Type &&
            BaseType == other.BaseType &&
            FilterHash == other.FilterHash;

        public override int GetHashCode() => HashCode.Combine(Type, BaseType, FilterHash);
    }
}

public readonly struct ParameterInfo
{
    private readonly PropertyInfo Property;
    private readonly FieldInfo Field;
    private readonly bool IsProperty;

    public ParameterInfo(PropertyInfo property)
    {
        Property = property;
        IsProperty = true;
        Field = null;
    }

    public ParameterInfo(FieldInfo field)
    {
        Property = null;
        IsProperty = false;
        Field = field;
    }

    public readonly string GetName() => IsProperty ? Property.Name : Field.Name;

    public readonly new Type GetType() => IsProperty ? Property.PropertyType : Field.FieldType;

    public readonly object GetValue(object _ref) => IsProperty ? Property.GetValue(_ref) : Field.GetValue(_ref);

    public readonly void SetValue(object _ref, object _value)
    {
        if (IsProperty) Property.SetValue(_ref, _value);
        else Field.SetValue(_ref, _value);
    }

    public readonly System.Reflection.ParameterInfo[] GetIndexParameters() => IsProperty ? Property.GetIndexParameters() : null;

    public readonly bool HasIndexParameters() => IsProperty && Property.GetIndexParameters().NotEmpty();
}