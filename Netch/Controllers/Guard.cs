using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers;

public abstract class Guard
{
    private FileStream? _logFileStream;
    private StreamWriter? _logStreamWriter;
    private string? _lastOutputLine;

    protected Guard(string mainFile, bool redirectOutput = true, Encoding? encoding = null)
    {
        RedirectOutput = redirectOutput;

        var fileName = Path.GetFullPath($"bin\\{mainFile}");
        if (!File.Exists(fileName))
            throw new MessageException(i18N.Translate($"bin\\{mainFile} file not found!"));

        Instance = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                WorkingDirectory = $"{Global.NetchDir}\\bin",
                CreateNoWindow = true,
                UseShellExecute = !RedirectOutput,
                RedirectStandardOutput = RedirectOutput,
                StandardOutputEncoding = RedirectOutput ? encoding : null,
                RedirectStandardError = RedirectOutput,
                StandardErrorEncoding = RedirectOutput ? encoding : null,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };
    }

    protected string LogPath => Path.Combine(Global.NetchDir, $"logging\\{Name}.log");

    protected virtual IEnumerable<string> StartedKeywords { get; } = new List<string>();

    protected virtual IEnumerable<string> FailedKeywords { get; } = new List<string>();

    public abstract string Name { get; }

    private State State { get; set; } = State.Waiting;

    private bool RedirectOutput { get; }

    public Process Instance { get; }

    protected async Task StartGuardAsync(string argument, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
    {
        State = State.Starting;

        _logFileStream = new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        _logStreamWriter = new StreamWriter(_logFileStream) { AutoFlush = true };

        Instance.StartInfo.Arguments = argument;
        Instance.Start();
        Global.Job.AddProcess(Instance);

        if (priority != ProcessPriorityClass.Normal)
            Instance.PriorityClass = priority;

        if (!RedirectOutput)
            return;

        ReadOutputAsync(Instance.StandardOutput).Forget();
        ReadOutputAsync(Instance.StandardError).Forget();

        if (!StartedKeywords.Any())
        {
            State = State.Started;
            return;
        }

        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(50);
            switch (State)
            {
                case State.Started:
                    OnStarted();
                    return;
                case State.Stopped:
                    var failedLine = _lastOutputLine;
                    await StopGuardAsync();
                    OnStartFailed();
                    throw new MessageException(BuildStartFailureMessage(failedLine));
            }
        }

        await StopGuardAsync();
        throw new MessageException($"{Name} controller start timed out.");
    }

    private async Task ReadOutputAsync(TextReader reader)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            _lastOutputLine = line;
            await _logStreamWriter!.WriteLineAsync(line);
            OnReadNewLine(line);

            if (State != State.Starting)
                continue;

            if (StartedKeywords.Any(s => line.Contains(s, StringComparison.OrdinalIgnoreCase)))
                State = State.Started;
            else if (FailedKeywords.Any(s => line.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                Log.Error("{Name} start failed: {Line}", Name, line);
                State = State.Stopped;
            }
        }
    }

    public virtual Task StopAsync()
    {
        return StopGuardAsync();
    }

    protected async Task StopGuardAsync()
    {
        try
        {
            if (Instance is { HasExited: false })
            {
                Instance.Kill();
                await Instance.WaitForExitAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Stop {Name} failed", Instance.ProcessName);
        }
        finally
        {
            if (_logStreamWriter != null)
                await _logStreamWriter.DisposeAsync();

            if (_logFileStream != null)
                await _logFileStream.DisposeAsync();

            Instance.Dispose();
            State = State.Stopped;
        }
    }

    protected virtual void OnStarted()
    {
    }

    protected virtual void OnReadNewLine(string line)
    {
    }

    protected virtual void OnStartFailed()
    {
        Utils.Utils.Open(LogPath);
    }

    private string BuildStartFailureMessage(string? failedLine)
    {
        return string.IsNullOrWhiteSpace(failedLine)
            ? $"{Name} controller start failed. See {LogPath}."
            : $"{Name} controller start failed.\n{failedLine}";
    }
}
