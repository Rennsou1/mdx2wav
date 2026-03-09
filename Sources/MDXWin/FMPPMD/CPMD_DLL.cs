using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace FMPPMD {
    public class CPMD_DLL : IDisposable {
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr hModule);

        public const int BufLenMax = 1024;

        public enum EErrorCode {
            PMDWIN_OK = 0, // 正常終了
            ERR_OPEN_MUSIC_FILE = 1, // 曲 データを開けなかった
            ERR_WRONG_MUSIC_FILE = 2, // PMD の曲データではなかった
            ERR_OPEN_PPC_FILE = 3, // PPC を開けなかった
            ERR_OPEN_P86_FILE = 4, // P86 を開けなかった
            ERR_OPEN_PPS_FILE = 5, // PPS を開けなかった
            ERR_OPEN_PPZ1_FILE = 6, // PPZ1 を開けなかった
            ERR_OPEN_PPZ2_FILE = 7, // PPZ2 を開けなかった
            ERR_WRONG_PPC_FILE = 8, // PPC/PVI ではなかった
            ERR_WRONG_P86_FILE = 9, // P86 ではなかった
            ERR_WRONG_PPS_FILE = 0, // PPS ではなかった
            ERR_WRONG_PPZ1_FILE = 11, // PVI/PZI ではなかった(PPZ1)
            ERR_WRONG_PPZ2_FILE = 12, // PVI/PZI ではなかった(PPZ2)
            WARNING_PPC_ALREADY_LOAD = 13, // PPC はすでに読み込まれている
            WARNING_P86_ALREADY_LOAD = 14, // P86 はすでに読み込まれている
            WARNING_PPS_ALREADY_LOAD = 15, // PPS はすでに読み込まれている
            WARNING_PPZ1_ALREADY_LOAD = 16, // PPZ1 はすでに読み込まれている
            WARNING_PPZ2_ALREADY_LOAD = 17, // PPZ2 はすでに読み込まれている

            ERR_WRONG_PARTNO = 30, // パート番号が不適
            // ERR_ALREADY_MASKED=31, // 指定パートはすでにマスクされている
            ERR_NOT_MASKED = 32, // 指定パートはマスクされていない
            ERR_MUSIC_STOPPED = 33, // 曲が止まっているのにマスク操作をした
            ERR_EFFECT_USED = 34, // 効果音で使用中なのでマスクを操作できない

            ERR_OUT_OF_MEMORY = 99, // メモリを確保できなかった
            ERR_OTHER = 999, // その他のエラー
        }

        private IntPtr Library = 0;

        public delegate int Tgetversion();
        public Tgetversion getversion;

        public delegate int Tgetinterfaceversion();
        public Tgetinterfaceversion getinterfaceversion;

        public delegate bool Tpmdwininit(string path);
        public Tpmdwininit pmdwininit; // ym2608_adpcm_rom.binがあるパスを指定する

        public delegate bool Tloadrhythmsample(string path);
        public Tloadrhythmsample loadrhythmsample; // ym2608_adpcm_rom.binがあるパスを指定する

        public delegate bool Tsetpcmdir(string[] pcmdir);
        public Tsetpcmdir setpcmdir;

        public delegate void Tsetpcmrate(int rate);
        public Tsetpcmrate setpcmrate;

        public delegate void Tsetppzrate(int rate);
        public Tsetppzrate setppzrate;

        public delegate void Tsetppsuse(bool value);
        public Tsetppsuse setppsuse;

        public delegate void Tsetrhythmwithssgeffect(bool value);
        public Tsetrhythmwithssgeffect setrhythmwithssgeffect;

        public delegate void Tsetpmd86pcmmode(bool value);
        public Tsetpmd86pcmmode setpmd86pcmmode;

        public delegate bool Tgetpmd86pcmmode();
        public Tgetpmd86pcmmode getpmd86pcmmode;

        public delegate EErrorCode Tmusic_load(string filename);
        public Tmusic_load music_load;

        public delegate EErrorCode Tmusic_load2(byte[] musdata, int size);
        public Tmusic_load2 music_load2;

        public delegate void Tmusic_start();
        public Tmusic_start music_start;

        public delegate void Tmusic_stop();
        public Tmusic_stop music_stop;

        public delegate void Tfadeout(int speed);
        public Tfadeout fadeout;

        public delegate void Tfadeout2(int speed);
        public Tfadeout2 fadeout2;

        public delegate void Tgetpcmdata(short[] buf, int nsamples);
        public Tgetpcmdata getpcmdata;

        public delegate void Tsetfmcalc55k(bool flag);
        public Tsetfmcalc55k setfmcalc55k;

        public delegate void Tsetppsinterpolation(bool ip);
        public Tsetppsinterpolation setppsinterpolation;

        public delegate void Tsetp86interpolation(bool ip);
        public Tsetp86interpolation setp86interpolation;

        public delegate void Tsetppzinterpolation(bool ip);
        public Tsetppzinterpolation setppzinterpolation;

        public delegate string Tgetmemo(string dest, byte[] musdata, int size, int al);
        public Tgetmemo getmemo;

        public delegate string Tgetmemo2(string dest, byte[] musdata, int size, int al);
        public Tgetmemo2 getmemo2;

        public delegate string Tgetmemo3(string dest, byte[] musdata, int size, int al);
        public Tgetmemo3 getmemo3;

        public delegate EErrorCode Tfgetmemo(string dest, string filename, int al);
        public Tfgetmemo fgetmemo;

        public delegate EErrorCode Tfgetmemo2(string dest, string filename, int al);
        public Tfgetmemo2 fgetmemo2;

        public delegate EErrorCode Tfgetmemo3(IntPtr dest, string filename, int al);
        public Tfgetmemo3 fgetmemo3;

        public delegate IntPtr Tgetmusicfilename(IntPtr dest);
        public Tgetmusicfilename getmusicfilename;

        public delegate IntPtr Tgetpcmfilename(IntPtr dest);
        public Tgetpcmfilename getpcmfilename;

        public delegate IntPtr Tgetppcfilename(IntPtr dest);
        public Tgetppcfilename getppcfilename;

        public delegate IntPtr Tgetppsfilename(IntPtr dest);
        public Tgetppsfilename getppsfilename;

        public delegate IntPtr Tgetp86filename(IntPtr dest);
        public Tgetp86filename getp86filename;

        public delegate IntPtr Tgetppzfilename(IntPtr dest, int bufnum);
        public Tgetppzfilename getppzfilename;

        public delegate EErrorCode Tppc_load(string filename);
        public Tppc_load ppc_load;

        public delegate EErrorCode Tpps_load(string filename);
        public Tpps_load pps_load;

        public delegate EErrorCode Tp86_load(string filename);
        public Tp86_load p86_load;

        public delegate EErrorCode Tppz_load(string filename, int bufnum);
        public Tppz_load ppz_load;

        public delegate EErrorCode Tmaskon(int ch);
        public Tmaskon maskon;

        public delegate EErrorCode Tmaskoff(int ch);
        public Tmaskoff maskoff;

        public delegate void Tsetfmvoldown(int voldown);
        public Tsetfmvoldown setfmvoldown;

        public delegate void Tsetssgvoldown(int voldown);
        public Tsetssgvoldown setssgvoldown;

        public delegate void Tsetrhythmvoldown(int voldown);
        public Tsetrhythmvoldown setrhythmvoldown;

        public delegate void Tsetadpcmvoldown(int voldown);
        public Tsetadpcmvoldown setadpcmvoldown;

        public delegate void Tsetppzvoldown(int voldown);
        public Tsetppzvoldown setppzvoldown;

        public delegate int Tgetfmvoldown();
        public Tgetfmvoldown getfmvoldown;

        public delegate int Tgetfmvoldown2();
        public Tgetfmvoldown2 getfmvoldown2;

        public delegate int Tgetssgvoldown();
        public Tgetssgvoldown getssgvoldown;

        public delegate int Tgetssgvoldown2();
        public Tgetssgvoldown2 getssgvoldown2;

        public delegate int Tgetrhythmvoldown();
        public Tgetrhythmvoldown getrhythmvoldown;

        public delegate int Tgetrhythmvoldown2();
        public Tgetrhythmvoldown2 getrhythmvoldown2;

        public delegate int Tgetadpcmvoldown();
        public Tgetadpcmvoldown getadpcmvoldown;

        public delegate int Tgetadpcmvoldown2();
        public Tgetadpcmvoldown2 getadpcmvoldown2;

        public delegate int Tgetppzvoldown();
        public Tgetppzvoldown getppzvoldown;

        public delegate int Tgetppzvoldown2();
        public Tgetppzvoldown2 getppzvoldown2;

        public delegate void Tsetpos(int pos);
        public Tsetpos setpos;

        public delegate void Tsetpos2(int pos);
        public Tsetpos2 setpos2;

        public delegate int Tgetpos();
        public Tgetpos getpos;

        public delegate int Tgetpos2();
        public Tgetpos2 getpos2;

        public delegate bool Tgetlength(string filename, out int length, out int loop);
        public Tgetlength getlength;

        public delegate bool Tgetlength2(string filename, out int length, out int loop);
        public Tgetlength2 getlength2;

        public delegate int Tgetloopcount();
        public Tgetloopcount getloopcount;

        public delegate void Tsetfmwait(int nsec);
        public Tsetfmwait setfmwait;

        public delegate void Tsetssgwait(int nsec);
        public Tsetssgwait setssgwait;

        public delegate void Tsetrhythmwait(int nsec);
        public Tsetrhythmwait setrhythmwait;

        public delegate void Tsetadpcmwait(int nsec);
        public Tsetadpcmwait setadpcmwait;

        public delegate IntPtr Tgetopenwork(); // pOPEN_WORK または pOPEN_WORK2
        public Tgetopenwork getopenwork;

        public delegate IntPtr Tgetpartwork(int ch); // pQQ
        public Tgetpartwork getpartwork;

        public CPMD_DLL(string DLLName) {
            Library = LoadLibrary(DLLName);

            getversion = (Tgetversion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getversion"), typeof(Tgetversion));
            getinterfaceversion = (Tgetinterfaceversion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getinterfaceversion"), typeof(Tgetinterfaceversion));

            if ((getinterfaceversion() < 117) || (200 <= getinterfaceversion())) { throw new Exception("Interface version check error. " + DLLName); }

            pmdwininit = (Tpmdwininit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "pmdwininit"), typeof(Tpmdwininit));
            loadrhythmsample = (Tloadrhythmsample)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "loadrhythmsample"), typeof(Tloadrhythmsample));
            setpcmdir = (Tsetpcmdir)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setpcmdir"), typeof(Tsetpcmdir));
            setpcmrate = (Tsetpcmrate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setpcmrate"), typeof(Tsetpcmrate));
            setppzrate = (Tsetppzrate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setppzrate"), typeof(Tsetppzrate));
            setppsuse = (Tsetppsuse)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setppsuse"), typeof(Tsetppsuse));
            setrhythmwithssgeffect = (Tsetrhythmwithssgeffect)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setrhythmwithssgeffect"), typeof(Tsetrhythmwithssgeffect));
            setpmd86pcmmode = (Tsetpmd86pcmmode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setpmd86pcmmode"), typeof(Tsetpmd86pcmmode));
            getpmd86pcmmode = (Tgetpmd86pcmmode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpmd86pcmmode"), typeof(Tgetpmd86pcmmode));
            music_load = (Tmusic_load)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "music_load"), typeof(Tmusic_load));
            music_load2 = (Tmusic_load2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "music_load2"), typeof(Tmusic_load2));
            music_start = (Tmusic_start)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "music_start"), typeof(Tmusic_start));
            music_stop = (Tmusic_stop)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "music_stop"), typeof(Tmusic_stop));
            fadeout = (Tfadeout)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fadeout"), typeof(Tfadeout));
            fadeout2 = (Tfadeout2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fadeout2"), typeof(Tfadeout2));
            getpcmdata = (Tgetpcmdata)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpcmdata"), typeof(Tgetpcmdata));
            setfmcalc55k = (Tsetfmcalc55k)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setfmcalc55k"), typeof(Tsetfmcalc55k));
            setppsinterpolation = (Tsetppsinterpolation)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setppsinterpolation"), typeof(Tsetppsinterpolation));
            setp86interpolation = (Tsetp86interpolation)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setp86interpolation"), typeof(Tsetp86interpolation));
            setppzinterpolation = (Tsetppzinterpolation)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setppzinterpolation"), typeof(Tsetppzinterpolation));
            getmemo = (Tgetmemo)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getmemo"), typeof(Tgetmemo));
            getmemo2 = (Tgetmemo2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getmemo2"), typeof(Tgetmemo2));
            getmemo3 = (Tgetmemo3)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getmemo3"), typeof(Tgetmemo3));
            fgetmemo = (Tfgetmemo)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fgetmemo"), typeof(Tfgetmemo));
            fgetmemo2 = (Tfgetmemo2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fgetmemo2"), typeof(Tfgetmemo2));
            fgetmemo3 = (Tfgetmemo3)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fgetmemo3"), typeof(Tfgetmemo3));
            getmusicfilename = (Tgetmusicfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getmusicfilename"), typeof(Tgetmusicfilename));
            getpcmfilename = (Tgetpcmfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpcmfilename"), typeof(Tgetpcmfilename));
            getppcfilename = (Tgetppcfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getppcfilename"), typeof(Tgetppcfilename));
            getppsfilename = (Tgetppsfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getppsfilename"), typeof(Tgetppsfilename));
            getp86filename = (Tgetp86filename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getp86filename"), typeof(Tgetp86filename));
            getppzfilename = (Tgetppzfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getppzfilename"), typeof(Tgetppzfilename));
            ppc_load = (Tppc_load)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "ppc_load"), typeof(Tppc_load));
            pps_load = (Tpps_load)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "pps_load"), typeof(Tpps_load));
            p86_load = (Tp86_load)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "p86_load"), typeof(Tp86_load));
            ppz_load = (Tppz_load)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "ppz_load"), typeof(Tppz_load));
            maskon = (Tmaskon)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "maskon"), typeof(Tmaskon));
            maskoff = (Tmaskoff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "maskoff"), typeof(Tmaskoff));
            setfmvoldown = (Tsetfmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setfmvoldown"), typeof(Tsetfmvoldown));
            setssgvoldown = (Tsetssgvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setssgvoldown"), typeof(Tsetssgvoldown));
            setrhythmvoldown = (Tsetrhythmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setrhythmvoldown"), typeof(Tsetrhythmvoldown));
            setadpcmvoldown = (Tsetadpcmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setadpcmvoldown"), typeof(Tsetadpcmvoldown));
            setppzvoldown = (Tsetppzvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setppzvoldown"), typeof(Tsetppzvoldown));
            getfmvoldown = (Tgetfmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getfmvoldown"), typeof(Tgetfmvoldown));
            getfmvoldown2 = (Tgetfmvoldown2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getfmvoldown2"), typeof(Tgetfmvoldown2));
            getssgvoldown = (Tgetssgvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getssgvoldown"), typeof(Tgetssgvoldown));
            getssgvoldown2 = (Tgetssgvoldown2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getssgvoldown2"), typeof(Tgetssgvoldown2));
            getrhythmvoldown = (Tgetrhythmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getrhythmvoldown"), typeof(Tgetrhythmvoldown));
            getrhythmvoldown2 = (Tgetrhythmvoldown2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getrhythmvoldown2"), typeof(Tgetrhythmvoldown2));
            getadpcmvoldown = (Tgetadpcmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getadpcmvoldown"), typeof(Tgetadpcmvoldown));
            getadpcmvoldown2 = (Tgetadpcmvoldown2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getadpcmvoldown2"), typeof(Tgetadpcmvoldown2));
            getppzvoldown = (Tgetppzvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getppzvoldown"), typeof(Tgetppzvoldown));
            getppzvoldown2 = (Tgetppzvoldown2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getppzvoldown2"), typeof(Tgetppzvoldown2));
            setpos = (Tsetpos)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setpos"), typeof(Tsetpos));
            setpos2 = (Tsetpos2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setpos2"), typeof(Tsetpos2));
            getpos = (Tgetpos)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpos"), typeof(Tgetpos));
            getpos2 = (Tgetpos2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpos2"), typeof(Tgetpos2));
            getlength = (Tgetlength)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getlength"), typeof(Tgetlength));
            getlength2 = (Tgetlength2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getlength2"), typeof(Tgetlength2));
            getloopcount = (Tgetloopcount)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getloopcount"), typeof(Tgetloopcount));
            setfmwait = (Tsetfmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setfmwait"), typeof(Tsetfmwait));
            setssgwait = (Tsetssgwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setssgwait"), typeof(Tsetssgwait));
            setrhythmwait = (Tsetrhythmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setrhythmwait"), typeof(Tsetrhythmwait));
            setadpcmwait = (Tsetadpcmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "setadpcmwait"), typeof(Tsetadpcmwait));
            getopenwork = (Tgetopenwork)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getopenwork"), typeof(Tgetopenwork));
            getpartwork = (Tgetpartwork)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "getpartwork"), typeof(Tgetpartwork));
        }

        public void Dispose() {
            if (Library != 0) {
                FreeLibrary(Library);
                Library = 0;
            }
        }

        public bool isLoaded() { return Library != 0; }
    }
}
