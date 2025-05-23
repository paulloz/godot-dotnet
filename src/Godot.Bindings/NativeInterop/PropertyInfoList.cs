using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot.Bridge;
using Godot.NativeInterop.Marshallers;

namespace Godot.NativeInterop;

internal sealed class PropertyInfoList : IList<PropertyInfo>
{
    private readonly List<PropertyInfo> _values = [];

    public PropertyInfo this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    public int Count => _values.Count;

    bool ICollection<PropertyInfo>.IsReadOnly => false;

    public void Add(PropertyInfo item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _values.Add(item);
    }

    public void AddRange(IEnumerable<PropertyInfo> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _values.AddRange(items);
    }

    public void AddRange(ReadOnlySpan<PropertyInfo> items)
    {
        _values.AddRange(items);
    }

    public bool Contains(PropertyInfo item)
    {
        return _values.Contains(item);
    }

    public void Clear()
    {
        _values.Clear();
    }

    public int IndexOf(PropertyInfo item)
    {
        return _values.IndexOf(item);
    }

    public void Insert(int index, PropertyInfo item)
    {
        _values.Insert(index, item);
    }

    public bool Remove(PropertyInfo item)
    {
        int index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        _values.RemoveAt(index);
    }

    void ICollection<PropertyInfo>.CopyTo(PropertyInfo[] array, int arrayIndex)
    {
        _values.CopyTo(array, arrayIndex);
    }

    public IEnumerator<PropertyInfo> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static unsafe GDExtensionPropertyInfo* ConvertToNative(PropertyInfoList value)
    {
        int count = value.Count;
        GDExtensionPropertyInfo* ptr = (GDExtensionPropertyInfo*)NativeMemory.Alloc((nuint)count, (nuint)sizeof(GDExtensionPropertyInfo));
        for (int i = 0; i < count; i++)
        {
            PropertyInfo propertyInfo = value[i];
            ref GDExtensionPropertyInfo refProperty = ref ptr[i];

            // Update the property info with the data from the managed type.
            refProperty.type = (GDExtensionVariantType)propertyInfo.Type;
            refProperty.name = StringNameMarshaller.ConvertToUnmanaged(propertyInfo.Name);
            refProperty.hint = (uint)propertyInfo.Hint;
            refProperty.hint_string = StringMarshaller.ConvertToUnmanaged(propertyInfo.HintString);
            refProperty.class_name = StringNameMarshaller.ConvertToUnmanaged(propertyInfo.ClassName);
            refProperty.usage = (uint)propertyInfo.Usage;
        }
        return ptr;
    }

    internal static unsafe void FreeNative(GDExtensionPropertyInfo* ptr, int count)
    {
        if (ptr is null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            ref GDExtensionPropertyInfo refProperty = ref ptr[i];

            StringNameMarshaller.Free(refProperty.name);
            StringMarshaller.Free(refProperty.hint_string);
            StringNameMarshaller.Free(refProperty.class_name);
        }

        NativeMemory.Free(ptr);
    }
}
