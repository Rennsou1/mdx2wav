using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace FMPPMD {
    internal class CFMP_DLL : IDisposable {
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr hModule);

        public const int BufLenMax = CFMP_Work.FMP_COMMENTDATASIZE;

        public enum EErrorCode {
            WINFMP_OK = 0, // 正常終了
            ERR_OPEN_MUSIC_FILE = 1, // 曲 データを開けなかった
            ERR_WRONG_MUSIC_FILE = 2, // PMD の曲データではなかった
            ERR_OPEN_PVI_FILE = 3, // PVI を開けなかった
            ERR_OPEN_PPZ1_FILE = 6, // PPZ1 を開けなかった
            ERR_WRONG_PVI_FILE = 8, // PVI ではなかった
            ERR_WRONG_PPZ1_FILE = 11, // PVI ではなかった(PPZ1)
            WARNING_PVI_ALREADY_LOAD = 13, // PVI はすでに読み込まれている
            WARNING_PPZ1_ALREADY_LOAD = 16, // PPZ1 はすでに読み込まれている

            ERR_WRONG_PARTNO = 30, // パート番号が不適
            // ERR_ALREADY_MASKED = 31; // 指定パートはすでにマスクされている
            ERR_NOT_MASKED = 32, // 指定パートはマスクされていない
            ERR_MUSIC_STOPPED = 33, // 曲が止まっているのにマスク操作をした

            ERR_OUT_OF_MEMORY = 99,		// メモリが足りない
        }

        public struct TComments {
            public IntPtr Comment1, Comment2, Comment3;
        }

        private IntPtr Library = 0;

        public delegate int Tgetversion();
        public Tgetversion getversion;

        public delegate int Tgetinterfaceversion();
        public Tgetinterfaceversion getinterfaceversion;

        public delegate bool Tinit(string path);
        public Tinit init;

        public delegate EErrorCode Tload(string filename);
        public Tload load;

        public delegate EErrorCode Tload2(byte[] musdata, int size);
        public Tload2 load2;

        public delegate void Tstart();
        public Tstart start;

        public delegate void Tstop();
        public Tstop stop;

        public delegate void Tgetpcmdata(short[] buf, int nsamples);
        public Tgetpcmdata getpcmdata;

        public delegate EErrorCode Tmaskon(bool rhythm_flag, int ah);
        public Tmaskon maskon;

        public delegate EErrorCode Tmaskoff(bool rhythm_flag, int ah);
        public Tmaskoff maskoff;

        public delegate bool Tloadrhythmsample(string path);
        public Tloadrhythmsample loadrhythmsample;

        public delegate bool Tsetpcmdir(string[] pcmdir);
        public Tsetpcmdir setpcmdir;

        public delegate void Tsetpcmrate(int rate);
        public Tsetpcmrate setpcmrate;

        public delegate void Tsetppzrate(int rate);
        public Tsetppzrate setppzrate;

        public delegate void Tfadeout(int speed);
        public Tfadeout fadeout;

        public delegate void Tfadeout2(int speed);
        public Tfadeout2 fadeout2;

        public delegate void Tsetfmcalc55k(bool flag);
        public Tsetfmcalc55k setfmcalc55k;

        public delegate void Tsetppzinterpolation(bool ip);
        public Tsetppzinterpolation setppzinterpolation;

        public delegate void Tsetadpcmppz8emulate(bool flag);
        public Tsetadpcmppz8emulate setadpcmppz8emulate;

        public delegate string Tgetcomment(string dest, byte[] musdata, int size);
        public Tgetcomment getcomment;

        public delegate string Tgetcomment2(string dest, byte[] musdata, int size);
        public Tgetcomment2 getcomment2;
        public delegate EErrorCode Tgetcomment3(IntPtr dest, byte[] musdata, int size); // 返値はPComment, TComment dest
        public Tgetcomment3 getcomment3;

        public delegate EErrorCode Tfgetcomment(byte[] dest, string filename);
        public Tfgetcomment fgetcomment;

        public delegate EErrorCode Tfgetcomment2(string dest, string filename);
        public Tfgetcomment2 fgetcomment2;

        public delegate EErrorCode Tfgetcomment3(ref TComments Comment, string filename); // TComment dest
        public Tfgetcomment3 fgetcomment3;

        public delegate string Tgetdefinedpcmfilename(IntPtr dest, byte[] musdata, int size);
        public Tgetdefinedpcmfilename getdefinedpcmfilename;

        public delegate string Tgetdefinedppzfilename(IntPtr dest, byte[] musdata, int size, int bufnum);
        public Tgetdefinedppzfilename getdefinedppzfilename;

        public delegate IntPtr Tfgetdefinedpcmfilename(IntPtr dest, string filename);
        public Tfgetdefinedpcmfilename fgetdefinedpcmfilename;

        public delegate IntPtr Tfgetdefinedppzfilename(IntPtr dest, string filename, int bufnum);
        public Tfgetdefinedppzfilename fgetdefinedppzfilename;

        public delegate IntPtr Tgetmusicfilename(IntPtr dest);
        public Tgetmusicfilename getmusicfilename;

        public delegate IntPtr Tgetpcmfilename(IntPtr dest);
        public Tgetpcmfilename getpcmfilename;

        public delegate IntPtr Tgetppzfilename(IntPtr dest, int bufnum);
        public Tgetppzfilename getppzfilename;

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

        public delegate int Tgetssgvoldown();
        public Tgetssgvoldown getssgvoldown;

        public delegate int Tgetrhythmvoldown();
        public Tgetrhythmvoldown getrhythmvoldown;

        public delegate int Tgetadpcmvoldown();
        public Tgetadpcmvoldown getadpcmvoldown;

        public delegate int Tgetppzvoldown();
        public Tgetppzvoldown getppzvoldown;

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

        public delegate int Tgetsyncscnt();
        public Tgetsyncscnt getsyncscnt;

        public delegate int Tgetlastsyncexttime();
        public Tgetlastsyncexttime getlastsyncexttime;

        public delegate IntPtr Tgetworks();
        public Tgetworks getworks;

        public CFMP_DLL(string DLLName) {
            Library = LoadLibrary(DLLName);

            getversion = (Tgetversion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getversion"), typeof(Tgetversion));
            getinterfaceversion = (Tgetinterfaceversion)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getinterfaceversion"), typeof(Tgetinterfaceversion));

            if ((getinterfaceversion() < 010) || (100 <= getinterfaceversion())) { throw new Exception("Interface version check error. " + DLLName); }

            init = (Tinit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_init"), typeof(Tinit));
            load = (Tload)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_load"), typeof(Tload));
            load2 = (Tload2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_load2"), typeof(Tload2));
            start = (Tstart)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_start"), typeof(Tstart));
            stop = (Tstop)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_stop"), typeof(Tstop));
            getpcmdata = (Tgetpcmdata)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getpcmdata"), typeof(Tgetpcmdata));
            maskon = (Tmaskon)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_maskon"), typeof(Tmaskon));
            maskoff = (Tmaskoff)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_maskoff"), typeof(Tmaskoff));
            loadrhythmsample = (Tloadrhythmsample)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_loadrhythmsample"), typeof(Tloadrhythmsample));
            setpcmdir = (Tsetpcmdir)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setpcmdir"), typeof(Tsetpcmdir));
            setpcmrate = (Tsetpcmrate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setpcmrate"), typeof(Tsetpcmrate));
            setppzrate = (Tsetppzrate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setppzrate"), typeof(Tsetppzrate));
            fadeout = (Tfadeout)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fadeout"), typeof(Tfadeout));
            fadeout2 = (Tfadeout2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fadeout2"), typeof(Tfadeout2));
            setfmcalc55k = (Tsetfmcalc55k)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setfmcalc55k"), typeof(Tsetfmcalc55k));
            setppzinterpolation = (Tsetppzinterpolation)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setppzinterpolation"), typeof(Tsetppzinterpolation));
            setadpcmppz8emulate = (Tsetadpcmppz8emulate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setadpcmppz8emulate"), typeof(Tsetadpcmppz8emulate));
            getcomment = (Tgetcomment)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getcomment"), typeof(Tgetcomment));
            getcomment2 = (Tgetcomment2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getcomment2"), typeof(Tgetcomment2));
            getcomment3 = (Tgetcomment3)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getcomment3"), typeof(Tgetcomment3));
            fgetcomment = (Tfgetcomment)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fgetcomment"), typeof(Tfgetcomment));
            fgetcomment2 = (Tfgetcomment2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fgetcomment2"), typeof(Tfgetcomment2));
            fgetcomment3 = (Tfgetcomment3)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fgetcomment3"), typeof(Tfgetcomment3));
            getdefinedpcmfilename = (Tgetdefinedpcmfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getdefinedpcmfilename"), typeof(Tgetdefinedpcmfilename));
            getdefinedppzfilename = (Tgetdefinedppzfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getdefinedppzfilename"), typeof(Tgetdefinedppzfilename));
            fgetdefinedpcmfilename = (Tfgetdefinedpcmfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fgetdefinedpcmfilename"), typeof(Tfgetdefinedpcmfilename));
            fgetdefinedppzfilename = (Tfgetdefinedppzfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_fgetdefinedppzfilename"), typeof(Tfgetdefinedppzfilename));
            getmusicfilename = (Tgetmusicfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getmusicfilename"), typeof(Tgetmusicfilename));
            getpcmfilename = (Tgetpcmfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getpcmfilename"), typeof(Tgetpcmfilename));
            getppzfilename = (Tgetppzfilename)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getppzfilename"), typeof(Tgetppzfilename));
            setfmvoldown = (Tsetfmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setfmvoldown"), typeof(Tsetfmvoldown));
            setssgvoldown = (Tsetssgvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setssgvoldown"), typeof(Tsetssgvoldown));
            setrhythmvoldown = (Tsetrhythmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setrhythmvoldown"), typeof(Tsetrhythmvoldown));
            setadpcmvoldown = (Tsetadpcmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setadpcmvoldown"), typeof(Tsetadpcmvoldown));
            setppzvoldown = (Tsetppzvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setppzvoldown"), typeof(Tsetppzvoldown));
            getfmvoldown = (Tgetfmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getfmvoldown"), typeof(Tgetfmvoldown));
            getssgvoldown = (Tgetssgvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getssgvoldown"), typeof(Tgetssgvoldown));
            getrhythmvoldown = (Tgetrhythmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getrhythmvoldown"), typeof(Tgetrhythmvoldown));
            getadpcmvoldown = (Tgetadpcmvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getadpcmvoldown"), typeof(Tgetadpcmvoldown));
            getppzvoldown = (Tgetppzvoldown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getppzvoldown"), typeof(Tgetppzvoldown));
            setpos = (Tsetpos)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setpos"), typeof(Tsetpos));
            setpos2 = (Tsetpos2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setpos2"), typeof(Tsetpos2));
            getpos = (Tgetpos)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getpos"), typeof(Tgetpos));
            getpos2 = (Tgetpos2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getpos2"), typeof(Tgetpos2));
            getlength = (Tgetlength)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getlength"), typeof(Tgetlength));
            getlength2 = (Tgetlength2)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getlength2"), typeof(Tgetlength2));
            getloopcount = (Tgetloopcount)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getloopcount"), typeof(Tgetloopcount));
            setfmwait = (Tsetfmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setfmwait"), typeof(Tsetfmwait));
            setssgwait = (Tsetssgwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setssgwait"), typeof(Tsetssgwait));
            setrhythmwait = (Tsetrhythmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setrhythmwait"), typeof(Tsetrhythmwait));
            setadpcmwait = (Tsetadpcmwait)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_setadpcmwait"), typeof(Tsetadpcmwait));
            getsyncscnt = (Tgetsyncscnt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getsyncscnt"), typeof(Tgetsyncscnt));
            getlastsyncexttime = (Tgetlastsyncexttime)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getlastsyncexttime"), typeof(Tgetlastsyncexttime));
            getworks = (Tgetworks)Marshal.GetDelegateForFunctionPointer(GetProcAddress(Library, "fmp_getworks"), typeof(Tgetworks));
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
