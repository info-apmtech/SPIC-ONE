using System.IO;

namespace SpicAPI.Controllers.Helpers
{
    public static class StreamUtils
    {
        public static byte[] ReadStreamFully(Stream input)
        {
            if (input is MemoryStream ms && ms.TryGetBuffer(out var seg))
                return seg.Array ?? ms.ToArray();

            using var memory = new MemoryStream();
            input.Position = 0;
            input.CopyTo(memory);
            return memory.ToArray();
        }
    }
}