cmake_minimum_required(VERSION 3.15)

project(mdx2wav LANGUAGES C CXX)

# C++17 用于 inline constexpr 等现代特性，C11 用于 Nuked OPM
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)

add_executable(mdx2wav
	src/mdx2wav.cpp
	gamdx/nuked/opm.c
	gamdx/mxdrvg/so.cpp
	gamdx/mxdrvg/opm_delegate.cpp
	gamdx/mxdrvg/opm_nuked.cpp
	gamdx/mxdrvg/opm_ymfm.cpp
	gamdx/pcm8/pcm8.cpp
	gamdx/pcm8/x68pcm8.cpp
	gamdx/ymfm/ymfm_opm.cpp
)

# 项目根目录作为 include 搜索路径（简化 #include 相对路径）
target_include_directories(mdx2wav PRIVATE ${CMAKE_SOURCE_DIR})

# 编译选项：使用 target 级别替代全局变量
target_compile_options(mdx2wav PRIVATE
	$<$<CONFIG:Release>:-Wall -O3>
	$<$<CONFIG:Debug>:-Wall -g>
)

# Debug 模式启用 PCM8 诊断输出
target_compile_definitions(mdx2wav PRIVATE
	$<$<CONFIG:Debug>:PCM8_DEBUG>
)
