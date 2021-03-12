using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;

namespace scriptwich
{
    class Program
    {
        private static readonly TextSpanFormatter formatter = new();

        static async Task Main(string[] args)
        {
            var root = new RootCommand
            {
                new Argument<FileInfo>("script")
                    .ExistingOnly()
            };

            root.Handler = CommandHandler.Create<FileInfo, IConsole>(RunScript);

            await root.InvokeAsync(args);
        }

        private static async Task RunScript(
            FileInfo script,
            IConsole console)
        {
            using var kernel = CreateKernel();

            var output = Console.Out;

            using var _ = kernel.KernelEvents
                                .Subscribe(e =>
                                {
                                    var (writer, message) = GetOutput(console, e);

                                    if (message != default)
                                    {
                                        var span = formatter.ParseToSpan(message);
                                        var text = span.ToString(OutputMode.Ansi);
                                        writer.WriteLine(text);
                                    }
                                });

            Formatter.DefaultMimeType = PlainTextFormatter.MimeType;

            var scriptCode = await File.ReadAllTextAsync(script.FullName);

            var result = await kernel.SubmitCodeAsync(scriptCode);
        }

        private static (IStandardStreamWriter, FormattableString) GetOutput(IConsole console, KernelEvent e)
        {
            return e switch
            {
                CommandFailed failed =>
                    (
                        console.Error,
                        failed.Message.Red()
                    ),
                ErrorProduced ep =>
                    (
                        console.Error,
                        ep.Message.Red()
                    ),
                StandardErrorValueProduced er =>
                    (
                        console.Error,
                        er.FormattedValues.FirstOrDefault(v => v.MimeType == "text/plain")?.Value.Default()
                    ),
                IncompleteCodeSubmissionReceived _ =>
                    (
                        console.Error,
                        default
                    ),
                DisplayEvent d =>
                    (
                        console.Out,
                        d.FormattedValues.FirstOrDefault(v => v.MimeType == "text/plain")?.Value.Default()
                    ),
                DiagnosticLogEntryProduced diag =>
                    (
                        console.Out,
                        diag.Message.Gray()
                    ),
                _ => default
            };
        }

        private static Kernel CreateKernel()
        {
            var compositeKernel = new CompositeKernel();

            compositeKernel.Add(
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho());

            compositeKernel.Add(
                new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDefaultNamespaces());

            compositeKernel.Add(
                new PowerShellKernel(),
                new[] { "powershell" });

            compositeKernel.Add(
                new JavaScriptKernel(),
                new[] { "js" });

            compositeKernel.Add(
                new HtmlKernel());

            return compositeKernel;
        }
    }
}