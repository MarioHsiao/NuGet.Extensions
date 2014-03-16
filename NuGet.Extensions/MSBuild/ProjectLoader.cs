using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;

namespace NuGet.Extensions.MSBuild
{
    public class ProjectLoader : IProjectLoader
    {
        private readonly IConsole _console;
        private readonly ProjectCollection _projectCollection;
        private readonly Dictionary<Guid, IVsProject> _projectsByGuid;
        private readonly IDictionary<string, string> _globalMsBuildProperties;
        private readonly IProjectLoader _projectLoader;

        public ProjectLoader(IDictionary<string, string> globalMsBuildProperties, IConsole console)
        {
            _globalMsBuildProperties = globalMsBuildProperties;
            _console = console;
            _projectCollection = new ProjectCollection();
            _projectsByGuid = new Dictionary<Guid, IVsProject>();
            _projectLoader = this;
        }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        public IVsProject GetProject(Guid projectGuid, string absoluteProjectPath)
        {
            IVsProject projectAdapter;
            if (_projectsByGuid.TryGetValue(projectGuid, out projectAdapter)) return projectAdapter;

            projectAdapter = GetProjectAdapterFromPath(absoluteProjectPath);
            _console.WriteLine("Potential authoring issue: Project {0} should have been referenced in the solution with guid {1}", Path.GetFileName(absoluteProjectPath), projectGuid);
            _projectsByGuid.Add(projectGuid, projectAdapter); //TODO This could cause an incorrect mapping, get the guid from the loaded project
            return projectAdapter;
        }

        private IVsProject GetProjectAdapterFromPath(string absoluteProjectPath)
        {
            try
            {
                var msBuildProject = GetMsBuildProject(absoluteProjectPath, _projectCollection, _globalMsBuildProperties);
                return GetRealProjectAdapter(_projectLoader, msBuildProject, _projectsByGuid);
            }
            catch (Exception e)
            {
                var nullProjectAdapter = new NullProjectAdapter(absoluteProjectPath);
                _console.WriteWarning("Problem loading {0}, any future messages about modifications to it are speculative only:");
                _console.WriteWarning("  {0}", e.Message);
                return nullProjectAdapter;
            }
        }

        private static IVsProject GetRealProjectAdapter(IProjectLoader projectLoader, Project msBuildProject, IDictionary<Guid, IVsProject> projectsByGuidCache)
        {
            var projectGuid = Guid.Parse(GetProjectGuid(msBuildProject));
            IVsProject projectAdapter;
            return projectsByGuidCache.TryGetValue(projectGuid, out projectAdapter) ? projectAdapter : new ProjectAdapter(msBuildProject, projectLoader);
        }

        private static string GetProjectGuid(Project msBuildProject)
        {
            return msBuildProject.GetPropertyValue("ProjectGuid");
        }

        private static Project GetMsBuildProject(string projectPath, ProjectCollection projectCollection, IDictionary<string, string> globalMsBuildProperties)
        {
            var canonicalProjectPath = Path.GetFullPath(projectPath).ToLowerInvariant();
            var existing = projectCollection.GetLoadedProjects(canonicalProjectPath).SingleOrDefault();
            return existing ?? new Project(canonicalProjectPath, globalMsBuildProperties, null, projectCollection);
        }
    }
}