using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonLib {
    internal class CDPI {
        public static bool Inited = false;

        private const double DefDPI = 96;

        private static double x = DefDPI;
        private static double y = DefDPI;

        public static void InitAfterWindowLoaded(System.Windows.Window Window) {
            Inited = true;

            var m = System.Windows.PresentationSource.FromVisual(Window).CompositionTarget.TransformToDevice;
            x = m.M11;
            y = m.M22;
        }

        public static double GetDpiX() { return (DefDPI * x); }
        public static double GetDpiY() { return (DefDPI * y); }

        public static double GetRatioX() { return (x); }
        public static double GetRatioY() { return (y); }
    }
}
