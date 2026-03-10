// MXDRVp 编译单元入口
// 编译 MXDRVp 核心（Variable Mode + MXDRV bug 修复）
// 通过预处理器宏将导出函数重命名为 mxdrvp_* 前缀，
// 以便与 MXDRVg 核心在同一二进制中共存。

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

// ── 宏前缀重命名：避免与 MXDRVg 的导出符号冲突 ──
#define MXDRVG_SetEmulationType mxdrvp_SetEmulationType
#define MXDRVG_Start            mxdrvp_Start
#define MXDRVG_End              mxdrvp_End
#define MXDRVG_GetPCM           mxdrvp_GetPCM
#define MXDRVG_TotalVolume      mxdrvp_TotalVolume
#define MXDRVG_GetTotalVolume   mxdrvp_GetTotalVolume
#define MXDRVG_ChannelMask      mxdrvp_ChannelMask
#define MXDRVG_GetChannelMask   mxdrvp_GetChannelMask
#define MXDRVG_SetData          mxdrvp_SetData
#define MXDRVG_GetWork          mxdrvp_GetWork
#define MXDRVG_MeasurePlayTime  mxdrvp_MeasurePlayTime
#define MXDRVG_PlayAt           mxdrvp_PlayAt
#define MXDRVG_GetPlayAt        mxdrvp_GetPlayAt
#define MXDRVG_GetTerminated    mxdrvp_GetTerminated
#define MXDRVG                  mxdrvp_Dispatch
#define MXDRVG_Play             mxdrvp_Play
#define MXDRVG_GetLoopCount     mxdrvp_GetLoopCount

// ── MXDRVp 专有 API ──
#define MXDRVG_SetPCM8Volume          mxdrvp_SetPCM8Volume
#define MXDRVG_SetPCM8VariableBaseRate mxdrvp_SetPCM8VariableBaseRate
#define MXDRVG_HasVariableMode        mxdrvp_HasVariableMode

// ── 内部符号重命名 ──
#define MXDRVG_MeasurePlayTime_OPMINT  mxdrvp_MeasurePlayTime_OPMINT
#define MXDRVG_CALLBACK_OPMINT         mxdrvp_CALLBACK_OPMINT

#define MXDRVG_EXPORT extern "C"
#define MXDRVG_CALLBACK

// ── 前向声明：便捷宏需要在核心头文件中展开时引用 dispatch 函数 ──
extern "C" void mxdrvp_Dispatch(X68REG *reg);

// ── 便捷宏重定义：使用重命名后的 dispatch 函数 ──
// 核心头文件 mxdrvp_core.h 中直接调用 MXDRVG_Fadeout() / MXDRVG_Stop()，
// 原版 mxdrvg.h 中这些是展开为 MXDRVG_Call 的宏。
// 这里必须重建这些宏，使其通过重命名后的 mxdrvp_Dispatch 进行调度。
#define MXDRVG_Call( a ) \
{ \
	X68REG reg; \
	reg.d0 = (a); \
	reg.d1 = 0x00; \
	mxdrvp_Dispatch( &reg ); \
}

#define MXDRVG_Call_2( a, b ) \
{ \
	X68REG reg; \
	reg.d0 = (a); \
	reg.d1 = (b); \
	mxdrvp_Dispatch( &reg ); \
}

#define MXDRVG_Replay()      MXDRVG_Call( 0x0f )
#define MXDRVG_Stop()        MXDRVG_Call( 0x05 )
#define MXDRVG_Pause()       MXDRVG_Call( 0x06 )
#define MXDRVG_Continue()    MXDRVG_Call( 0x07 )
#define MXDRVG_Fadeout()     MXDRVG_Call_2( 0x0c, 19 )
#define MXDRVG_Fadeout2(a)   MXDRVG_Call_2( 0x0c, (a) )

// 注意: OpmReg1B 已在 so_mxdrvg.cpp 中定义，这里使用 extern
extern volatile unsigned char OpmReg1B;

#include "mxdrvp_core.h"

// ── 可链接包装函数 ──
// mxdrv_api.h 将 mxdrvp_Stop / mxdrvp_Fadeout 等声明为 extern "C" 函数，
// 供 mdx2wav.cpp 的函数指针分发表使用。
// 核心头文件内部只通过宏展开调用，这里需要提供实际的函数定义。
#undef MXDRVG_Stop
#undef MXDRVG_Pause
#undef MXDRVG_Continue
#undef MXDRVG_Fadeout
#undef MXDRVG_Replay

extern "C" void mxdrvp_Stop(void) {
	X68REG reg;
	reg.d0 = 0x05;
	reg.d1 = 0x00;
	mxdrvp_Dispatch(&reg);
}

extern "C" void mxdrvp_Pause(void) {
	X68REG reg;
	reg.d0 = 0x06;
	reg.d1 = 0x00;
	mxdrvp_Dispatch(&reg);
}

extern "C" void mxdrvp_Continue(void) {
	X68REG reg;
	reg.d0 = 0x07;
	reg.d1 = 0x00;
	mxdrvp_Dispatch(&reg);
}

extern "C" void mxdrvp_Fadeout(void) {
	X68REG reg;
	reg.d0 = 0x0c;
	reg.d1 = 19;
	mxdrvp_Dispatch(&reg);
}
