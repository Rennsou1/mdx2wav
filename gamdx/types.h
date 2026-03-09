#pragma once

// 项目全局类型定义
// 基于 <cstdint> 标准类型，保留遗留别名以兼容 PCM8/MXDRVG 模块

#include <cstdint>

typedef uint8_t   uchar;
typedef uint16_t  ushort;
typedef uint32_t  uint;
typedef uint64_t  ulong;

typedef uint8_t   uint8;
typedef uint16_t  uint16;
typedef uint32_t  uint32;

typedef int8_t    sint8;
typedef int16_t   sint16;
typedef int32_t   sint32;

typedef int8_t    int8;
typedef int16_t   int16;
typedef int32_t   int32;

typedef int16 MXDRVG_SAMPLETYPE;  // PCM 采样类型: int16
