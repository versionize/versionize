using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Versionize
{
    public class Projects
    {
        private IEnumerable<Project> _projects;

        private Projects(IEnumerable<Project> projects)
        {
            _projects = projects;
        }

        public Version Version { get => _projects.First().Version; }

        public static Projects Discover(string workingDirectory)
        {
            var projects = Directory
                .GetFiles(workingDirectory, "*.csproj", SearchOption.AllDirectories)
                .Where(Project.IsVersionable)
                .Select(Project.Create)
                .ToList();

            if (!projects.Any())
            {
                throw new InvalidOperationException($"No project found {workingDirectory} that contains a valid version element - for example use <Version>1.0.0</Version>");
            }

            var firstProjectVersion = projects.First().Version;
            var projectsWithDifferingVersion = projects.Where(p => !p.Version.Equals(firstProjectVersion));

            if (projectsWithDifferingVersion.Any())
            {
                throw new InvalidOperationException($"Expected all projects to have version {firstProjectVersion} but found a differing version in {String.Join(", ", projectsWithDifferingVersion)}");
            }

            return new Projects(projects);
        }

        public void WriteVersion(Version nextVersion)
        {
            foreach (var project in _projects)
            {
                project.WriteVersion(nextVersion);
            }
        }

        public IEnumerable<string> GetProjectFiles()
        {
            return _projects.Select(project => project.ProjectFile);
        }
    }
}