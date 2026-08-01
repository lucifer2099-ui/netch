using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Netch.Forms;
using Netch.Models;
using Netch.Models.Modes;
using WindowsJobAPI;

namespace Netch;

public static class Global
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    ///     主窗体的静态实例
    /// </summary>
    public static MainForm MainForm => ServiceProvider.GetRequiredService<MainForm>();

    /// <summary>
    ///     用于读取和写入的配置
    /// </summary>
    public static Setting Settings = new();

    public static readonly JobObject Job = new();

    /// <summary>
    ///     用于存储模式
    /// </summary>
    public static readonly List<Mode> Modes = new();

    public static readonly string NetchDir;
    public static readonly string NetchExecutable;

    static Global()
    {
        NetchExecutable = Application.ExecutablePath;
        NetchDir = Application.StartupPath;
    }

    public static JsonSerializerOptions NewCustomJsonSerializerOptions() => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}