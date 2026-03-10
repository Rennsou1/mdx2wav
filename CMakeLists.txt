cmake_minimum_required(VERSION 3.15)

project(mdx2wav LANGUAGES C CXX)

# C++17 用于 inline constexpr 等现代特性，C11 用于 Nuked OPM
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)

add_executable(mdx2wav
	src/mdx2wav.cpp
	gamdx/nuked/opm.c
	# MXDRV 双核心
	gamdx/mxdrv/mxdrvg64/so_mxdrvg.cpp  # MXDRVg-64
	gamdx/mxdrv/mxdrvp/so_mxdrvp.cpp    # MXDRVp
	# 共享组件
	gamdx/mxdrv/opm_delegate.cpp
	gamdx/mxdrv/opm_nuked.cpp
	gamdx/mxdrv/opm_ymfm.cpp
	gamdx/pcm8/pcm8.cpp
	gamdx/pcm8/x68pcm8.cpp
	gamdx/ymfm/ymfm_opm.cpp
)

# 项目根目录作为 include 搜索路径（简化 #include 相对路径）
target_include_directories(mdx2wav PRIVATE ${CMAKE_SOURCE_DIR})

# 编译选项：使用 target 级别替代全局变量
# -fpermissive: MXDRV 原版代码使用 ULONG (32-bit) 模拟 X68000 D 寄存器，
#   偶尔存储指针值，在 64-bit 下会产生精度丢失警告。
#   这是 GORRY 原版翻译的固有特征，不影运行时正确性（指针只在 A 寄存器间传递）。
target_compile_options(mdx2wav PRIVATE
	-fpermissive
	$<$<CONFIG:Release>:-Wall -O3>
	$<$<CONFIG:Debug>:-Wall -g>
)

# Debug 模式启用 PCM8 诊断输出
target_compile_definitions(mdx2wav PRIVATE
	$<$<CONFIG:Debug>:PCM8_DEBUG>
)

# 静态链接 GCC/C++ 运行时（生成独立 exe，不依赖 libgcc/libstdc++ DLL）
target_link_options(mdx2wav PRIVATE -static -static-libgcc -static-libstdc++)
