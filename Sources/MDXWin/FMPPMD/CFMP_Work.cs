using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FMPPMD {
    internal class CFMP_Work {
        public const int FMP_NumOfFMPart = 6;
        public const int FMP_NumOfSSGPart = 3;
        public const int FMP_NumOfADPCMPart = 1;
        public const int FMP_NumOFOPNARhythmPart = 1;
        public const int FMP_NumOfExtPart = 3;
        public const int FMP_NumOfPPZ8Part = 8;
        public const int FMP_NumOfAllPart = FMP_NumOfFMPart + FMP_NumOfSSGPart + FMP_NumOfADPCMPart + FMP_NumOFOPNARhythmPart + FMP_NumOfExtPart + FMP_NumOfPPZ8Part;

        public const int FMP_MUSDATASIZE = 65536; // 最大曲データサイズ
        public const int FMP_COMMENTDATASIZE = 8192; // 最大３行コメントサイズ

        // ＦＭＰ各種状態保持ｂｉｔ FMP_sysbit
        public const int FMP_SYS_PPZ8PVI = 0x0008; // ＰＰＺ８エミュレート中
        public const int FMP_SYS_PPZ8USE = 0x0040; // ＰＰＺ８ファイル使用中
        public const int FMP_SYS_FADE = 0x2000; // フェードアウト中
        public const int FMP_SYS_LOOP = 0x4000; // ループした
        public const int FMP_SYS_STOP = 0x8000; // 演奏停止中
        public const int FMP_SYS_INIT = FMP_SYS_STOP;

        public const int FMP_PCM_USEV1 = 0x0001; // ＰＶＩ１使用中
        public const int FMP_PCM_USEZ1 = 0x0010; // ＰＰＺ１使用中

        public const int FMP_WLFO_SYNC = 0x0080; // シンクロビット

        public enum EState_flg {
            Keyon = 1 << 0,
            Sular = 1 << 1,
            Continue_of_Tai = 1 << 2,
            This_channel_off = 1 << 3,
            LFO_syncro = 1 << 4,
            Pitchvend_on = 1 << 5,
            Rest = 1 << 6,
            Tai = 1 << 7,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TLFOS { // ＬＦＯワーク構造体定義 ビブラートワーク構造体
            public int LfoSdelay; // ビブラート ディレイ値
            public int LfoSspeed; // ビブラート スピード
            public int LfoScnt_dly; // ビブラート ディレイカウンタ
            public int LfoScnt_spd; // ビブラート スピードカウンタ
            public int LfoSdepth; // ビブラート ずらしカウント値
            public int LfoScnt_dep; // ビブラート ずらしカウンタ
            public int LfoSrate1; // ビブラート かかり値
            public int LfoSrate2; // ビブラート かかり値（サブ）
            public int LfoSwave; // ビブラート 波形
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TALFOS { // ＬＦＯワーク構造体定義 トレモロワーク構造体
            public int AlfoSdelay; // トレモロ ディレイ値
            public int AlfoSspeed; // トレモロ スピード
            public int AlfoScnt_dly; // トレモロ ディレイカウンタ
            public int AlfoScnt_spd; // トレモロ スピード
            public int AlfoSdepth; // トレモロ 変化量
            public int AlfoScnt_dep; // トレモロ 変化量カウンタ
            public int AlfoSrate; // トレモロ かかり値
            public int AlfoSrate_org; // トレモロ かかり値
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TWLFOS { // ＬＦＯワーク構造体定義 ワウワウワーク構造体
            public int WlfoSdelay; // ワウワウ ディレイ値
            public int WlfoSspeed; // ワウワウ スピード
            public int WlfoScnt_dly; // ワウワウ ディレイカウンタ
            public int WlfoScnt_spd; // ワウワウ スピードカウンタ
            public int WlfoSdepth; // ワウワウ 変化量
            public int WlfoScnt_dep; // ワウワウ 変化量カウンタ
            public int WlfoSrate; // ワウワウ かかり値
            public int WlfoSrate_org; // ワウワウ 現在のずらし値
            public int WlfoSrate_now; // ワウワウ 現在のずらし値
            public int WlfoSsync; // ワウワウ シンクロ／マスク
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TPITS { // ピッチベンドワーク構造体定義
            public int PitSdat; // ピッチベンド変化値
            public int PitSdelay; // ディレイ値
            public int PitSspeed; // スピード
            public int PitScnt; // スピードカウンタ
            public int PitSwave; // 目標周波数
            public int PitStarget; // 目標音階
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TENVS { // ＳＳＧエンベロープワーク構造体
            public int EnvSsv; // スタートヴォユーム
            public int EnvSar; // アタックレート
            public int EnvSdr; // ディケイレート
            public int EnvSsl; // サスティンレベル
            public int EnvSsr; // サスティンレート
            public int EnvSrr; // リリースレート
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TCPATS { // 共通パートワーク構造体定義
            public int PartSlfo_f; // ＬＦＯ状態フラグ
            public int PartSdeflen; // デフォルトの音長

            public int PartSvol; // 現在の音量
            public int PartSdat_q; // ゲート処理比較値

            public int PartScnt; // 音長基準カウンタ
            public int PartSorg_q; // ゲート処理用カウンタ

            public int PartStmpvol; // 実際の出力音量
            public int PartSdat_k; // Ｋｅｙｏｎを遅らせる値

            public int PartScnt_k; // Ｋｅｙｏｎ遅らせ処理用カウンタ
            public int PartSbefore; // １つ前の音程

            public int PartSstatus; // 状態フラグ EState_flg
            public int PartSsync; // シンクロフラグ

            public int PartSdetune; // デチューン値

            public TPITS PartSpitch; // ピッチベンド用ワーク
            public TLFOS PartSlfo_0, PartSlfo_1, PartSlfo_2; // ビブラートＬＦＯ（＃０，＃１，＃２）
            public int PartSwave; // 実際の出力周波数

            public int PartSwave2; // １つ前の出力周波数

            public int PartSxtrns; // 音階のずれ用
            public int PartStone; // 現在の音色番号

            public int PartSkeyon; // 外部Ｋｅｙｏｎ取得用

            public int PartSpan; // パン取得用
            public int PartSalg; // 現在のアルゴリズム番号

            public int PartSio; // 出力Ｉ／Ｏアドレス
            public IntPtr PartSpoint; // 読み込みポインタ
            public IntPtr PartSloop; // くり返しポインタ
            public int PartSchan; // チャンネル識別用
            public int PartSbit; // チャンネル制御bit
            public int PartSport; // ＦＭ裏アドレスアクセス用
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        unsafe public struct TFPATS { // ＦＭパートワーク構造体
            public IntPtr FpatSaddr; // ＴＬアドレス
            public TALFOS FpatSalfo; // トレモロ用ワーク
            public TWLFOS FpatSwlfo; // ワウワウ用ワーク
            public int FpatS_hdly; // ＨＬＦＯディレイ
            public int FpatS_hdlycnt; // ＨＬＦＯカウンタ
            public int FpatS_hfreq; // ＨＬＦＯ　ｆｒｅｑ
            public int FpatS_hapms; // ＨＬＦＯ　ＰＭＳ／ＡＭＳ
            public int FpatSextend; // extendモード
            public fixed int FpatSslot_v[4]; // スロットごとの相対値
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TSPATS { // ＳＳＧパートワーク構造体
            public int SpatSnow_vol; // 現在の音量
            public int SpatSflg; // エンヴェロープ状態フラグ
            public int SpatSoct; // オクターブ
            public int SpatSvol; // 現在の出力音量
            public TENVS SpatSenv; // ソフトウェアエンベロープ
            public IntPtr SpatSenvadr; // SSG Env pattern Address PENVS
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TAPATS { // ＡＤＰＣＭパートワーク構造体
            public int ApatSstart; // ＰＣＭ スタートアドレス
            public int ApatSend; // ＰＣＭ エンドアドレス
            public int ApatSdelta; // ＰＣＭ ΔＮ値
        }

        private const int TPARTS_UnionSize = 368;

        [StructLayout(LayoutKind.Sequential, Pack = 8, Size = TPARTS_UnionSize)]
        unsafe public struct TPARTS_FM { // パートワーク構造体
            public TCPATS CPatS; // 共通ワーク
            public TFPATS FPatS; // ＦＭパートワーク構造体
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8, Size = TPARTS_UnionSize)]
        unsafe public struct TPARTS_SSG { // パートワーク構造体
            public TCPATS CPatS; // 共通ワーク
            public TSPATS SPatS; // ＳＳＧパートワーク構造体
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8, Size = TPARTS_UnionSize)]
        unsafe public struct TPARTS_ADPCM { // パートワーク構造体
            public TCPATS CPatS; // 共通ワーク
            public TAPATS APatS; // ＡＤＰＣＭパートワーク構造体
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TFADES { // フェードアウトデータ構造体
            public int FadeSfm;
            public int FadeSssg;
            public int FadeSrhy;
            public int FadeSapcm;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TSYNCS { // 外部同期ワーク構造体
            public int SyncSdat; // 同期データ
            public int SyncScnt; // 同期カウント
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TFMPS { // ＦＭＰ内部ワーク構造体
            public int FmpStempo; // 00 現在のテンポ
            public TSYNCS FmpSsync; // 01 外部同期データ
            public int FmpSsysbit; // 0a ＦＭＰステータスｂｉｔ
            public int FmpScnt_c; // 0c 曲演奏中クロック
            public int FmpScnt_t; // 0e 待ちカウンタ
            public TFADES FmpSfade; // 0f フェードアウト音量
            public TFADES FmpSfade_o; // 13 フェードアウト音量（オリジナル）
            public int FmpSloop_c; // 14 曲ループ回数
            public int FmpStempo_t; // 17 現在のテンポ（予備）
            public int FmpSmix_s; // 18 ??FEDFED
            public int FmpStimer; // 19 タイマーロード値
            public int FmpSnoise; // 1a ＳＳＧのノイズ周波数
            public int FmpSsho; // 1b 小節カウンタ
            public int FmpSpcmuse; // 1d ＰＣＭ使用状況
            public int FmpScnt_ct; // 1f 曲全体カウント数
            public int FmpScnt_cl; // 23 曲ループカウント数
            public int FmpSmix_e; // 29 効果音処理中のmixer
            public int FmpStempo_e; // 2a 効果音時のデフォルトテンポ
        }


        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        unsafe public struct TWORKS2 { // 全体ワーク（－ファイル名部分他）
            public TFMPS ExtBuff; // 外部参照許可ワーク
            public TPARTS_FM Parts_FM_0, Parts_FM_1, Parts_FM_2, Parts_FM_3, Parts_FM_4, Parts_FM_5; // ＦＭ音源ワーク
            public TPARTS_ADPCM Parts_ADPCM; // ＡＤＰＣＭ音源ワーク
            public TPARTS_FM Parts_FMx_0, Parts_FMx_1, Parts_FMx_2; // ＦＭextendワーク
            public TPARTS_ADPCM Parts_PPZ_0, Parts_PPZ_1, Parts_PPZ_2, Parts_PPZ_3, Parts_PPZ_4, Parts_PPZ_5, Parts_PPZ_6, Parts_PPZ_7; // ＰＣＭ(ppz8)音源ワーク
            public TPARTS_SSG Parts_SSG_0, Parts_SSG_1, Parts_SSG_2; // ＳＳＧ音源ワーク

            // リズム音源部ワーク
            public fixed int R_key[16]; // 絶対に変えられない
            public int R_mask;
            public int R_Oncho_cnt;
            public int R_Oncho_def;
            public int RTL_vol;
            public fixed int R_vol[6];
            public fixed int R_pan[6];
            public int R_Loop_now;
            public int R_Sync_flg;
            public int R_State_flg; // イネーブルフラグ
            public TPARTS_FM _R; // 多分リズムチャネル TPARTS_Rhythm？

            // ｂｙｔｅデータワーク
            public int TotalLoop; // ループ終了カウンタ
            public int Loop_cnt; // ループ終了回数
            public int Int_fcT; // 割り込みフェードカウンタ
            public int Int_fc; // 割り込みフェードカウンタ
            public int TimerA_cnt; // Ｔｉｍｅｒディレイ
            public int Ver; // 曲データバージョン
            public int NowPPZmode; // 現在のＰＰＺの再生モード
            public int MusicClockCnt; // 曲のクロックカウント(C??)
            public int ClockCnt; // クロックカウント
            public int PcmHardVol; // ＰＣＭハード音量
            public int ExtendKeyon; // extend状態の3chのkeyon
            public int ExtendAlg; // extendチャンネルのアルゴリズム

            // ｗｏｒｄデータワーク
            public fixed int FM_effect_dat[4]; // 効果音モードずらし値
            public int Play_flg; // 演奏中フラグ
            public int Loop_flg; // ループフラグ
            public int Int_CX; // 割り込みフェードカウンタ

            // チャンネル別ワークアドレステーブル
            public IntPtr Chan_tbl_R; // リズム PPARTS
            public fixed long Chan_tbl[FMP_NumOfFMPart + FMP_NumOfSSGPart + FMP_NumOfPPZ8Part + 1 + FMP_NumOfExtPart + 1]; // PPARTS[] 64bitIntPtr=8byte, long=8byte.
        }
    }
}
