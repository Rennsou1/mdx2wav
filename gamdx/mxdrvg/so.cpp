// mxdrvg_core.h 编译单元入口
// mxdrvg_core.h 是从 X68K 汇编直接移植的 #include 式编译单元（7580 行），
// 全部使用 static 函数和全局变量。本文件是其唯一编译入口，
// 通过定义 MXDRVG_EXPORT / MXDRVG_CALLBACK 宏后 #include 使之编译。

#define MXDRVG_EXPORT
#define MXDRVG_CALLBACK

volatile unsigned char OpmReg1B;  // OPM レジスタ $1B の内容

#include "mxdrvg_core.h"
