using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImPlotNET;
using System.Runtime.CompilerServices;
using TestDotNetStandardLib;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using static ImGuiNET.ImGuiNative;

namespace ImGuiNET
{
    class Program
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        // private static MemoryEditor _memoryEditor;

        // UI state
        private static float _f = 0.0f;
        private static int _counter = 0;
        private static int _dragInt = 0;
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static bool _showImGuiDemoWindow = true;
        private static bool _showAnotherWindow = false;
        private static bool _showMemoryEditor = false;
        private static bool _showNewFeaturesWindow = true;
        private static byte[] _memoryEditorData;
        private static int _fontAtlasButtonClicks = 0;
        private static int _clipperPinnedIndex = 42;
        private static float _vectorFontPreviewSize = 18f;
        private static ImFontPtr _vectorFont;
        private static uint s_tab_bar_flags = (uint)ImGuiTabBarFlags.Reorderable;
        private static ImGuiListClipperFlags s_clipperFlags = ImGuiListClipperFlags.None;
        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        static void SetThing(out float i, float val) { i = val; }

        static void Main(string[] args)
        {
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            InitializeSampleFonts();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
            // _memoryEditor = new MemoryEditor();
            Random random = new Random();
            _memoryEditorData = Enumerable.Range(0, 1024).Select(i => (byte)random.Next(255)).ToArray();

            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;
            // Main application loop
            while (_window.Exists)
            {
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        private static unsafe void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.Begin(string) / ImGui.End() the widgets automatically appears in a window called "Debug".
            {
                ImGui.Text("");
                ImGui.Text(string.Empty);
                ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)
                ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  // Edit 1 float using a slider from 0.0f to 1.0f    
                //ImGui.ColorEdit3("clear color", ref _clearColor);                   // Edit 3 floats representing a color

                ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

                ImGui.Checkbox("ImGui Demo Window", ref _showImGuiDemoWindow);                 // Edit bools storing our windows open/close state
                ImGui.Checkbox("Another Window", ref _showAnotherWindow);
                ImGui.Checkbox("Memory Editor", ref _showMemoryEditor);
                ImGui.Checkbox("Dear ImGui 1.92 Samples", ref _showNewFeaturesWindow);
                if (ImGui.Button("Button"))                                         // Buttons return true when clicked (NB: most widgets return true when edited/activated)
                    _counter++;
                ImGui.SameLine(0, -1);
                ImGui.Text($"counter = {_counter}");

                ImGui.DragInt("Draggable Int", ref _dragInt);

                float framerate = ImGui.GetIO().Framerate;
                ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
            }

            // 2. Show another simple window. In most cases you will use an explicit Begin/End pair to name your windows.
            if (_showAnotherWindow)
            {
                ImGui.Begin("Another Window", ref _showAnotherWindow);
                ImGui.Text("Hello from another window!");
                if (ImGui.Button("Close Me"))
                    _showAnotherWindow = false;
                ImGui.End();
            }

            // 3. Show the ImGui demo window. Most of the sample code is in ImGui.ShowDemoWindow(). Read its code to learn more about Dear ImGui!
            if (_showImGuiDemoWindow)
            {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            }

            if (_showNewFeaturesWindow)
            {
                ImGui.SetNextWindowSize(new Vector2(520, 560), ImGuiCond.FirstUseEver);
                ImGui.Begin("Dear ImGui 1.92 Samples", ref _showNewFeaturesWindow);
                SubmitNewFeatureSamples();
                ImGui.End();
            }
            
            if (ImGui.TreeNode("Tabs"))
            {
                if (ImGui.TreeNode("Basic"))
                {
                    ImGuiTabBarFlags tab_bar_flags = ImGuiTabBarFlags.None;
                    if (ImGui.BeginTabBar("MyTabBar", tab_bar_flags))
                    {
                        if (ImGui.BeginTabItem("Avocado"))
                        {
                            ImGui.Text("This is the Avocado tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Broccoli"))
                        {
                            ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Cucumber"))
                        {
                            ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Advanced & Close Button"))
                {
                    // Expose a couple of the available flags. In most cases you may just call BeginTabBar() with no flags (0).
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_Reorderable", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.Reorderable);
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_AutoSelectNewTabs", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.AutoSelectNewTabs);
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_NoCloseWithMiddleMouseButton", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);
                    if ((s_tab_bar_flags & (uint)ImGuiTabBarFlags.FittingPolicyMask) == 0)
                        s_tab_bar_flags |= (uint)ImGuiTabBarFlags.FittingPolicyDefault;
                    if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyShrink", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyShrink))
                        s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyShrink);
                    if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyScroll", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyScroll))
                        s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyScroll);

                    // Tab Bar
                    string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };

                    for (int n = 0; n < s_opened.Length; n++)
                    {
                        if (n > 0) { ImGui.SameLine(); }
                        ImGui.Checkbox(names[n], ref s_opened[n]);
                    }

                    // Passing a bool* to BeginTabItem() is similar to passing one to Begin(): the underlying bool will be set to false when the tab is closed.
                    if (ImGui.BeginTabBar("MyTabBar", (ImGuiTabBarFlags)s_tab_bar_flags))
                    {
                        for (int n = 0; n < s_opened.Length; n++)
                            if (s_opened[n] && ImGui.BeginTabItem(names[n], ref s_opened[n]))
                            {
                                ImGui.Text($"This is the {names[n]} tab!");
                                if ((n & 1) != 0)
                                    ImGui.Text("I am an odd tab.");
                                ImGui.EndTabItem();
                            }
                        ImGui.EndTabBar();
                    }
                    ImGui.Separator();
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }

            ImGuiIOPtr io = ImGui.GetIO();
            SetThing(out io.DeltaTime, 2f);

            if (_showMemoryEditor)
            {
                ImGui.Text("Memory editor currently supported.");
                // _memoryEditor.Draw("Memory Editor", _memoryEditorData, _memoryEditorData.Length);
            }
            
            // ReadOnlySpan<char> and .NET Standard 2.0 tests
            TestStringParameterOnDotNetStandard.Text(); // String overloads should always be available.
            
            // On .NET Standard 2.1 or greater, you can use ReadOnlySpan<char> instead of string to prevent allocations.
            long allocBytesStringStart = GC.GetAllocatedBytesForCurrentThread();
            ImGui.Text($"Hello, world {Random.Shared.Next(100)}!");
            long allocBytesStringEnd = GC.GetAllocatedBytesForCurrentThread() - allocBytesStringStart;
            Console.WriteLine("GC (string): " + allocBytesStringEnd);
                
            long allocBytesSpanStart = GC.GetAllocatedBytesForCurrentThread();
            ImGui.Text($"Hello, world {Random.Shared.Next(100)}!".AsSpan()); // Note that this call will STILL allocate memory due to string interpolation, but you can prevent that from happening by using an InterpolatedStringHandler.
            long allocBytesSpanEnd = GC.GetAllocatedBytesForCurrentThread() - allocBytesSpanStart;
            Console.WriteLine("GC (span): " + allocBytesSpanEnd);
            
            ImGui.Text("Empty span:");
            ImGui.SameLine();
            ImGui.GetWindowDrawList().AddText(ImGui.GetCursorScreenPos(), uint.MaxValue, ReadOnlySpan<char>.Empty);
            ImGui.NewLine();
            ImGui.GetWindowDrawList().AddText(ImGui.GetCursorScreenPos(), uint.MaxValue, $"{ImGui.CalcTextSize("h")}");
            ImGui.NewLine();
            ImGui.TextUnformatted("TextUnformatted now passes end ptr but isn't cut off");
        }

        private static void InitializeSampleFonts()
        {
            if (ImGui.GetCurrentContext() == IntPtr.Zero)
            {
                ImGui.CreateContext();
            }

            _vectorFont = ImGui.GetIO().Fonts.AddFontDefaultVector();
        }

        private static unsafe void SubmitNewFeatureSamples()
        {
            ImGui.TextWrapped("Focused demonstrations for the newer texture/font, vector font, and list clipper APIs surfaced by this fork.");
            ImGui.Separator();

            ImGui.Text("Font atlas / ImTextureData");
            ImTextureDataPtr fontTexData = ImGui.GetIO().Fonts.TexData;
            if (fontTexData.NativePtr == null)
            {
                ImGui.TextUnformatted("Font atlas texture data is not available yet.");
            }
            else
            {
                ImGui.Text($"Status: {fontTexData.Status}");
                ImGui.Text($"Size: {fontTexData.Width} x {fontTexData.Height} ({fontTexData.BytesPerPixel} bytes/pixel)");
                ImGui.Text($"UniqueId: {fontTexData.UniqueID} | RefCount: {fontTexData.RefCount}");
                ImGui.Text($"TextureId: 0x{fontTexData.GetTexID().ToInt64():X}");

                ImTextureRef fontTexRef = fontTexData.GetTexRef();
                Vector2 previewSize = new Vector2(256, MathF.Max(96f, 256f * fontTexData.Height / (float)fontTexData.Width));
                ImGui.Image(fontTexRef, previewSize);
                ImGui.SameLine();
                ImGui.BeginGroup();
                ImGui.ImageWithBg(fontTexRef, new Vector2(128, 96), Vector2.Zero, new Vector2(0.35f, 0.35f), new Vector4(0.1f, 0.1f, 0.1f, 1f), Vector4.One);
                if (ImGui.ImageButton("Font Atlas Button", fontTexRef, new Vector2(128, 32)))
                {
                    _fontAtlasButtonClicks++;
                }
                ImGui.Text($"ImageButton clicks: {_fontAtlasButtonClicks}");
                ImGui.EndGroup();
            }

            ImGui.Separator();
            ImGui.Text("Included vector font");
            if (_vectorFont.NativePtr == null)
            {
                ImGui.TextUnformatted("The included vector font is not available.");
            }
            else
            {
                ImGui.Text($"Debug name: {_vectorFont.GetDebugName()}");
                ImGui.Text($"Loaded: {_vectorFont.IsLoaded()} | LegacySize: {_vectorFont.LegacySize:0.##}");
                ImGui.SliderFloat("Vector preview size", ref _vectorFontPreviewSize, 10f, 48f, "%.0f px");
                ImFontBakedPtr baked = _vectorFont.GetFontBaked(_vectorFontPreviewSize);
                if (baked.NativePtr != null)
                {
                    ImGui.Text($"Baked size: {baked.Size:0.##} | Surface: {baked.MetricsTotalSurface} | Ascent/Descent: {baked.Ascent:0.##}/{baked.Descent:0.##}");
                }

                ImGui.PushFont(_vectorFont, _vectorFontPreviewSize);
                ImGui.Text("The quick brown fox jumps over 13 lazy dogs.");
                ImGui.Text("Vector font sample: 0123456789 +-*/ [] {} ()");
                ImGui.PopFont();

                ImGui.TextUnformatted("Same included vector font, reused at multiple sizes:");
                foreach (float size in new[] { 13f, 20f, 32f })
                {
                    ImGui.PushFont(_vectorFont, size);
                    ImGui.Text($"[{size:0}px] Sphinx of black quartz, judge my vow.");
                    ImGui.PopFont();
                }
            }

            ImGui.Separator();
            ImGui.Text("ImGuiListClipper");
            ImGui.SliderInt("Pinned row", ref _clipperPinnedIndex, 0, 199);
            bool noSetTableRowCounters = (s_clipperFlags & ImGuiListClipperFlags.NoSetTableRowCounters) != 0;
            if (ImGui.Checkbox("NoSetTableRowCounters", ref noSetTableRowCounters))
            {
                s_clipperFlags = noSetTableRowCounters ? ImGuiListClipperFlags.NoSetTableRowCounters : ImGuiListClipperFlags.None;
            }

            if (ImGui.BeginChild("Clipper Demo", new Vector2(0, 220), ImGuiChildFlags.Borders))
            {
                ImGuiListClipper* nativeClipper = ImGuiListClipper_ImGuiListClipper();
                if (nativeClipper == null)
                {
                    ImGui.TextUnformatted("Failed to allocate ImGuiListClipper.");
                }
                else
                {
                    try
                    {
                        ImGuiListClipperPtr clipper = nativeClipper;
                        clipper.Flags = s_clipperFlags;
                        clipper.Begin(200, ImGui.GetTextLineHeightWithSpacing());
                        clipper.IncludeItemByIndex(_clipperPinnedIndex);
                        while (clipper.Step())
                        {
                            for (int item = clipper.DisplayStart; item < clipper.DisplayEnd; item++)
                            {
                                bool isPinned = item == _clipperPinnedIndex;
                                if (isPinned)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.85f, 0.35f, 1f));
                                }

                                ImGui.Selectable($"Item {item:000}  | visible range {clipper.DisplayStart:000}-{clipper.DisplayEnd - 1:000}", isPinned);

                                if (isPinned)
                                {
                                    ImGui.PopStyleColor();
                                }
                            }
                        }
                        clipper.End();
                    }
                    finally
                    {
                        ImGuiListClipper_destroy(nativeClipper);
                    }
                }

                ImGui.EndChild();
            }
        }
    }
}
