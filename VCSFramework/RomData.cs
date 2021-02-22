using System;

namespace VCSFramework
{
    // @TODO - RomDataSingle? For one element.
    // @TODO - 16-bit length/index type.
    /// <summary>
    /// A compile-time only construct that provides read-only access to ROM data.
    /// This structure occupies no memory at runtime. This has the major advantages of being optimizable and free
    /// to use, but the disadvantage that it can't be passed through methods or stored in local variables.
    /// </summary>
    /// <typeparam name="T">The type of the elements stored in ROM.</typeparam>
    public readonly struct RomData<T> where T : unmanaged
    {
        /// <summary>
        /// Gets the total number of elements.
        /// This can be optimized to a compile-time constant. So when possible, this property should be
        /// used directly, instead of assigning it to a variable.
        /// </summary>
        public byte Length
        {
            [ReplaceWithEntry(typeof(RomDataLengthCall))]
            get => throw new NotImplementedException();
        }

        public unsafe T* Pointer
        {
            [ReplaceWithEntry(typeof(RomDataGetPointerCall))]
            get => throw new NotImplementedException();
        }

        public byte Stride
        {
            [ReplaceWithEntry(typeof(RomDataStrideCall))]
            get => throw new NotImplementedException();
        }

        public ref readonly T this[byte index]
        {
            [ReplaceWithEntry(typeof(RomDataGetterCall))]
            get => throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A version of <see cref="RomData{T}"/> that occupies memory at runtime. This has the advantage
    /// of being able to be passed through methods or stored in local variables, but the disadvantage of
    /// consuming 3 bytes of RAM at runtime.
    /// Generally <see cref="RomData{T}"/> should be used instead of this type when possible.
    /// </summary>
    /// <typeparam name="T">The type of the elements stored in ROM.</typeparam>
    internal unsafe ref struct RuntimeRomData<T> where T : unmanaged
    {
        internal T* Pointer { get; }
        public byte Length { get; }

        internal RuntimeRomData(T* pointer, byte length)
        {
            Pointer = pointer;
            Length = length;
        }

        public ref readonly T this[byte index]
        {
            get => ref Pointer[index];
        }
    }
}
