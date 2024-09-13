using System;

static class Utils
{
    /// <summary>Returns a new array with the <paramref name="value"/> appended to the end.</summary>
    public static T[] Append<T>(this T[] array, T value)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        return Insert(array, array.Length, value);
    }

    /// <summary>
    ///     Returns a new array in which a single element has been replaced.</summary>
    /// <param name="array">
    ///     The array from which to create a new array with one element replaced.</param>
    /// <param name="index">
    ///     The index at which to replace one element.</param>
    /// <param name="element">
    ///     The new element to replace the old element with.</param>
    public static T[] Replace<T>(this T[] array, int index, T element)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (index < 0 || index > array.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "index must be between 0 and the size of the input array.");
        var result = (T[])array.Clone();
        result[index] = element;
        return result;
    }

    /// <summary>
    ///     Similar to <see cref="string.Remove(int,int)"/>, but for arrays. Returns a new array containing everything except
    ///     the <paramref name="length"/> items starting from the specified <paramref name="startIndex"/>.</summary>
    /// <remarks>
    ///     Returns a new copy of the array even if <paramref name="length"/> is 0.</remarks>
    public static T[] Remove<T>(this T[] array, int startIndex, int length)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be negative.");
        if (length < 0 || startIndex + length > array.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative or extend beyond the end of the array.");
        T[] result = new T[array.Length - length];
        Array.Copy(array, 0, result, 0, startIndex);
        Array.Copy(array, startIndex + length, result, startIndex, array.Length - length - startIndex);
        return result;
    }

    /// <summary>
    ///     Similar to <see cref="string.Insert(int, string)"/>, but for arrays. Returns a new array with the <paramref
    ///     name="values"/> inserted starting from the specified <paramref name="startIndex"/>.</summary>
    /// <remarks>
    ///     Returns a new copy of the array even if <paramref name="values"/> is empty.</remarks>
    public static T[] Insert<T>(this T[] array, int startIndex, params T[] values)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (startIndex < 0 || startIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be between 0 and the size of the input array.");
        T[] result = new T[array.Length + values.Length];
        Array.Copy(array, 0, result, 0, startIndex);
        Array.Copy(values, 0, result, startIndex, values.Length);
        Array.Copy(array, startIndex, result, startIndex + values.Length, array.Length - startIndex);
        return result;
    }
}