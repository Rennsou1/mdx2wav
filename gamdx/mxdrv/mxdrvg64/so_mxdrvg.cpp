// MXDRVg-64 编译单元入口
// 编译 GORRY MXDRVg 核心（仅 64-bit 安全修正，无行为变更）
// 通过预处理器宏将导出函数重命名为 mxdrvg_* 前缀，
// 以便与 MXDRVp 核心在同一二进制中共存。

// 跳过 mxdrvg.h 中的函数声明（在 core.h 中已定义）
#define __MXDRVG_LOADMODULE

// 先 include mxdrvg.h 获取类型定义，然后清除其便捷宏
#include "../mxdrvg.h"
#undef MXDRVG_Call
#undef MXDRVG_Call_2
#undef MXDRVG_Replay
#undef MXDRVG_Stop
#undef MXDRVG_Pause
#undef MXDRVG_Cont
#undef MXDRVG_Fadeout
#undef MXDRVG_Fadeout2

// ── 宏前缀重命名：避免与 MXDRVp 的导出符号冲突 ──
// 注意: 只重命名函数名，不重命名类型名 (MXDRVG_WORK_CH 等)
#define MXDRVG_SetEmulationType mxdrvg_SetEmulationType
#define MXDRVG_Start            mxdrvg_Start
#define MXDRVG_End              mxdrvg_End
#define MXDRVG_GetPCM           mxdrvg_GetPCM
#define MXDRVG_TotalVolume      mxdrvg_TotalVolume
#define MXDRVG_GetTotalVolume   mxdrvg_GetTotalVolume
#define MXDRVG_ChannelMask      mxdrvg_ChannelMask
#define MXDRVG_GetChannelMask   mxdrvg_GetChannelMask
#define MXDRVG_SetData          mxdrvg_SetData
#define MXDRVG_GetWork          mxdrvg_GetWork
#define MXDRVG_MeasurePlayTime  mxdrvg_MeasurePlayTime
#define MXDRVG_PlayAt           mxdrvg_PlayAt
#define MXDRVG_GetPlayAt        mxdrvg_GetPlayAt
#define MXDRVG_GetTerminated    mxdrvg_GetTerminated
// 注意: MXDRVG() 主调度函数也需要重命名, 但它只在核心内部调用
// 所以重命名不影响外部 API
#define MXDRVG                  mxdrvg_Dispatch
#define MXDRVG_Play             mxdrvg_Play
#define MXDRVG_GetLoopCount     mxdrvg_GetLoopCount

// ── 内部使用的 MXDRVG_MeasurePlayTime_OPMINT 也需要避免冲突 ──
#define MXDRVG_MeasurePlayTime_OPMINT  mxdrvg_MeasurePlayTime_OPMINT
#define MXDRVG_CALLBACK_OPMINT         mxdrvg_CALLBACK_OPMINT

#define MXDRVG_EXPORT extern "C"
#define MXDRVG_CALLBACK

// ── 前向声明：便捷宏需要在核心头文件中展开时引用 dispatch 函数 ──
extern "C" void mxdrvg_Dispatch(X68REG *reg);

// ── 便捷宏重定义：使用重命名后的 dispatch 函数 ──
// 核心头文件 mxdrvg_64_core.h 中直接调用 MXDRVG_Fadeout() / MXDRVG_Stop()，
// 原版 mxdrvg.h 中这些是展开为 MXDRVG_Call 的宏。
// 这里必须重建这些宏，使其通过重命名后的 mxdrvg_Dispatch 进行调度。
#define MXDRVG_Call( a ) \
{ \
	X68REG reg; \
	reg.d0 = (a); \
	reg.d1 = 0x00; \
	mxdrvg_Dispatch( &reg ); \
}

#define MXDRVG_Call_2( a, b ) \
{ \
	X68REG reg; \
	reg.d0 = (a); \
	reg.d1 = (b); \
	mxdrvg_Dispatch( &reg ); \
}

#define MXDRVG_Replay()      MXDRVG_Call( 0x0f )
#define MXDRVG_Stop()        MXDRVG_Call( 0x05 )
#define MXDRVG_Pause()       MXDRVG_Call( 0x06 )
#define MXDRVG_Continue()    MXDRVG_Call( 0x07 )
#define MXDRVG_Fadeout()     MXDRVG_Call_2( 0x0c, 19 )
#define MXDRVG_Fadeout2(a)   MXDRVG_Call_2( 0x0c, (a) )

volatile unsigned char OpmReg1B;  // OPM レジスタ $1B の内容

#include "mxdrvg_64_core.h"

// ── 可链接包装函数 ──
// mxdrv_api.h 将 mxdrvg_Stop / mxdrvg_Fadeout 等声明为 extern "C" 函数，
// 供 mdx2wav.cpp 的函数指针分发表使用。
// 核心头文件内部只通过宏展开调用，这里需要提供实际的函数定义。
#undef MXDRVG_Stop
#undef MXDRVG_Pause
#undef MXDRVG_Continue
#undef MXDRVG_Fadeout
#undef MXDRVG_Replay

extern "C" void mxdrvg_Stop(void) {
	X68REG reg;
	reg.d0 = 0x05;
	reg.d1 = 0x00;
	mxdrvg_Dispatch(&reg);
}

extern "C" void mxdrvg_Pause(void) {
	X68REG reg;
	reg.d0 = 0x06;
	reg.d1 = 0x00;
	mxdrvg_Dispatch(&reg);
}

extern "C" void mxdrvg_Continue(void) {
	X68REG reg;
	reg.d0 = 0x07;
	reg.d1 = 0x00;
	mxdrvg_Dispatch(&reg);
}

extern "C" void mxdrvg_Fadeout(void) {
	X68REG reg;
	reg.d0 = 0x0c;
	reg.d1 = 19;
	mxdrvg_Dispatch(&reg);
}
