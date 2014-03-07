﻿using System;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.TestData;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.MSBuild
{
    [TestFixture]
    public class ProjectAdapterTests
    {
        private const string _expectedBinaryDependencyAssemblyName = "Newtonsoft.Json";
        private const string _expectedBinaryDependencyVersion = "6.0.0.0";

        [Test]
        public void ProjectWithDependenciesAssemblyNameIsProjectWithDependencies()
        {
            var projectAdapter = CreateProjectAdapter(Paths.ProjectWithDependencies);
            Assert.That(projectAdapter.AssemblyName, Is.EqualTo("ProjectWithDependencies"));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson()
        {
            var projectAdapter = CreateProjectAdapter(Paths.ProjectWithDependencies);

            var binaryReferences = projectAdapter.GetBinaryReferences();

            var binaryReferenceIncludeNames = binaryReferences.Select(r => r.IncludeName).ToList();
            Assert.That(binaryReferenceIncludeNames, Contains.Item(_expectedBinaryDependencyAssemblyName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson6000()
        {
            var projectAdapter = CreateProjectAdapter(Paths.ProjectWithDependencies);

            var binaryReferences = projectAdapter.GetBinaryReferences();

            var newtonsoft = binaryReferences.Single(r => r.IncludeName == _expectedBinaryDependencyAssemblyName);
            Assert.That(newtonsoft.IncludeVersion, Is.EqualTo(_expectedBinaryDependencyVersion));
        }

        private static ProjectAdapter CreateProjectAdapter(string projectWithDependencies)
        {
            var msBuildProject = new Project(projectWithDependencies, null, null);
            var projectAdapter = new ProjectAdapter(msBuildProject, "packages.config");
            return projectAdapter;
        }
    }
}
