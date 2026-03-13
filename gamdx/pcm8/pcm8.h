// PCM8 single-channel driver
// Modified by Rennsou1_2006 (2026):
//   MAME-accurate ADPCM decoding, 16-bit/8-bit PCM support,
//   Variable frequency mode, PCM8A/PCM8PP runtime switching,
//   SetDriverMode/SetVariableFreq/SoftStop APIs

#pragma once

namespace X68K
{

class Pcm8 {
	static const int TotalVolume = 256;

	int Scale;  // 
	int Pcm;  // 16bit PCM Data
	int Pcm16Prev;  // 16bit,8bitPCMの1つ前のデータ
	int InpPcm,InpPcm_prev,OutPcm;  // HPF用 16bit PCM Data
	int OutInpPcm,OutInpPcm_prev;  // HPF用
	int AdpcmRate;  // 187500(15625*12), 125000(10416.66*12), 93750(7812.5*12), 62500(5208.33*12), 46875(3906.25*12), ...
	int RateCounter;
	int N1Data;  // ADPCM 1サンプルのデータの保存
	int N1DataFlag;  // 0 or 1

	volatile int Mode;
	volatile int Volume;  // x/16
	volatile int PcmKind;  // 0〜4:ADPCM  5:16bitPCM  6:8bitPCM  7:謎
	int Pcm16VolumeShift;  // 16-bit PCM 音量右移位数（参照 MDXWin PCMS16_Volume）
	int DriverMode;        // 0=PCM8PP, 1=PCM8A（运行时通过预扫描 MDX 决定）

	unsigned char DmaLastValue;
	unsigned char AdpcmReg;

	volatile unsigned char *DmaMar;
	volatile unsigned int DmaMtc;
	volatile unsigned char *DmaBar;
	volatile unsigned int DmaBtc;
	volatile int DmaOcr;  // 0:チェイン動作なし 0x08:アレイチェイン 0x0C:リンクアレイチェイン

	int DmaArrayChainSetNextMtcMar();
	int DmaLinkArrayChainSetNextMtcMar();
	int DmaGetByte();
	void adpcm2pcm(unsigned char adpcm);
	void pcm16_2pcm(int pcm16);
	void pcm8_2pcm(int pcm8);

	// 统一 PCM 采样获取内部实现（消除 GetPcm22/GetPcm62 重复）
	int GetPcmInternal(int rateIncrement);

public:

	// ── 可变频率 PCM 状态（方案 B：播放器重定向） ──
	struct VariableState {
		int   mode;          // 0=普通模式, 1=可变频率激活
		int   baseRate;      // 基准频率 (Hz×12)
		int   baseNote;      // 基准样本 slot 号（基准音符）
		int   pendingNote;   // mode-set 时暂存的基准音符
		void *baseAddr;      // 首次播放的数据地址（重定向用）
		int   baseLen;       // 首次播放的数据长度
		int   hasBase;       // 0=未捕获, 1=已捕获
		int   baseBank;      // 捕获时 bank 号（bank 变化时需重新捕获）

		void Clear() {
			mode = 0; hasBase = 0; pendingNote = 0;
			baseAddr = 0; baseLen = 0; baseBank = -1;
		}
		void ResetBase() { hasBase = 0; pendingNote = 0; baseBank = -1; }
		bool IsReady() const { return mode && hasBase; }
	} Var;

	Pcm8(void);
	~Pcm8() {};
	void Init();
	void Reset();

	int Out(void *adrs, int mode, int len);
	int Aot(void *tbl, int mode, int cnt);
	int Lot(void *tbl, int mode);
	int SetMode(int mode);
	void SetDriverMode(int mode);  // 设置 PCM 驱动模式（PCM_DRIVER_PCM8PP / PCM_DRIVER_PCM8A）
	int GetDriverMode();
	int GetRest();
	int GetMode();
	// ── 可变频率 PCM 公共接口 ──
	void SetVariableFreq(int note);           // 根据 note 偏移计算 AdpcmRate
	void SetVariableBaseRate(int rate_hz);    // 设置可变频率基准采样率
	bool IsVariableMode() const { return Var.mode != 0; }
	bool IsVariableRedirectReady() const { return Var.IsReady(); }
	bool IsModeCodeVariable(int mode_byte) const;
	void SoftStop();  // 停止播放但保留 Variable 模式状态

	int GetPcm22();
	int GetPcm62();

};

}

// [EOF]
