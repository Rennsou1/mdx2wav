// X68PCM8 — 8-channel PCM mixer for X68000 emulation
// Modified by Rennsou1_2006 (2026):
//   SetDriverMode forwarding, SoftStop (Variable mode state preservation),
//   SetVariableBaseRate/IsVariableMode/IsVariableRedirectReady APIs

#pragma once

#include <cstdio>
#include <cstdlib>
#include <cmath>
#include <cstring>
#include <cassert>

#include "../types.h"
#include "global.h"
#include "pcm8.h"

namespace X68K
{
	typedef MXDRVG_SAMPLETYPE Sample;
	typedef int32 ISample;

	class X68PCM8
	{
	public:
		X68PCM8();
		~X68PCM8() {}

		bool Init(uint rate);
		bool SetRate(uint rate);
		void Reset();

		int Out(int ch, void *adrs, int mode, int len);
		void Abort();

		void Mix(Sample *buffer, int nsamples);
		void SetVolume(int db);
		void SetChannelMask(uint mask);
		void SetDriverMode(int mode);  // 设置所有通道的 PCM 驱动模式
		void SoftStop();  // 停止 ch0 播放但保留 Variable 模式状态
		void SetVariableBaseRate(int rate_hz) { mPcm8[0].SetVariableBaseRate(rate_hz); }  // 从 PDX 元数据设置可变频率基准采样率（代理到 ch0）
		bool IsVariableMode() const { return mPcm8[0].IsVariableMode(); }  // 查询 ch0 是否处于可变频率模式
		bool IsVariableRedirectReady() const { return mPcm8[0].IsVariableRedirectReady(); }  // 可变频率重定向就绪查询（代理到 ch0）

	private:
		Pcm8 mPcm8[PCM8_NCH];
		uint mMask;
		int mVolume;
		int mSampleRate;

		sint32 OutInpAdpcm[2];
		sint32 OutInpAdpcm_prev[2];
		sint32 OutInpAdpcm_prev2[2];
		sint32 OutOutAdpcm[2];
		sint32 OutOutAdpcm_prev[2];
		sint32 OutOutAdpcm_prev2[2];  // 高音フィルター２用バッファ

		sint32 OutInpOutAdpcm[2];
		sint32 OutInpOutAdpcm_prev[2];
		sint32 OutInpOutAdpcm_prev2[2];
		sint32 OutOutInpAdpcm[2];
		sint32 OutOutInpAdpcm_prev[2];  // 高音フィルター３用バッファ

		inline void pcmset62500(Sample* buffer, int ndata);
		inline void pcmset22050(Sample* buffer, int ndata);

	};

	inline int Max(int x, int y) { return (x > y) ? x : y; }
	inline int Min(int x, int y) { return (x < y) ? x : y; }
	inline int Abs(int x) { return x >= 0 ? x : -x; }

	inline int Limit(int v, int max, int min) 
	{ 
		return v > max ? max : (v < min ? min : v); 
	}

	inline void StoreSample(Sample& dest, ISample data)
	{
		if (sizeof(Sample) == 2)
			dest = (Sample) Limit(dest + data, 0x7fff, -0x8000);
		else
			dest += data;
	}

}

// [EOF]
