﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AMF.Tools.Core.WebApiGenerator;

namespace RamlCrawl
{
    internal class Program
    {
        private static readonly T4Service T4Service = new T4Service();

        private static void Main(string[] args)
        {
            DoMeasuredTask(
                "Process All RAML file(s)",
                () =>
                {
                    foreach (var ramlContract in RamlContract.LoadAll())
                    {
                        if (args.Contains("--download-from-source"))
                        {
                            DoMeasuredTask(
                                $"Downloading {ramlContract.Name}",
                                ramlContract.DownloadRamlFileFromSource);
                        }

                        DoMeasuredTask(
                            $"Processing {ramlContract.Name}",
                            () =>
                            {
                                var model = DoMeasuredTask(
                                    $"Parsing {Path.GetFileName(ramlContract.RamlFile)}",
                                    ramlContract.ToWebApiGeneratorModel);

                                ramlContract.ClearAutogeneratedFolder();

                                foreach (var (path, content) in
                                    new Func<
                                            RamlContract, 
                                            WebApiGeneratorModel,
                                            IEnumerable<(string OutputPath, string Content)>>[]
                                        {
                                            ProcessControllerInterface,
                                            ProcessControllerBase,
                                            ProcessModels,
                                            ProcessEnums,
                                            ProcessControllerImplementation
                                        }
                                        .Select(x => x.Invoke(ramlContract, model))
                                        .SelectMany(x => x))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");

                                    DoMeasuredTask(
                                        $"[{ramlContract.Name}] Writing {Path.GetFileName(path)}",
                                        () => File.WriteAllText(path, content));
                                }
                            });
                    }
                });
        }

        private static IEnumerable<(string OutputPath, string Content)> ProcessModels(
            RamlContract ramlContract,
            WebApiGeneratorModel model) =>
            model.Objects
                .Select(modelObject => DoMeasuredTask(
                $"[{ramlContract.Name}] Processing Model {modelObject.Name}",
                () =>
                (
                    Path.Combine(ramlContract.ApiModelDestinationPath, $"{modelObject.Name}.cs"),
                    "// Template: Models (ApiModel.t4) version 5.0\r\n" +
                    T4Service.Process(
                        ramlContract.ApiModelTemplatePath,
                        ("apiObject", modelObject),
                        ("modelsNamespace", ramlContract.ModelsNamespace))
                )));

        private static IEnumerable<(string OutputPath, string Content)> ProcessEnums(
            RamlContract ramlContract,
            WebApiGeneratorModel model) =>
            model.Enums
                .Select(apiEnum => DoMeasuredTask(
                $"[{ramlContract.Name}] Processing Enum {apiEnum.Name}",
                () =>
                (
                    Path.Combine(
                        ramlContract.ApiControllerEnumDestinationPath,
                        $"{apiEnum.Name}.cs"),
                    "// Template: Models (ApiEnum.t4) version 5.0\r\n" +
                    T4Service
                        .Process(
                            ramlContract.ApiEnumTemplatePath,
                            ("apiEnum", apiEnum),
                            ("modelsNamespace", ramlContract.ModelsNamespace))
                )));

        private static IEnumerable<(string OutputPath, string Content)> ProcessControllerInterface(
            RamlContract ramlContract,
            WebApiGeneratorModel model) =>
            model.Controllers
                .Select(controllerObject => DoMeasuredTask(
                $"[{ramlContract.Name}] Processing ControllerInterface {controllerObject.Name}",
                () =>
                (
                    Path.Combine(
                        ramlContract.ApiControllerInterfaceDestinationPath,
                        $"I{controllerObject.Name}Controller.cs"),
                    "// Template: Controller Interface (ApiControllerInterface.t4) version 5.0\r\n" +
                    T4Service
                        .SetVariableReplacement("namespace", ramlContract.ControllersNamespace)
                        .Process(
                            ramlContract.ApiControllerInterfaceTemplatePath,
                            ("controllerObject", controllerObject),
                            ("hasModels", true),
                            ("useAsyncMethods", true),
                            ("apiVersion", ""),
                            ("modelsNamespace", ramlContract.ModelsNamespace))
                )));

        private static IEnumerable<(string OutputPath, string Content)> ProcessControllerBase(
            RamlContract ramlContract,
            WebApiGeneratorModel model) =>
            model.Controllers
                .Select(controllerObject => DoMeasuredTask(
                $"[{ramlContract.Name}] Processing ControllerBase {controllerObject.Name}",
                () =>
                (
                    Path.Combine(
                        ramlContract.ApiControllerBaseDestinationPath,
                        $"{controllerObject.Name}Controller.cs"),
                    "// Template: Base Controller (ApiControllerBase.t4) version 5.0\r\n" +
                    T4Service
                        .SetVariableReplacement("namespace", ramlContract.ControllersNamespace)
                        .Process(
                            ramlContract.ApiControllerBaseTemplatePath,
                            ("controllerObject", controllerObject),
                            ("hasModels", true),
                            ("useAsyncMethods", true),
                            ("apiVersion", ""),
                            ("modelsNamespace", ramlContract.ModelsNamespace))
                )));

        private static IEnumerable<(string OutputPath, string Content)> ProcessControllerImplementation(
            RamlContract ramlContract,
            WebApiGeneratorModel model) =>
            model.Controllers
                .Select(c => new
                {
                    Path = Path.Combine(ramlContract.ApiControllerImplementationDestinationPath, $"{c.Name}Controller.cs"),
                    Controller = c
                })
                .Where(x => !File.Exists(x.Path))
                .Select(x => DoMeasuredTask(
                    $"[{ramlContract.Name}] Processing ControllerImplementation {x.Controller.Name}",
                    () =>
                    (
                        x.Path,
                        "// Template: Controller Implementation (ApiControllerImplementation.t4) version 5.0\r\n" +
                        T4Service
                            .SetVariableReplacement("namespace", ramlContract.ControllersNamespace)
                            .Process(
                                ramlContract.ApiControllerImplementationTemplatePath,
                                ("controllerObject", x.Controller),
                                ("hasModels", true),
                                ("useAsyncMethods", true),
                                ("apiVersion", ""),
                                ("modelsNamespace", ramlContract.ModelsNamespace))
                    )));

        private static void DoMeasuredTask(string taskName, Func<Task> task)
        {
            using (new MeasuredTask(taskName))
            {
                task().Wait();
            }
        }

        private static T DoMeasuredTask<T>(string taskName, Func<T> task)
        {
            using (new MeasuredTask(taskName))
            {
                return task();
            }
        }

        private static void DoMeasuredTask(string taskName, Action task)
        {
            using (new MeasuredTask(taskName))
            {
                task();
            }
        }
    }

    public class MeasuredTask : IDisposable
    {
        private readonly string _taskName;
        private static MeasuredTask _lastMeasuredTask;
        private readonly Stopwatch _stopWatch;

        public MeasuredTask(string taskName)
        {
            _taskName = taskName;
            if (_lastMeasuredTask != null)
            {
                Console.WriteLine();
            }

            _lastMeasuredTask = this;
            _stopWatch = Stopwatch.StartNew();

            Console.Write(taskName);
        }

        public void Dispose()
        {
            Console.WriteLine(_lastMeasuredTask == this
                ? $" took {_stopWatch.ElapsedMilliseconds}ms"
                : $"{_taskName} took {_stopWatch.ElapsedMilliseconds}ms");

            _lastMeasuredTask = null;
        }
    }
}