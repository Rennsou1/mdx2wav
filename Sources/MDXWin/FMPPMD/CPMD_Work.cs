using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FMPPMD {
    public class CPMD_Work {
        public const int NumOfFMPart = 6;
        public const int NumOfSSGPart = 3;
        public const int NumOfADPCMPart = 1;
        public const int NumOFOPNARhythmPart = 1;
        public const int NumOfExtPart = 3;
        public const int NumOfRhythmPart = 1;
        public const int NumOfEffPart = 1;
        public const int NumOfPPZ8Part = 8;
        public const int NumOfAllPart = NumOfFMPart + NumOfSSGPart + NumOfADPCMPart + NumOFOPNARhythmPart + NumOfExtPart + NumOfRhythmPart + NumOfEffPart + NumOfPPZ8Part;

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct TQQ { // パートワークの定義
            public IntPtr address; // 2 ｴﾝｿｳﾁｭｳ ﾉ ｱﾄﾞﾚｽ PByte uint8_t*
            public IntPtr partloop; // 2 ｴﾝｿｳ ｶﾞ ｵﾜｯﾀﾄｷ ﾉ ﾓﾄﾞﾘｻｷ PByte uint8_t*
            public int leng; // 1 ﾉｺﾘ LENGTH
            public int qdat; // 1 gatetime (q/Q値を計算した値)
            public uint fnum; // 2 ｴﾝｿｳﾁｭｳ ﾉ BLOCK/FNUM
            public int detune; // 2 ﾃﾞﾁｭｰﾝ
            public int lfodat; // 2 LFO DATA
            public int porta_num; // 2 ポルタメントの加減値（全体）
            public int porta_num2; // 2 ポルタメントの加減値（一回）
            public int porta_num3; // 2 ポルタメントの加減値（余り）
            public int volume; // 1 VOLUME
            public int shift; // 1 ｵﾝｶｲ ｼﾌﾄ ﾉ ｱﾀｲ
            public int delay; // 1 LFO [DELAY] 
            public int speed; // 1 [SPEED]
            public int step; // 1 [STEP]
            public int time; // 1 [TIME]
            public int delay2; // 1 [DELAY_2]
            public int speed2; // 1 [SPEED_2]
            public int step2; // 1 [STEP_2]
            public int time2; // 1 [TIME_2]
            public int lfoswi; // 1 LFOSW. B0/tone B1/vol B2/同期 B3/porta B4/tone B5/vol B6/同期
            public int volpush; // 1 Volume PUSHarea
            public int mdepth; // 1 M depth
            public int mdspd; // 1 M speed
            public int mdspd2; // 1 M speed_2
            public int envf; // 1 PSG ENV. [START_FLAG] / -1でextend
            public int eenv_count; // 1 ExtendPSGenv/No=0 AR=1 DR=2 SR=3 RR=4
            public int eenv_ar; // 1 /AR /旧pat
            public int eenv_dr; // 1 /DR /旧pv2
            public int eenv_sr; // 1 /SR /旧pr1
            public int eenv_rr; // 1 /RR /旧pr2
            public int eenv_sl; // 1 /SL
            public int eenv_al; // 1 /AL
            public int eenv_arc; // 1 /ARのカウンタ /旧patb
            public int eenv_drc; // 1 /DRのカウンタ
            public int eenv_src; // 1 /SRのカウンタ /旧pr1b
            public int eenv_rrc; // 1 /RRのカウンタ /旧pr2b
            public int eenv_volume; // 1 /Volume値(0～15)/旧penv
            public int extendmode; // 1 B1/Detune B2/LFO B3/Env Normal/Extend
            public int fmpan; // 1 FM Panning + AMD + PMD
            public int psgpat; // 1 PSG PATTERN [TONE/NOISE/MIX]
            public int voicenum; // 1 音色番号
            public int loopcheck; // 1 ループしたら１ 終了したら３
            public int carrier; // 1 FM Carrier
            public int slot1; // 1 SLOT 1 ﾉ TL
            public int slot3; // 1 SLOT 3 ﾉ TL
            public int slot2; // 1 SLOT 2 ﾉ TL
            public int slot4; // 1 SLOT 4 ﾉ TL
            public int slotmask; // 1 FM slotmask
            public int neiromask; // 1 FM 音色定義用maskdata
            public int lfo_wave; // 1 LFOの波形
            public int partmask; // 1 PartMask b0:通常 b1:効果音 b2:NECPCM用 b3:none b4:PPZ/ADE用 b5:s0時 b6:m b7:一時
            public int keyoff_flag; // 1 KeyoffしたかどうかのFlag
            public int volmask; // 1 音量LFOのマスク
            public int qdata; // 1 qの値
            public int qdatb; // 1 Qの値
            public int hldelay; // 1 HardLFO delay
            public int hldelay_c; // 1 HardLFO delay Counter
            public int _lfodat; // 2 LFO DATA
            public int _delay; // 1 LFO [DELAY] 
            public int _speed; // 1 [SPEED]
            public int _step; // 1 [STEP]
            public int _time; // 1 [TIME]
            public int _delay2; // 1 [DELAY_2]
            public int _speed2; // 1 [SPEED_2]
            public int _step2; // 1 [STEP_2]
            public int _time2; // 1 [TIME_2]
            public int _mdepth; // 1 M depth
            public int _mdspd; // 1 M speed
            public int _mdspd2; // 1 M speed_2
            public int _lfo_wave; // 1 LFOの波形
            public int _volmask; // 1 音量LFOのマスク
            public int mdc; // 1 M depth Counter (変動値)
            public int mdc2; // 1 M depth Counter
            public int _mdc; // 1 M depth Counter (変動値)
            public int _mdc2; // 1 M depth Counter
            public int onkai; // 1 演奏中の音階データ (0ffh:rest)
            public int sdelay; // 1 Slot delay
            public int sdelay_c; // 1 Slot delay counter
            public int sdelay_m; // 1 Slot delay Mask
            public int alg_fb; // 1 音色のalg/fb
            public int keyon_flag; // 1 新音階/休符データを処理したらinc
            public int qdat2; // 1 q 最低保証値
            public int onkai_def; // 1 演奏中の音階データ (転調処理前 / ?fh:rest)
            public int shift_def; // 1 マスター転調値
            public int qdat3; // 1 q Random
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        unsafe public struct TOPEN_WORK2 { // OPEN_WORK の定義（－ファイル名部分他、、メモリ節約）
            public fixed long MusPart[NumOfAllPart]; // パートワークのポインタ 64bitIntPtr=8byte, long=8byte.
            public IntPtr mmlbuf; // Musicdataのaddress+1 PByte uint8_t*
            public IntPtr tondat; // Voicedataのaddress PByte uint8_t*
            public IntPtr efcdat; // FM Effecdataのaddress PByte uint8_t*
            public IntPtr prgdat_adr; // 曲データ中音色データ先頭番地 PByte uint8_t*
            public IntPtr radtbl; // R part offset table 先頭番地 PWord uint16_t*
            public IntPtr rhyadr; // R part 演奏中番地 PByte uint8_t*
            public int rhythmmask; // Rhythm音源のマスク x8c/10hのbitに対応
            public int fm_voldown; // FM voldown 数値
            public int ssg_voldown; // PSG voldown 数値
            public int pcm_voldown; // ADPCM voldown 数値
            public int rhythm_voldown; // RHYTHM voldown 数値
            public int prg_flg; // 曲データに音色が含まれているかflag
            public int x68_flg; // OPM flag
            public int status; // status1
            public int status2; // status2
            public int tempo_d; // tempo (TIMER-B)
            public int fadeout_speed; // Fadeout速度
            public int fadeout_volume; // Fadeout音量
            public int tempo_d_push; // tempo (TIMER-B) / 保存用
            public int syousetu_lng; // 小節の長さ
            public int opncount; // 最短音符カウンタ
            public int TimerAtime; // TimerAカウンタ
            public int effflag; // PSG効果音発声on/off flag(ユーザーが代入)
            public int psnoi; // PSG noise周波数
            public int psnoi_last; // PSG noise周波数(最後に定義した数値)
            public int pcmstart; // PCM音色のstart値
            public int pcmstop; // PCM音色のstop値
            public int rshot_dat; // リズム音源 shot flag
            public fixed int rdat[6]; // リズム音源 音量/パンデータ
            public int rhyvol; // リズムトータルレベル
            public int kshot_dat; // ＳＳＧリズム shot flag
            public int play_flag; // play flag
            public int fade_stop_flag; // Fadeout後 MSTOPするかどうかのフラグ
            public bool kp_rhythm_flag; // K/RpartでRhythm音源を鳴らすかflag
            public int pcm_gs_flag; // ADPCM使用 許可フラグ (0で許可)
            public int slot_detune1; // FM3 Slot Detune値 slot1
            public int slot_detune2; // FM3 Slot Detune値 slot2
            public int slot_detune3; // FM3 Slot Detune値 slot3
            public int slot_detune4; // FM3 Slot Detune値 slot4
            public int TimerB_speed; // TimerBの現在値(=ff_tempoならff中)
            public int fadeout_flag; // 内部からfoutを呼び出した時1
            public int revpan; // PCM86逆相flag
            public int pcm86_vol; // PCM86の音量をSPBに合わせるか?
            public int syousetu; // 小節カウンタ
            public int port22h; // OPN-PORT 22H に最後に出力した値(hlfo)
            public int tempo_48; // 現在のテンポ(clock=48 tの値)
            public int tempo_48_push; // 現在のテンポ(同上/保存用)
            public int _fm_voldown; // FM voldown 数値 (保存用)
            public int _ssg_voldown; // PSG voldown 数値 (保存用)
            public int _pcm_voldown; // PCM voldown 数値 (保存用)
            public int _rhythm_voldown; // RHYTHM voldown 数値 (保存用)
            public int _pcm86_vol; // PCM86の音量をSPBに合わせるか? (保存用)
            public int rshot_bd; // リズム音源 shot inc flag (BD)
            public int rshot_sd; // リズム音源 shot inc flag (SD)
            public int rshot_sym; // リズム音源 shot inc flag (CYM)
            public int rshot_hh; // リズム音源 shot inc flag (HH)
            public int rshot_tom; // リズム音源 shot inc flag (TOM)
            public int rshot_rim; // リズム音源 shot inc flag (RIM)
            public int rdump_bd; // リズム音源 dump inc flag (BD)
            public int rdump_sd; // リズム音源 dump inc flag (SD)
            public int rdump_sym; // リズム音源 dump inc flag (CYM)
            public int rdump_hh; // リズム音源 dump inc flag (HH)
            public int rdump_tom; // リズム音源 dump inc flag (TOM)
            public int rdump_rim; // リズム音源 dump inc flag (RIM)
            public int ch3mode; // ch3 Mode
            public int ppz_voldown; // PPZ8 voldown 数値
            public int _ppz_voldown; // PPZ8 voldown 数値 (保存用)
            public int TimerAflag; // TimerA割り込み中？フラグ
            public int TimerBflag; // TimerB割り込み中？フラグ

            // for PMDWin
            public int rate; // PCM 出力周波数(11k, 22k, 44k, 55k)
            public bool ppz8ip; // PPZ8 で補完するか
            public bool ppsip; // PPS で補完するか
            public bool p86ip; // P86 で補完するか
            public bool use_p86; // P86 を使用しているか
            public int fadeout2_speed; // fadeout(高音質)speed(>0で fadeout)
        }

    }
}