// X68PCM8 — 8-channel PCM mixer implementation
// Modified by Rennsou1_2006 (2026):
//   SetDriverMode forwarding, SoftStop implementation

#include "x68pcm8.h"

namespace X68K
{


// ---------------------------------------------------------------------------
//	構築
//
X68PCM8::X68PCM8()
{
}

// ---------------------------------------------------------------------------
//	初期化
//
bool X68PCM8::Init(uint rate)
{
	mMask = 0;
	mVolume = 256;
	for (int i=0; i<PCM8_NCH; ++i) {
		mPcm8[i].Init();
	}

	OutInpAdpcm[0] = OutInpAdpcm[1] =
	  OutInpAdpcm_prev[0] = OutInpAdpcm_prev[1] =
	  OutInpAdpcm_prev2[0] = OutInpAdpcm_prev2[1] =
	  OutOutAdpcm[0] = OutOutAdpcm[1] =
	  OutOutAdpcm_prev[0] = OutOutAdpcm_prev[1] =
	  OutOutAdpcm_prev2[0] = OutOutAdpcm_prev2[1] =
	  0;
	OutInpOutAdpcm[0] = OutInpOutAdpcm[1] =
	  OutInpOutAdpcm_prev[0] = OutInpOutAdpcm_prev[1] =
	  OutInpOutAdpcm_prev2[0] = OutInpOutAdpcm_prev2[1] =
	  OutOutInpAdpcm[0] = OutOutInpAdpcm[1] =
	  OutOutInpAdpcm_prev[0] = OutOutInpAdpcm_prev[1] =
	  0;

	SetRate(rate);

	return true;
}

// ---------------------------------------------------------------------------
//	サンプルレート設定
//
bool X68PCM8::SetRate(uint rate)
{
	mSampleRate = rate;

	return true;
}

// ---------------------------------------------------------------------------
//	リセット
//
void X68PCM8::Reset()
{
	Init(mSampleRate);
}

// ---------------------------------------------------------------------------
//	パラメータセット
//
int X68PCM8::Out(int ch, void *adrs, int mode, int len)
{
	return mPcm8[ch & (PCM8_NCH-1)].Out(adrs, mode, len);
}

// ---------------------------------------------------------------------------
//	アボート
//
void X68PCM8::Abort()
{
	Reset();
}

// ---------------------------------------------------------------------------
//	チャンネルマスクの設定
//
void X68PCM8::SetChannelMask(uint mask)
{
	mMask = mask;
}

// ---------------------------------------------------------------------------
//	音量設定
//
void X68PCM8::SetVolume(int db)
{
	db = Min(db, 20);
	if (db > -192)
		mVolume = int(16384.0 * pow(10, db / 40.0));
	else
		mVolume = 0;
}


// ---------------------------------------------------------------------------
//	62500Hz用ADPCM合成処理
//	X68000 実機: YM2151 と MSM6258 は 1:1 等比混音（MAME add_route 両方 0.50）
//
inline void X68PCM8::pcmset62500(Sample* buffer, int ndata) {
	Sample* limit = buffer + ndata * 2;
	for (Sample* dest = buffer; dest < limit; dest+=2) {
		OutInpAdpcm[0] = OutInpAdpcm[1] = 0;

		for (int ch=0; ch<PCM8_NCH; ++ch) {
			int pan = mPcm8[ch].GetMode();
			int o = mPcm8[ch].GetPcm62();
			if (o != (int)0x80000000) {
#ifdef PCM8_DEBUG
				// 诊断: 前20个采样中每通道的详细值
				{
					static int pcm_diag_count = 0;
					if (pcm_diag_count < 20 && (dest == buffer)) {
						fprintf(stderr, "[MIX ch%d] GetPcm62=%d pan=0x%X\n", ch, o, pan);
					}
				}
#endif
				OutInpAdpcm[0] += (-(pan&1)) & o;
				OutInpAdpcm[1] += (-((pan>>1)&1)) & o;
			}
		}

#ifdef PCM8_DEBUG
		// 诊断: 混音后总值
		{
			static int mix_total_diag = 0;
			if (mix_total_diag < 20 && (dest == buffer)) {
				fprintf(stderr, "[MIX TOTAL] L=%d R=%d mVolume=%d\n",
					OutInpAdpcm[0], OutInpAdpcm[1], mVolume);
				mix_total_diag++;
			}
		}
#endif

		// mVolume=256, >> 9 で OPM とのバランス調整（実機ヒアリング結果×0.5）
		OutInpAdpcm[0] = (OutInpAdpcm[0] * mVolume) >> 9;
		OutInpAdpcm[1] = (OutInpAdpcm[1] * mVolume) >> 9;

		// クランプ
		OutInpAdpcm[0] = Limit(OutInpAdpcm[0], 0x7fff, -0x8000);
		OutInpAdpcm[1] = Limit(OutInpAdpcm[1], 0x7fff, -0x8000);

		StoreSample(dest[0], OutInpAdpcm[0]);
		StoreSample(dest[1], OutInpAdpcm[1]);
	}
}


// ---------------------------------------------------------------------------
//	22050Hz用ADPCM合成処理
//
inline void X68PCM8::pcmset22050(Sample* buffer, int ndata) {
	Sample* limit = buffer + ndata * 2;
	for (Sample* dest = buffer; dest < limit; dest+=2) {

		static int rate=0,rate2=0;
		rate2 -= 15625;
		if (rate2 < 0) {
			rate2 += 22050;
			OutInpAdpcm[0] = OutInpAdpcm[1] = 0;

			for (int ch=0; ch<PCM8_NCH; ++ch) {
				int pan = mPcm8[ch].GetMode();
				int o = mPcm8[ch].GetPcm22();
				if (o != (int)0x80000000) {
					OutInpAdpcm[0] += (-(pan&1)) & o;
					OutInpAdpcm[1] += (-((pan>>1)&1)) & o;
				}
			}

			// mVolume=256, >> 9 で OPM とのバランス調整（実機ヒアリング結果×0.5）
			OutInpAdpcm[0] = (OutInpAdpcm[0] * mVolume) >> 9;
			OutInpAdpcm[1] = (OutInpAdpcm[1] * mVolume) >> 9;

			// クランプ
			OutInpAdpcm[0] = Limit(OutInpAdpcm[0], 0x7fff, -0x8000);
			OutInpAdpcm[1] = Limit(OutInpAdpcm[1], 0x7fff, -0x8000);
		}

		StoreSample(dest[0], OutInpAdpcm[0]);
		StoreSample(dest[1], OutInpAdpcm[1]);
	}
}


// ---------------------------------------------------------------------------
//	合成 (stereo)
//
void X68PCM8::Mix(Sample* buffer, int nsamples)
{
	if (mSampleRate == 22050) {
		pcmset22050(buffer, nsamples);
	} else {
		pcmset62500(buffer, nsamples);
	}
}

// ---------------------------------------------------------------------------
// 	驱动模式设置（转发到所有通道）
//
void X68PCM8::SetDriverMode(int mode)
{
	for (int i=0; i<PCM8_NCH; ++i) {
		mPcm8[i].SetDriverMode(mode);
	}
}

// ---------------------------------------------------------------------------
//	ソフト停止（Variable モード状態保持）
//
void X68PCM8::SoftStop()
{
	mPcm8[0].SoftStop();
}

// ---------------------------------------------------------------------------
}  // namespace X68K

// [EOF]
