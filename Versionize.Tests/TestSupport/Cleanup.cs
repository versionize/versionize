using System.IO;

namespace Versionize.Tests.TestSupport
{
    public static class Cleanup
    {
        public static void DeleteDirectory(string tempDir)
        {
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var attribs = File.GetAttributes(file);
                if (attribs.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(file, attribs & ~FileAttributes.ReadOnly);
                }
            }

            Directory.Delete(tempDir, true);
        } 
    }
}
