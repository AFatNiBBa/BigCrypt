using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace BigCrypt.Business;

/// <summary>
/// Utility wrapper over <see cref="MemoryMappedFile"/> and <see cref="MemoryMappedViewAccessor"/> that provides a reference to the first byte of the mapped memory.
/// The reference produced by <see cref="First"/> is unmanaged
/// </summary>
/// <param name="file">The actual memory mapped file</param>
/// <param name="size">The size of the file</param>
/// <param name="access">The mode in which to open the file</param>
public class Mmap(MemoryMappedFile file, long size, MemoryMappedFileAccess access) : IDisposable
{
    private readonly MemoryMappedViewAccessor _accessor = file.CreateViewAccessor(0, 0, access);

    #region PROP

    public long Size { get; } = size;

    public unsafe ref byte First
    {
        get
        {
            byte* res = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref res);
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer(); // Non ci serve la protezione del GC qui, però DEVE rilasciare il puntatore
            return ref *res;
        }
    }

    #endregion

    #region METHOD

    /// <inheritdoc/>
    public void Dispose()
    {
        using (file)
        using (_accessor) { }
    }

    /// <summary>
    /// Creates a <see cref="Mmap"/> by opening a file
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <param name="access">The mode in which to open the file</param>
    public static Mmap Open(string path, MemoryMappedFileAccess access = MemoryMappedFileAccess.Read)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_fileHandle")]
        static extern ref readonly SafeFileHandle? GetFileHandle(MemoryMappedFile mmap);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(GetFileLength))] // The "nameof()" is needed because the actual names of local functions get mangled
        static extern long GetFileLength(SafeFileHandle handle);

        var file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, access);
        return new(file, GetFileLength(GetFileHandle(file)!), access);
    }

    /// <summary>
    /// Creates a <see cref="Mmap"/> by creating a file
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <param name="size">The size of the file</param>
    public static Mmap Create(string path, long size)
    {
        const MemoryMappedFileAccess ACCESS = MemoryMappedFileAccess.ReadWrite; // Per qualche motivo, quando il file deve essere creato, serve anche "Read"
        var file = MemoryMappedFile.CreateFromFile(path, FileMode.Create, null, size, ACCESS);
        return new(file, size, ACCESS);
    } 

    #endregion
}