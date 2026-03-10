// mxdrv_api.h — MXDRV 双核心统一 API 声明
// MXDRVg-64（GORRY + 64-bit 安全）和 MXDRVp
// 两个核心编译为独立编译单元，通过不同前缀共存于同一二进制

#ifndef MXDRV_API_H
#define MXDRV_API_H

#include "mxdrvg.h"  // MXDRVG_WORK_CH, MXDRVG_WORK_GLOBAL 等类型定义

// ═══════════════════════════════════════════
// MXDRVg-64 API (仅 64-bit 安全修正)
// ═══════════════════════════════════════════
extern "C" {
void mxdrvg_SetEmulationType(int ym2151type);
int  mxdrvg_Start(int samprate, int filtermode, int mdxbufsize, int pdxbufsize);
void mxdrvg_End(void);
int  mxdrvg_GetPCM(SWORD *buf, int len);
void mxdrvg_TotalVolume(int vol);
int  mxdrvg_GetTotalVolume(void);
void mxdrvg_ChannelMask(int mask);
int  mxdrvg_GetChannelMask(void);
void mxdrvg_SetData(void *mdx, ULONG mdxsize, void *pdx, ULONG pdxsize);
void volatile *mxdrvg_GetWork(int i);
ULONG mxdrvg_MeasurePlayTime(int loop, int fadeout);
void mxdrvg_PlayAt(ULONG playat, int loop, int fadeout);
ULONG mxdrvg_GetPlayAt(void);
int  mxdrvg_GetTerminated(void);
void mxdrvg_Play(void);
void mxdrvg_Stop(void);
void mxdrvg_Pause(void);
void mxdrvg_Continue(void);
void mxdrvg_Fadeout(void);
int  mxdrvg_GetLoopCount(void);
}

// ═══════════════════════════════════════════
// MXDRVp API (bug 修复 + Variable Mode)
// ═══════════════════════════════════════════
extern "C" {
void mxdrvp_SetEmulationType(int ym2151type);
int  mxdrvp_Start(int samprate, int filtermode, int mdxbufsize, int pdxbufsize);
void mxdrvp_End(void);
int  mxdrvp_GetPCM(SWORD *buf, int len);
void mxdrvp_TotalVolume(int vol);
int  mxdrvp_GetTotalVolume(void);
void mxdrvp_ChannelMask(int mask);
int  mxdrvp_GetChannelMask(void);
void mxdrvp_SetData(void *mdx, ULONG mdxsize, void *pdx, ULONG pdxsize);
void volatile *mxdrvp_GetWork(int i);
ULONG mxdrvp_MeasurePlayTime(int loop, int fadeout);
void mxdrvp_PlayAt(ULONG playat, int loop, int fadeout);
ULONG mxdrvp_GetPlayAt(void);
int  mxdrvp_GetTerminated(void);
void mxdrvp_Play(void);
void mxdrvp_Stop(void);
void mxdrvp_Pause(void);
void mxdrvp_Continue(void);
void mxdrvp_Fadeout(void);
int  mxdrvp_GetLoopCount(void);

// MXDRVp 专有 API
void mxdrvp_SetPCM8Volume(int db);
void mxdrvp_SetPCM8VariableBaseRate(int rate_hz);
int  mxdrvp_HasVariableMode(void);
}

// ═══════════════════════════════════════════
// 核心模式枚举
// ═══════════════════════════════════════════
enum MXDRVCoreMode {
    MXDRV_CORE_MXDRVG = 0,  // GORRY MXDRVg (实机兼容)
    MXDRV_CORE_MXDRVP = 1,  // MXDRVp (默认)
};

#endif // MXDRV_API_H
