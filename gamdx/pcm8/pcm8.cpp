// PCM8 single-channel driver implementation
// Modified by Rennsou1_2006 (2026):
//   MAME-accurate ADPCM decoding (diff_lookup + index_shift),
//   16-bit/8-bit PCM playback, Variable frequency mode,
//   PCM8A/PCM8PP runtime switching, SoftStop

#include "pcm8.h"
#include "global.h"
#include <math.h>
#include <stdio.h>  // 诊断日志用

namespace X68K
{

// MAME okim6258 精确 ADPCM diff_lookup 预计算表
// 49 步 × 16 nibble，参考 MSM6258 数据手册和 MAME 实现
static int diff_lookup[49 * 16];
static int diff_tables_computed = 0;

static void compute_diff_tables() {
	if (diff_tables_computed) return;

	// nibble → sign + 3 magnitude bits
	static const int nbl2bit[16][4] = {
		{ 1, 0, 0, 0}, { 1, 0, 0, 1}, { 1, 0, 1, 0}, { 1, 0, 1, 1},
		{ 1, 1, 0, 0}, { 1, 1, 0, 1}, { 1, 1, 1, 0}, { 1, 1, 1, 1},
		{-1, 0, 0, 0}, {-1, 0, 0, 1}, {-1, 0, 1, 0}, {-1, 0, 1, 1},
		{-1, 1, 0, 0}, {-1, 1, 0, 1}, {-1, 1, 1, 0}, {-1, 1, 1, 1},
	};

	for (int step = 0; step <= 48; step++) {
		int stepval = (int)floor(16.0 * pow(11.0 / 10.0, (double)step));
		for (int nib = 0; nib < 16; nib++) {
			diff_lookup[step * 16 + nib] = nbl2bit[nib][0] *
				(stepval   * nbl2bit[nib][1] +
				 stepval/2 * nbl2bit[nib][2] +
				 stepval/4 * nbl2bit[nib][3] +
				 stepval/8);
		}
	}

	diff_tables_computed = 1;
}

// MAME index_shift: nibble の下位 3 bit でステップインデックスを調整
static const int index_shift[8] = { -1, -1, -1, -1, 2, 4, 6, 8 };



inline int MemRead(unsigned char *adrs) {
	return *adrs;
}


int Pcm8::DmaArrayChainSetNextMtcMar() {
	if ( DmaBtc == 0 ) {
		return 1;
	}
	--DmaBtc;

	int mem0,mem1,mem2,mem3,mem4,mem5;
	mem0 = MemRead((unsigned char *)DmaBar++);
	mem1 = MemRead((unsigned char *)DmaBar++);
	mem2 = MemRead((unsigned char *)DmaBar++);
	mem3 = MemRead((unsigned char *)DmaBar++);
	mem4 = MemRead((unsigned char *)DmaBar++);
	mem5 = MemRead((unsigned char *)DmaBar++);
	if ((mem0|mem1|mem2|mem3|mem4|mem5) == -1) {
		// バスエラー(ベースアドレス/ベースカウンタ)
		return 1;
	} 
	DmaMar = (volatile unsigned char *)((mem0<<24)|(mem1<<16)|(mem2<<8)|(mem3));  // MAR
	DmaMtc = (mem4<<8)|(mem5);  // MTC

	if ( DmaMtc == 0 ) {  // MTC == 0 ?
		// カウントエラー(メモリアドレス/メモリカウンタ)
		return 1;
	}
	return 0;
}


int Pcm8::DmaLinkArrayChainSetNextMtcMar() {
	if (DmaBar == (unsigned char *)0) {
		return 1;
	}

	int mem0,mem1,mem2,mem3,mem4,mem5;
	int mem6,mem7,mem8,mem9;
	mem0 = MemRead((unsigned char *)DmaBar++);
	mem1 = MemRead((unsigned char *)DmaBar++);
	mem2 = MemRead((unsigned char *)DmaBar++);
	mem3 = MemRead((unsigned char *)DmaBar++);
	mem4 = MemRead((unsigned char *)DmaBar++);
	mem5 = MemRead((unsigned char *)DmaBar++);
	mem6 = MemRead((unsigned char *)DmaBar++);
	mem7 = MemRead((unsigned char *)DmaBar++);
	mem8 = MemRead((unsigned char *)DmaBar++);
	mem9 = MemRead((unsigned char *)DmaBar++);
	if ((mem0|mem1|mem2|mem3|mem4|mem5|mem6|mem7|mem8|mem9) == -1) {
		// バスエラー(ベースアドレス/ベースカウンタ)
		return 1;
	}
	DmaMar = (volatile unsigned char *)((mem0<<24)|(mem1<<16)|(mem2<<8)|(mem3));  // MAR
	DmaMtc = (mem4<<8)|(mem5);  // MTC
	DmaBar = (volatile unsigned char *)((mem6<<24)|(mem7<<16)|(mem8<<8)|(mem9));  // BAR

	if ( DmaMtc == 0 ) {  // MTC == 0 ?
		// カウントエラー(メモリアドレス/メモリカウンタ)
		return 1;
	}
	return 0;
}


int Pcm8::DmaGetByte() {
	if (DmaMtc == 0) {
		return 0x80000000;
	}
	{
		int mem;
		mem = MemRead((unsigned char *)DmaMar);
		if (mem == -1) {
		// バスエラー(メモリアドレス/メモリカウンタ)
			return 0x80000000;
		}
		DmaLastValue = mem;
		DmaMar += 1;
	}

	--DmaMtc;

//	try {
		if (DmaMtc == 0) {
			if (DmaOcr & 0x08) {  // チェイニング動作
				if (!(DmaOcr & 0x04)) {  // アレイチェイン
					if (DmaArrayChainSetNextMtcMar()) {
//						throw "";
					}
				} else {  // リンクアレイチェイン
					if (DmaLinkArrayChainSetNextMtcMar()) {
//						throw "";
					}
				}
			}
		}
//	} catch (void *) {
//	}

	return DmaLastValue;
}


// 参照 MDXWin CPCMConvert.cs: ADPCM 累加器不做 12-bit clamp
// MDXWin 原作注释: "本当は最下位2ビットを捨てるみたいだけど、捨てない"
// 仅做 16-bit 安全範囲保護（防止 InpPcm = Pcm<<4 溢出 int）
#define PCM_SAFE_MAX  32767
#define PCM_SAFE_MIN  (-32768)


// MAME okim6258 clock_adpcm 準拠の ADPCM デコード
// diff_lookup 事前計算テーブルで delta を一発取得
void Pcm8::adpcm2pcm(unsigned char adpcm) {
	compute_diff_tables();

	// diff_lookup から delta を直接取得（MAME 方式）
	int delta = diff_lookup[Scale * 16 + (adpcm & 15)];
	Pcm += delta;

#ifdef PCM8_DEBUG
	// 诊断: 前50个nibble的解码过程
	{
		static int adpcm_diag_count = 0;
		if (adpcm_diag_count < 50) {
			fprintf(stderr, "[ADPCM #%d] nibble=0x%X Scale=%d delta=%d Pcm=%d\n",
				adpcm_diag_count, adpcm & 15, Scale, delta, Pcm);
			adpcm_diag_count++;
		}
	}
#endif

	// 参照 MDXWin: 12-bit clamp しない（動的範囲を保持）
	// 安全範囲クランプのみ（Pcm<<4 が int 範囲内に収まること）
	if (Pcm > PCM_SAFE_MAX)
		Pcm = PCM_SAFE_MAX;
	else if (Pcm < PCM_SAFE_MIN)
		Pcm = PCM_SAFE_MIN;

	// 16-bit にスケール
	InpPcm = Pcm << 4;

	// ステップインデックス更新（MAME index_shift 準拠）
	Scale += index_shift[adpcm & 7];
	if (Scale > 48)
		Scale = 48;
	else if (Scale < 0)
		Scale = 0;
}


// 16-bit PCM を入力して InpPcm を設定する
// 参照 MDXWin CPDXFile.cs: 直接値を使用、差分積分なし、12-bit クランプなし
// MDXWin では int16 / 2048 で正規化 → ADPCM の Pcm / 2048 と同スケール
// gamdx 整数パイプライン等価: InpPcm = pcm16 >> Pcm16VolumeShift
//   ADPCM の InpPcm = Pcm << 4 (range [-32768, 32752]) と同範囲
void Pcm8::pcm16_2pcm(int pcm16) {
	// 直接代入（差分積分しない）
	// Pcm16VolumeShift: モード $05 → 0 (等倍), モード $07+ → 4 (1/16 縮小)
	InpPcm = pcm16 >> Pcm16VolumeShift;
}


// 8-bit PCM を入力して InpPcm を設定する
// MDXWin では PCMS8 未実装（break のみ）だが、互換性のため合理的にスケーリング
// int8 [-128, 127] → << 8 で ADPCM InpPcm と同範囲 [-32768, 32512]
void Pcm8::pcm8_2pcm(int pcm8) {
	InpPcm = pcm8 << 8;
}


// -32768<<4 <= retval <= +32768<<4
int Pcm8::GetPcm22() {
	if (AdpcmReg & 0x80) {  // ADPCM 停止中
		return 0x80000000;
	}
	RateCounter -= AdpcmRate;
	while (RateCounter < 0) {
		if (PcmKind == 5) {  // 16bitPCM
			int dataH,dataL;
			dataH = DmaGetByte();
			if (dataH == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			dataL = DmaGetByte();
			if (dataL == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			pcm16_2pcm((int)(short)((dataH<<8)|dataL));  // 16-bit PCM: pcm16_2pcm で差分積分処理
		} else if (PcmKind == 6) {  // 8bitPCM
			int data;
			data = DmaGetByte();
			if (data == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			pcm8_2pcm((int)(char)data);  // 8-bit PCM: 直接スケーリング
		} else {
			int N10Data;  // (N1Data << 4) | N0Data
			if (N1DataFlag == 0) {  // 次のADPCMデータが内部にない場合
				N10Data = DmaGetByte();  // DMA転送(1バイト)
				if (N10Data == 0x80000000) {
					RateCounter = 0;
					AdpcmReg = 0xC7;  // ADPCM 停止
					return 0x80000000;
				}
				adpcm2pcm(N10Data & 0x0F);  // InpPcm に値が入る
				N1Data = (N10Data >> 4) & 0x0F;
				N1DataFlag = 1;
			} else {
				adpcm2pcm(N1Data);  // InpPcm に値が入る
				N1DataFlag = 0;
			}
		}
		RateCounter += 15625*12;
	}
	return (InpPcm * Volume) >> 9;
}


// -32768<<4 <= retval <= +32768<<4
int Pcm8::GetPcm62() {
	if (AdpcmReg & 0x80) {  // ADPCM 停止中
		return 0x80000000;
	}
	RateCounter -= AdpcmRate;
	while (RateCounter < 0) {
		if (PcmKind == 5) {  // 16bitPCM
			int dataH,dataL;
			dataH = DmaGetByte();
			if (dataH == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			dataL = DmaGetByte();
			if (dataL == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			pcm16_2pcm((int)(short)((dataH<<8)|dataL));  // 16-bit PCM: pcm16_2pcm で差分積分処理
		} else if (PcmKind == 6) {  // 8bitPCM
			int data;
			data = DmaGetByte();
			if (data == 0x80000000) {
				RateCounter = 0;
				AdpcmReg = 0xC7;  // ADPCM 停止
				return 0x80000000;
			}
			pcm8_2pcm((int)(char)data);  // 8-bit PCM: 直接スケーリング
		} else {
			int N10Data;  // (N1Data << 4) | N0Data
			if (N1DataFlag == 0) {  // 次のADPCMデータが内部にない場合
				N10Data = DmaGetByte();  // DMA転送(1バイト)
				if (N10Data == 0x80000000) {
					RateCounter = 0;
					AdpcmReg = 0xC7;  // ADPCM 停止
					return 0x80000000;
				}
				adpcm2pcm(N10Data & 0x0F);  // InpPcm に値が入る
				N1Data = (N10Data >> 4) & 0x0F;
				N1DataFlag = 1;
			} else {
				adpcm2pcm(N1Data);  // InpPcm に値が入る
				N1DataFlag = 0;
			}
		}
		RateCounter += 15625*12*4;
	}
	return (InpPcm * Volume) >> 9;
}


Pcm8::Pcm8(void) {
	DriverMode = PCM_DRIVER_PCM8PP;
	VariableMode = 0;
	VariableHasBase = 0;
	VariableBaseAddr = NULL;
	VariableBaseLen = 0;
	VariableBaseRate = 15625*12;
	VariableBaseNote = 0;
	Mode = 0x00080403;
	SetMode(Mode);
}


void Pcm8::Init() {
	AdpcmReg = 0xC7;  // ADPCM動作停止

	Scale = 0;
	Pcm = 0;
	Pcm16Prev = 0;
	Pcm16VolumeShift = 0;
	DriverMode = PCM_DRIVER_PCM8PP;  // 默认 PCM8PP（可由预扫描覆盖）
	VariableMode = 0;
	VariableHasBase = 0;
	VariableBaseAddr = NULL;
	VariableBaseLen = 0;
	VariableBaseRate = 15625*12;
	VariableBaseNote = 0;
	InpPcm = InpPcm_prev = OutPcm = 0;
	OutInpPcm = OutInpPcm_prev = 0;
	AdpcmRate = 15625*12;
	RateCounter = 0;
	N1Data = 0;
	N1DataFlag = 0;
	DmaLastValue = 0;

	DmaMar = NULL;
	DmaMtc = 0;
	DmaBar = NULL;
	DmaBtc = 0;
	DmaOcr = 0;
}


void Pcm8::Reset() {  // ADPCM キーオン時の処理
	Scale = 0;
	Pcm = 0;
	Pcm16Prev = 0;
	Pcm16VolumeShift = 0;
	InpPcm = InpPcm_prev = OutPcm = 0;
	OutInpPcm = OutInpPcm_prev = 0;

	N1Data = 0;
	N1DataFlag = 0;
}


int Pcm8::Out(void *adrs, int mode, int len) {
	int note = (mode >> 24) & 0xFF;
	int pan = mode & 0x03;

	// ── 先调用 SetMode 以建立 VariableMode 状态 ──
	// 但对于 stop/control 调用(pan=0)，跳过 SetMode 以避免破坏 Variable 状态
	if (pan != 0) {
		SetMode(mode);
	}

	// len=0 的 Variable 重定向需要 pan!=0 且 VariableHasBase
	if (len <= 0 && !(VariableMode && VariableHasBase && len == 0 && pan != 0)) {
		if (len < 0) {
			return GetRest();
		} else {
			// stop/key-off: 仅停止播放
			if (pan == 0) {
				AdpcmReg = 0xC7;
				DmaMtc = 0;
			}
			return 0;
		}
	}

	AdpcmReg = 0xC7;
	DmaMtc = 0;

	// Variable 模式重定向逻辑（仅对实际音符触发，pan!=0）
	if (VariableMode) {
		if (!VariableHasBase) {
			// 首次播放：捕获 base
			VariableBaseAddr = adrs;
			VariableBaseLen  = len;
			VariableBaseNote = note;
			VariableHasBase  = 1;
#ifdef PCM8_DEBUG
			fprintf(stderr, "[VAR CAPTURE] note=%d addr=%p len=%d baseRate=%d\n",
				note, adrs, len, VariableBaseRate);
#endif
		} else if (len == 0) {
			// 空 slot 重定向到 base 数据
			adrs = VariableBaseAddr;
			len  = VariableBaseLen;
#ifdef PCM8_DEBUG
			fprintf(stderr, "[VAR REDIRECT] note=%d baseNote=%d addr=%p len=%d\n",
				note, VariableBaseNote, adrs, len);
#endif
		}
		// 计算音高偏移后的回放频率
		SetVariableFreq(note);
#ifdef PCM8_DEBUG
		fprintf(stderr, "[VAR FREQ] note=%d diff=%d AdpcmRate=%d\n",
			note, note - VariableBaseNote, AdpcmRate);
#endif
	}

	DmaMar = (unsigned char *)adrs;
	if (pan != 0) {
		DmaMtc = len;
		Reset();
		AdpcmReg = 0x47;
#ifdef PCM8_DEBUG
		fprintf(stderr, "[PCM8 PLAY] mode=0x%08X PcmKind=%d AdpcmRate=%d DmaMtc=%u note=%d VarMode=%d\n",
			Mode, PcmKind, AdpcmRate, DmaMtc, note, VariableMode);
#endif
	}
	return 0;
}


int Pcm8::Aot(void *tbl, int mode, int cnt) {
	if (cnt <= 0) {
		if (cnt < 0) {
			return GetRest();
		} else {
			DmaMtc = 0;
			return 0;
		}
	}
	AdpcmReg = 0xC7;  // ADPCM 停止
	DmaMtc = 0;
	DmaBar = (unsigned char *)tbl;
	DmaBtc = cnt;
	SetMode(mode);
	if ((mode&3) != 0) {
		DmaArrayChainSetNextMtcMar();
		Reset();
		AdpcmReg = 0x47;  // ADPCM 動作開始
	}
	return 0;
}


int Pcm8::Lot(void *tbl, int mode) {
	AdpcmReg = 0xC7;  // ADPCM 停止
	DmaMtc = 0;
	DmaBar = (unsigned char *)tbl;
	SetMode(mode);
	if ((mode&3) != 0) {
		DmaLinkArrayChainSetNextMtcMar();
		Reset();
		AdpcmReg = 0x47;  // ADPCM 動作開始
	}
	return 0;
}


int Pcm8::SetMode(int mode) {
	int m;
#ifdef PCM8_DEBUG
	fprintf(stderr, "[PCM8 SetMode] input mode=0x%08X\n", mode);
#endif
	m = (mode>>16) & 0xFF;
	if (m != 0xFF) {
		m &= 15;
		Volume = PCM8VOLTBL[m];
		Mode = (Mode&0xFF00FFFF)|(m<<16);
	}
	m = (mode>>8) & 0xFF;
	if (m != 0xFF) {
		m &= 0x3F;  // 6-bit 模式码范围
		// 根据驱动模式选择对应的模式码表
		if (DriverMode == PCM_DRIVER_PCM8A) {
			AdpcmRate = ADPCMRATEADDTBL_PCM8A[m];
			PcmKind = PCM8A_PCMKIND[m];
			Pcm16VolumeShift = PCM8A_PCM16VOLSHIFT[m];
		} else {
			AdpcmRate = ADPCMRATEADDTBL[m];
			PcmKind = PCM8PP_PCMKIND[m];
			Pcm16VolumeShift = PCM8PP_PCM16VOLSHIFT[m];
		}
		// 可变频率模式检测：AdpcmRate==0 表示 Variable 模式码
		if (AdpcmRate == 0) {
			if (!VariableMode) {
				// 首次进入 Variable 模式：初始化状态
				VariableMode = 1;
				VariableHasBase = 0;
				VariableBaseAddr = NULL;
				VariableBaseLen = 0;
			}
			// 为 Variable 模式设置安全默认（会被 SetVariableFreq 覆盖）
			AdpcmRate = VariableBaseRate;
		} else {
			// 固定频率模式：记录为潜在的 VariableBaseRate
			VariableBaseRate = AdpcmRate;
			// 注意：不在此处清除 VariableMode/VariableHasBase
			// Variable 模式一旦通过 $ED 命令 (mode $07) 激活，
			// 就保持到下一次显式的 $ED 非 Variable 模式设定
			// （通过 key-off / ADPCMMOD_STOP 传入的 mode=0 不应破坏状态）
		}
		Mode = (Mode&0xFFFF00FF)|(m<<8);
#ifdef PCM8_DEBUG
		fprintf(stderr, "[PCM8 SetMode] driver=%s freq_mode=0x%02X -> AdpcmRate=%d PcmKind=%d\n",
			DriverMode == PCM_DRIVER_PCM8A ? "PCM8A" : "PCM8PP",
			m, AdpcmRate, PcmKind);
#endif
	}
	m = (mode) & 0xFF;
	if (m != 0xFF) {
		m &= 3;
		if (m == 0) {
			AdpcmReg = 0xC7;  // ADPCM 停止
			DmaMtc = 0;
		} else {
			Mode = (Mode&0xFFFFFF00)|(m);
		}
	}
	return 0;
}


int Pcm8::GetRest() {
	if (DmaMtc == 0) {
		return 0;
	}
	if (DmaOcr & 0x08) {  // チェイニング動作
		if (!(DmaOcr & 0x04)) {  // アレイチェイン
			return -1;
		} else {  // リンクアレイチェイン
			return -2;
		}
	}
	return DmaMtc;
}


int Pcm8::GetMode() {
	return Mode;
}


void Pcm8::SetDriverMode(int mode) {
	DriverMode = mode;
#ifdef PCM8_DEBUG
	fprintf(stderr, "[PCM8] 驱动模式切换: %s\n",
		mode == PCM_DRIVER_PCM8A ? "PCM8A" : "PCM8PP");
#endif
}

int Pcm8::GetDriverMode() {
	return DriverMode;
}


void Pcm8::SoftStop() {
	// 停止当前播放，保留 Variable 模式状态
	AdpcmReg = 0xC7;  // ADPCM 停止
	DmaMtc = 0;
	// 重置解码器状态（Scale/Pcm/N1Data）但不触碰 Variable* 字段
	Scale = 0;
	Pcm = 0;
	Pcm16Prev = 0;
	InpPcm = InpPcm_prev = OutPcm = 0;
	OutInpPcm = OutInpPcm_prev = 0;
	N1Data = 0;
	N1DataFlag = 0;
}


void Pcm8::SetVariableFreq(int note) {
	// 可变频率计算：根据音符偏移算回放频率
	// rate = base_rate × 2^((note - base_note) / 12)
	if (VariableBaseRate <= 0) return;
	int diff = note - VariableBaseNote;
	if (diff == 0) {
		AdpcmRate = VariableBaseRate;
	} else {
		AdpcmRate = (int)(VariableBaseRate * pow(2.0, diff / 12.0));
	}
	if (AdpcmRate <= 0) AdpcmRate = 1;  // 防止零频率死循环
#ifdef PCM8_DEBUG
	fprintf(stderr, "[PCM8 Variable] note=%d base=%d diff=%d rate=%d->%d\n",
		note, VariableBaseNote, diff, VariableBaseRate, AdpcmRate);
#endif
}

/// 从 PDX 元数据设置可变频率基准采样率
/// rate_hz: 原始 WAV 采样率 (Hz)，例如 12000
void Pcm8::SetVariableBaseRate(int rate_hz) {
	if (rate_hz > 0) {
		VariableBaseRate = rate_hz * 12;  // 转换到内部格式 Hz×12
	}
}


}

// [EOF]
