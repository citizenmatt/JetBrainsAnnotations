﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;

public class WeaverHelper
{
    string projectPath;
    string assemblyPath;
    public Assembly Assembly { get; set; }

    public WeaverHelper(string projectPath):this(projectPath,null, null)
    {
    }

    public WeaverHelper(string projectPath, string includeNamespaces, string excludeNamespaces)
    {
        this.projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\", projectPath));

        GetAssemblyPath();

        var newAssembly = assemblyPath.Replace(".dll", "2.dll");
        File.Copy(assemblyPath, newAssembly, true);

        var assemblyResolver = new MockAssemblyResolver
        {
            Directory = Path.GetDirectoryName(assemblyPath)
        };
        var moduleDefinition = ModuleDefinition.ReadModule(newAssembly);
        var weavingTask = new ModuleWeaver
        {
            IncludeNamespaces = includeNamespaces,
            ExcludeNamespaces = excludeNamespaces,
            ModuleDefinition = moduleDefinition,
            AssemblyResolver = assemblyResolver,
        };

        weavingTask.Execute();
        moduleDefinition.Write(newAssembly);

        Assembly = Assembly.LoadFile(newAssembly);
    }

    void GetAssemblyPath()
    {
        assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), GetOutputPathValue(), GetAssemblyName() + ".dll");
    }

    string GetAssemblyName()
    {
        var xDocument = XDocument.Load(projectPath);
        xDocument.StripNamespace();

        return xDocument.Descendants("AssemblyName")
            .Select(x => x.Value)
            .First();
    }

    string GetOutputPathValue()
    {
        var xDocument = XDocument.Load(projectPath);
        xDocument.StripNamespace();

        var outputPathValue = (from propertyGroup in xDocument.Descendants("PropertyGroup")
                               let condition = ((string)propertyGroup.Attribute("Condition"))
                               where (condition != null) &&
                                     (condition.Trim() == "'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'")
                               from outputPath in propertyGroup.Descendants("OutputPath")
                               select outputPath.Value).First();
#if (!DEBUG)
            outputPathValue = outputPathValue.Replace("Debug", "Release");
#endif
        return outputPathValue;
    }


}