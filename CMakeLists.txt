
cmake_minimum_required(VERSION 3.5)

project(mdx2wav)

set(CMAKE_C_FLAGS_RELEASE "-Wall -O3")
set(CMAKE_CXX_FLAGS_RELEASE "-Wall -O3")
set(CMAKE_C_FLAGS_DEBUG "-Wall -g")
set(CMAKE_CXX_FLAGS_DEBUG "-Wall -g -DPCM8_DEBUG")

add_executable(mdx2wav
	src/mdx2wav.cpp
	gamdx/fmgen/fmgen.cpp
	gamdx/fmgen/fmtimer.cpp
	gamdx/fmgen/opm.cpp
	gamdx/mxdrvg/so.cpp
	gamdx/mxdrvg/opm_delegate.cpp
	gamdx/mxdrvg/opm_ymfm.cpp
	gamdx/pcm8/pcm8.cpp
	gamdx/pcm8/x68pcm8.cpp
	gamdx/ymfm/ymfm_opm.cpp
)

