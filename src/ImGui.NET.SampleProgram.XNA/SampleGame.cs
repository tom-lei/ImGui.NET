using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Num = System.Numerics;

namespace ImGuiNET.SampleProgram.XNA
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// </summary>
    public class SampleGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        private Texture2D _xnaTexture;
        private IntPtr _imGuiTexture;

        public SampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.PreferMultiSampling = true;

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            InitializeSampleFonts();
            _imGuiRenderer.RebuildFontAtlas();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Texture loading example

			// First, load the texture as a Texture2D (can also be done using the XNA/FNA content pipeline)
			_xnaTexture = CreateTexture(GraphicsDevice, 300, 150, pixel =>
			{
				var red = (pixel % 300) / 2;
				return new Color(red, 1, 1);
			});

			// Then, bind it to an ImGui-friendly pointer, that we can use during regular ImGui.** calls (see below)
			_imGuiTexture = _imGuiRenderer.BindTexture(_xnaTexture);

            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(clear_color.X, clear_color.Y, clear_color.Z));

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        // Direct port of the example at https://github.com/ocornut/imgui/blob/master/examples/sdl_opengl2_example/main.cpp
        private float f = 0.0f;

        private bool show_test_window = false;
        private bool show_another_window = false;
        private bool show_new_features_window = true;
        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        private byte[] _textBuffer = new byte[100];
        private int _textureButtonClicks = 0;
        private float _vectorFontPreviewSize = 18f;
        private ImFontPtr _vectorFont;

        protected virtual void ImGuiLayout()
        {
            // 1. Show a simple window
            // Tip: if we don't call ImGui.Begin()/ImGui.End() the widgets appears in a window automatically called "Debug"
            {
                ImGui.Text("Hello, world!");
                ImGui.SliderFloat("float", ref f, 0.0f, 1.0f, string.Empty);
                ImGui.ColorEdit3("clear color", ref clear_color);
                if (ImGui.Button("Test Window")) show_test_window = !show_test_window;
                if (ImGui.Button("Another Window")) show_another_window = !show_another_window;
                ImGui.Checkbox("Dear ImGui 1.92 Samples", ref show_new_features_window);
                ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate, ImGui.GetIO().Framerate));

                ImGui.InputText("Text input", _textBuffer, 100);

                ImGui.Text("Texture sample");
                // In Dear ImGui 1.92+, Image takes an ImTextureRef instead of an IntPtr.
                // Construct an ImTextureRef wrapping the backend texture ID.
                var texRef = new ImTextureRef { _TexID = _imGuiTexture };
                ImGui.Image(texRef, new Num.Vector2(300, 150), Num.Vector2.Zero, Num.Vector2.One); // Here, the previously loaded texture is used
                ImGui.ImageWithBg(texRef, new Num.Vector2(300, 40), Num.Vector2.Zero, Num.Vector2.One, new Num.Vector4(0.1f, 0.1f, 0.1f, 1f), Num.Vector4.One);
                if (ImGui.ImageButton("Texture Button", texRef, new Num.Vector2(120, 48)))
                    _textureButtonClicks++;
                ImGui.Text($"ImageButton clicks: {_textureButtonClicks}");
            }

            // 2. Show another simple window, this time using an explicit Begin/End pair
            if (show_another_window)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(200, 100), ImGuiCond.FirstUseEver);
                ImGui.Begin("Another Window", ref show_another_window);
                ImGui.Text("Hello");
                ImGui.End();
            }

            // 3. Show the ImGui test window. Most of the sample code is in ImGui.ShowTestWindow()
            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new Num.Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref show_test_window);
            }

            if (show_new_features_window)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(460, 320), ImGuiCond.FirstUseEver);
                ImGui.Begin("Dear ImGui 1.92 Samples", ref show_new_features_window);
                SubmitNewFeatureSamples();
                ImGui.End();
            }
        }

        private unsafe void SubmitNewFeatureSamples()
        {
            ImGui.TextWrapped("This XNA/FNA sample demonstrates the newer ImTextureRef-, ImTextureData-, and included vector font APIs.");
            ImGui.Separator();

            ImTextureDataPtr fontTexData = ImGui.GetIO().Fonts.TexData;
            if (fontTexData.NativePtr == null)
            {
                ImGui.TextUnformatted("Font atlas texture data is not available yet.");
                return;
            }

            ImGui.Text($"Font atlas status: {fontTexData.Status}");
            ImGui.Text($"Atlas size: {fontTexData.Width} x {fontTexData.Height}");
            ImGui.Text($"TextureId: 0x{fontTexData.GetTexID().ToInt64():X}");

            ImTextureRef fontAtlasRef = fontTexData.GetTexRef();
            float height = Math.Max(72f, 240f * fontTexData.Height / (float)fontTexData.Width);
            ImGui.Image(fontAtlasRef, new Num.Vector2(240, height));
            ImGui.TextUnformatted("The preview above is sourced from ImGui.GetIO().Fonts.TexData.GetTexRef().");

            ImGui.Separator();
            ImGui.Text("Included vector font");
            if (_vectorFont.NativePtr == null)
            {
                ImGui.TextUnformatted("The included vector font is not available.");
                return;
            }

            ImGui.Text($"Debug name: {_vectorFont.GetDebugName()}");
            ImGui.Text($"Loaded: {_vectorFont.IsLoaded()} | LegacySize: {_vectorFont.LegacySize:0.##}");
            ImGui.SliderFloat("Vector preview size", ref _vectorFontPreviewSize, 10f, 48f, "%.0f px");
            ImFontBakedPtr baked = _vectorFont.GetFontBaked(_vectorFontPreviewSize);
            if (baked.NativePtr != null)
            {
                ImGui.Text($"Baked size: {baked.Size:0.##} | Surface: {baked.MetricsTotalSurface}");
            }

            ImGui.PushFont(_vectorFont, _vectorFontPreviewSize);
            ImGui.Text("The quick brown fox jumps over 13 lazy dogs.");
            ImGui.Text("Vector font sample: 0123456789 +-*/ [] {} ()");
            ImGui.PopFont();
        }

        private void InitializeSampleFonts()
        {
            _vectorFont = ImGui.GetIO().Fonts.AddFontDefaultVector();
        }

		public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
		{
			//initialize a texture
			var texture = new Texture2D(device, width, height);

			//the array holds the color for each pixel in the texture
			Color[] data = new Color[width * height];
			for(var pixel = 0; pixel < data.Length; pixel++)
			{
				//the function applies the color according to the specified pixel
				data[pixel] = paint( pixel );
			}

			//set the color
			texture.SetData( data );

			return texture;
		}
	}
}
