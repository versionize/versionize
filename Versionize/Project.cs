using System.Xml;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize
{
    public class Project
    {
        public string ProjectFile { get; }
        public Version Version { get; }

        private Project(string projectFile, Version version)
        {
            ProjectFile = projectFile;
            Version = version;
        }

        public static Project Create(string projectFile)
        {
            var version = ReadVersion(projectFile);

            return new Project(projectFile, version);
        }

        public static bool IsVersionable(string projectFile)
        {
            try
            {
                ReadVersion(projectFile);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Version ReadVersion(string projectFile)
        {
            XmlDocument doc = new XmlDocument {PreserveWhitespace = true};

            try
            {
                doc.Load(projectFile);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {projectFile} is not a valid csproj file. Please make sure that you have a valid csproj file in place!");
            }

            var versionString = doc.SelectSingleNode("/Project/PropertyGroup/Version")?.InnerText;

            if (string.IsNullOrWhiteSpace(versionString))
            {
                throw new InvalidOperationException($"Project {projectFile} contains no or an empty <Version> XML Element. Please add one if you want to version this project - for example use <Version>1.0.0</Version>");
            }

            try
            {
                return Version.Parse(versionString);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version - for example use <Version>1.0.0</Version>");
            }
        }

        public void WriteVersion(Version nextVersion)
        {
            var doc = new XmlDocument {PreserveWhitespace = true};

            try
            {
                doc.Load(ProjectFile);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {ProjectFile} is not a valid csproj file. Please make sure that you have a valid csproj file in place!");
            }

            var versionElement = doc.SelectSingleNode("/Project/PropertyGroup/Version");
            versionElement.InnerText = nextVersion.ToString();

            doc.Save(ProjectFile);
        }
    }
}
