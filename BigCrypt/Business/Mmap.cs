using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace BigCrypt.Business;

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

    public void Dispose()
    {
        using (file)
        using (_accessor) { }
    }

    public static Mmap Open(string path, MemoryMappedFileAccess access = MemoryMappedFileAccess.Read)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_fileHandle")]
        static extern ref readonly SafeFileHandle? GetFileHandle(MemoryMappedFile mmap);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(GetFileLength))] // Serve il "nameof()" perchè i nomi dei metodi locali vengono sputtanati
        static extern long GetFileLength(SafeFileHandle handle);

        var file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, access);
        return new(file, GetFileLength(GetFileHandle(file)!), access);
    }

    public static Mmap Create(string path, long capacity)
    {
        const MemoryMappedFileAccess ACCESS = MemoryMappedFileAccess.ReadWrite; // Per qualche motivo, quando il file deve essere creato, serve anche "Read"
        var file = MemoryMappedFile.CreateFromFile(path, FileMode.Create, null, capacity, ACCESS);
        return new(file, capacity, ACCESS);
    } 

    #endregion
}