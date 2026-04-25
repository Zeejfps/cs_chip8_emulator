using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Chip8Emulator.Core;
using Silk.NET.OpenGL;

namespace Chip8Emulator.Desktop;

internal sealed class EmulatorView : OpenGlControlBase, IRenderer
{
    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _texture;
    private int _texWidth;
    private int _texHeight;
    private int _uPaletteLoc;
    private int _uDisplayLoc;

    private static readonly float[] DefaultPalette =
    [
        // [0] background
        0.06f, 0.06f, 0.08f, 1.0f,
        // [1] plane 0
        0.95f, 0.95f, 0.95f, 1.0f,
        // [2] plane 1
        0.95f, 0.35f, 0.20f, 1.0f,
        // [3] both planes
        0.95f, 0.85f, 0.20f, 1.0f,
    ];

    private readonly DesktopInput _input = new();
    private readonly DesktopAudio _audio = new();
    private readonly DesktopFlagStore _flagStore = new();
    private readonly ManualClock _clock = new();
    private IInterpreter? _interpreter;
    private byte[]? _currentRom;
    private long _lastTimestamp;
    private bool _paused;
    private Settings _settings = new();

    public DesktopInput Input => _input;
    public bool IsPaused => _paused;
    public bool HasRom => _currentRom != null;

    public Settings Settings
    {
        get => _settings;
        set => _settings = value;
    }

    public void LoadRom(byte[] rom)
    {
        _currentRom = rom;
        BuildInterpreter();
    }

    public void Reset()
    {
        if (_currentRom == null) return;
        BuildInterpreter();
    }

    public void TogglePause() => _paused = !_paused;

    public void Pause() => _paused = true;

    public void Resume()
    {
        _paused = false;
        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    public void ApplySettings(Settings settings)
    {
        _settings = settings;
        if (_interpreter == null) return;
        _interpreter.InstructionsPerSecond = settings.InstructionsPerSecond;
        _interpreter.ShiftUsesVy = settings.ShiftUsesVy;
        _interpreter.JumpUsesVx = settings.JumpUsesVx;
        _interpreter.LoadStoreIncrementsI = settings.LoadStoreIncrementsI;
        _interpreter.LogicResetsVf = settings.LogicResetsVf;
        _interpreter.SpritesWrap = settings.SpritesWrap;
        _interpreter.DisplayWait = settings.DisplayWait;
        _interpreter.VfResultWrittenLast = settings.VfResultWrittenLast;
    }

    private void BuildInterpreter()
    {
        if (_currentRom == null) return;

        _interpreter?.Stop();
        _audio.Reset();
        _input.Reset();

        var interp = Chip8.Builder()
            .WithInput(_input)
            .WithAudio(_audio)
            .WithClock(_clock)
            .WithFlagStore(_flagStore)
            .WithRenderer(this)
            .Build();

        interp.InstructionsPerSecond = _settings.InstructionsPerSecond;
        interp.ShiftUsesVy = _settings.ShiftUsesVy;
        interp.JumpUsesVx = _settings.JumpUsesVx;
        interp.LoadStoreIncrementsI = _settings.LoadStoreIncrementsI;
        interp.LogicResetsVf = _settings.LogicResetsVf;
        interp.SpritesWrap = _settings.SpritesWrap;
        interp.DisplayWait = _settings.DisplayWait;
        interp.VfResultWrittenLast = _settings.VfResultWrittenLast;

        interp.LoadProgram(_currentRom);
        interp.Start();

        _interpreter = interp;
        _lastTimestamp = Stopwatch.GetTimestamp();
        _paused = false;
    }

    void IRenderer.Render(IReadOnlyDisplay display)
    {
        // No-op: GL upload happens on the render thread inside OnOpenGlRender.
    }

    // Drive the emulator on a wall-clock timer rather than on Avalonia's render loop.
    // OpenGlControlBase has known issues where its compositor-driven loop stalls on macOS
    // (Avalonia #17865) until a layout invalidation (e.g. resize) restarts it. A
    // standalone timer also matches how emulators are conventionally driven: tick at a
    // fixed cadence, ask the view to repaint, draw the latest state. Pause stops the
    // timer; resume restarts it.
    private DispatcherTimer? _tickTimer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _tickTimer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Render,
            (_, _) => RequestNextFrameRendering());
        _tickTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _tickTimer?.Stop();
        _tickTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        _gl = GL.GetApi(gl.GetProcAddress);

        var vertSrc = LoadEmbeddedText("Chip8Emulator.Desktop.Shaders.Display.vert");
        var fragSrc = LoadEmbeddedText("Chip8Emulator.Desktop.Shaders.Display.frag");

        var vs = CompileShader(_gl, ShaderType.VertexShader, vertSrc);
        var fs = CompileShader(_gl, ShaderType.FragmentShader, fragSrc);
        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vs);
        _gl.AttachShader(_program, fs);
        _gl.LinkProgram(_program);
        _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out var ok);
        if (ok == 0)
        {
            var log = _gl.GetProgramInfoLog(_program);
            throw new InvalidOperationException("Program link failed: " + log);
        }
        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
        _uPaletteLoc = _gl.GetUniformLocation(_program, "uPalette[0]");
        if (_uPaletteLoc < 0) _uPaletteLoc = _gl.GetUniformLocation(_program, "uPalette");
        _uDisplayLoc = _gl.GetUniformLocation(_program, "uDisplay");

        _vao = _gl.GenVertexArray();

        _texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _texWidth = 0;
        _texHeight = 0;

        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        _lastTimestamp = Stopwatch.GetTimestamp();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_gl == null) return;
        if (_program != 0) _gl.DeleteProgram(_program);
        if (_vao != 0) _gl.DeleteVertexArray(_vao);
        if (_texture != 0) _gl.DeleteTexture(_texture);
        _gl = null;
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_gl == null) return;

        // Tick the interpreter by wall-clock delta.
        if (_interpreter != null && !_paused)
        {
            var now = Stopwatch.GetTimestamp();
            var delta = now - _lastTimestamp;
            _lastTimestamp = now;
            if (delta > 0)
            {
                _clock.Advance(delta);
            }
        }
        else
        {
            _lastTimestamp = Stopwatch.GetTimestamp();
        }

        // Get framebuffer pixel size.
        var pixelSize = GetPixelSize();
        var fbW = pixelSize.width;
        var fbH = pixelSize.height;

        _gl.Viewport(0, 0, (uint)fbW, (uint)fbH);
        _gl.ClearColor(DefaultPalette[0], DefaultPalette[1], DefaultPalette[2], 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        if (_interpreter == null) return;

        var display = _interpreter.Display;
        var dispW = display.Width;
        var dispH = display.Height;
        if (dispW <= 0 || dispH <= 0) return;

        // Upload pixel data.
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        var span = display.VMem.Span;

        // The buffer is row-major with stride = display.Width and only Width*Height bytes
        // are meaningful. Upload exactly that range.
        var meaningful = dispW * dispH;
        if (meaningful > span.Length) meaningful = span.Length;

        fixed (byte* p = span)
        {
            if (dispW != _texWidth || dispH != _texHeight)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.R8ui,
                    (uint)dispW,
                    (uint)dispH,
                    0,
                    PixelFormat.RedInteger,
                    PixelType.UnsignedByte,
                    p);
                _texWidth = dispW;
                _texHeight = dispH;
            }
            else
            {
                _gl.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    0,
                    0,
                    (uint)dispW,
                    (uint)dispH,
                    PixelFormat.RedInteger,
                    PixelType.UnsignedByte,
                    p);
            }
        }

        // Compute integer-scale letterbox viewport.
        var scale = Math.Min(fbW / dispW, fbH / dispH);
        if (scale < 1) scale = 1;
        var drawW = dispW * scale;
        var drawH = dispH * scale;
        var x = (fbW - drawW) / 2;
        var y = (fbH - drawH) / 2;
        _gl.Viewport(x, y, (uint)drawW, (uint)drawH);

        _gl.UseProgram(_program);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        if (_uDisplayLoc >= 0) _gl.Uniform1(_uDisplayLoc, 0);
        if (_uPaletteLoc >= 0)
        {
            fixed (float* pp = DefaultPalette)
            {
                _gl.Uniform4(_uPaletteLoc, 4, pp);
            }
        }

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        _gl.BindVertexArray(0);

        // Schedule the next frame.
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
    }

    private (int width, int height) GetPixelSize()
    {
        var scaling = VisualRoot?.RenderScaling ?? 1.0;
        var w = Math.Max(1, (int)(Bounds.Width * scaling));
        var h = Math.Max(1, (int)(Bounds.Height * scaling));
        return (w, h);
    }

    private static uint CompileShader(GL gl, ShaderType type, string src)
    {
        var s = gl.CreateShader(type);
        gl.ShaderSource(s, src);
        gl.CompileShader(s);
        gl.GetShader(s, ShaderParameterName.CompileStatus, out var ok);
        if (ok == 0)
        {
            var log = gl.GetShaderInfoLog(s);
            throw new InvalidOperationException($"Shader compile failed ({type}): {log}");
        }
        return s;
    }

    private static string LoadEmbeddedText(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var s = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }
}
