namespace Versionize.Tests.TestSupport
{
    public static class TempDir
    {
        public static string Create()
        {
            var tempDir = Path.Join(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);

            return tempDir;
        }
    }
}
