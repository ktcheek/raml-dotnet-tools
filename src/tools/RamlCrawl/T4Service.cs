using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TextTemplating;

namespace RamlCrawl
{
    public class T4Service
    {
        private readonly Engine _engine = new Engine();
        private readonly TextTemplatingEngineHost _host = new TextTemplatingEngineHost();
        private static readonly Dictionary<string, string> FileContents = new Dictionary<string, string>();

        private readonly List<(string Variable, string Replacement)> _variableReplacements = new List<(string, string)>();

        public T4Service()
        {
            _host.Session = new TextTemplatingSession();
        }

        public string Process(string templatePath, params (string, object)[] arguments)
        {
            try
            {
                _host.TemplateFile = templatePath;

                foreach (var (key, value) in arguments)
                {
                    _host.Session[key] = value;
                }

                var templateContent = LoadFileContent(templatePath);

                foreach (var (variable, replacement) in _variableReplacements)
                {
                    templateContent = templateContent.Replace($"$({variable})", replacement);
                }

                var output = _engine.ProcessTemplate(templateContent, _host);

                if (!_host.Errors.HasErrors) return output;

                foreach (CompilerError hostError in _host.Errors)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"[Error {hostError.ErrorNumber}] col:{hostError.Column} line:{hostError.Line}");
                    Console.WriteLine(hostError.ErrorText);
                }

                return "";
            }
            finally{
            {
                _variableReplacements.Clear();
            }}
        }

        private static string LoadFileContent(string templatePath)
        {
            if (FileContents.TryGetValue(templatePath, out var fileContent)) return fileContent;
            
            fileContent = File.ReadAllText(templatePath);
            FileContents[templatePath] = fileContent;
            return fileContent;
        }

        public T4Service SetVariableReplacement(string variable, string replacement)
        {
            _variableReplacements.Add((variable, replacement));
            return this;
        }
    }
}