using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MoonLib {
    internal class CDirectDraw {
        private int SrcWidth, SrcHeight;
        public int DstWidth, DstHeight;

        private SharpDX.Direct3D11.Device Device3D;
        private SharpDX.DXGI.SwapChain SwapChain;
        private SharpDX.Direct3D11.Texture2D BackBuffer;

        private SharpDX.Direct2D1.Factory Factory2D;
        private SharpDX.Direct2D1.RenderTarget RenderTarget2D;

        private SharpDX.Direct2D1.Bitmap bm;

        private const SharpDX.DXGI.Format PixelFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm;

        public CDirectDraw(System.Windows.Forms.Panel WindowsFormPanel, int _SrcWidth, int _SrcHeight, int _DstWidth, int _DstHeight) {
            SrcWidth = _SrcWidth;
            SrcHeight = _SrcHeight;
            DstWidth = _DstWidth;
            DstHeight = _DstHeight;

            var desc = new SharpDX.DXGI.SwapChainDescription() {
                BufferCount = 1,
                ModeDescription = new SharpDX.DXGI.ModeDescription(DstWidth, DstHeight, new SharpDX.DXGI.Rational(60, 1), PixelFormat),
                IsWindowed = true,
                OutputHandle = WindowsFormPanel.Handle,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            try {
                SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport, new[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }, desc, out Device3D, out SwapChain);
            } catch (Exception exhard) {
                Debug.WriteLine(Lang.CLang.GetDirect3D(Lang.CLang.EDirect3D.Hardware_CantInit) + " " + exhard.Message);
                try {
                    SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Warp, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport, new[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }, desc, out Device3D, out SwapChain);
                } catch (Exception exwarp) {
                    Debug.WriteLine(Lang.CLang.GetDirect3D(Lang.CLang.EDirect3D.Wrap_CantInit) + " " + exwarp.Message);
                    try {
                        SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Software, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport, new[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }, desc, out Device3D, out SwapChain);
                    } catch (Exception exsoft) {
                        Debug.WriteLine(Lang.CLang.GetDirect3D(Lang.CLang.EDirect3D.Software_CantInit) + " " + exsoft.Message);
                        try {
                            SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Reference, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport, new[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }, desc, out Device3D, out SwapChain);
                        } catch (Exception exref) {
                            Debug.WriteLine(Lang.CLang.GetDirect3D(Lang.CLang.EDirect3D.Reference_CantInit) + " " + exref.Message);
                            throw new Exception(Lang.CLang.GetDirect3D(Lang.CLang.EDirect3D.CantInit));
                        }
                    }
                }
            }

            // Ignore all windows events
            var factory = SwapChain.GetParent<SharpDX.DXGI.Factory>();
            factory.MakeWindowAssociation(WindowsFormPanel.Handle, SharpDX.DXGI.WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            BackBuffer = SharpDX.Direct3D11.Texture2D.FromSwapChain<SharpDX.Direct3D11.Texture2D>(SwapChain, 0);

            Factory2D = new SharpDX.Direct2D1.Factory();
            using (var surface = BackBuffer.QueryInterface<SharpDX.DXGI.Surface>()) {
                RenderTarget2D = new SharpDX.Direct2D1.RenderTarget(Factory2D, surface, new SharpDX.Direct2D1.RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, SharpDX.Direct2D1.AlphaMode.Ignore)));
            }
            RenderTarget2D.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;

            var bitmapProperties = new SharpDX.Direct2D1.BitmapProperties(new SharpDX.Direct2D1.PixelFormat(PixelFormat, SharpDX.Direct2D1.AlphaMode.Ignore));
            bm = new SharpDX.Direct2D1.Bitmap(RenderTarget2D, new SharpDX.Size2(SrcWidth, SrcHeight), bitmapProperties);
        }

        public void Free() {
            if (bm != null) {
                bm.Dispose();
                bm = null;
            }

            if (RenderTarget2D != null) {
                RenderTarget2D.Dispose();
                RenderTarget2D = null;
            }

            if (Factory2D != null) {
                Factory2D.Dispose();
                Factory2D = null;
            }

            if (BackBuffer != null) {
                BackBuffer.Dispose();
                BackBuffer = null;
            }

            if (SwapChain != null) {
                SwapChain.Dispose();
                SwapChain = null;
            }

            if (Device3D != null) {
                Device3D.Dispose();
                Device3D = null;
            }
        }

        public void UpdateFrame(Int32[] buf) {
            bm.CopyFromMemory(buf, SrcWidth * 4);

            var dstrect = new SharpDX.Mathematics.Interop.RawRectangleF(0, 0, DstWidth, DstHeight);

            RenderTarget2D.BeginDraw();
            RenderTarget2D.DrawBitmap(bm, dstrect, 1, SharpDX.Direct2D1.BitmapInterpolationMode.NearestNeighbor);
            RenderTarget2D.EndDraw();

            var WaitVSync = true;
            SwapChain.Present(WaitVSync ? 1 : 0, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
