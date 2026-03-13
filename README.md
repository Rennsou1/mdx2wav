# mdx2wav

Sharp X68000 MDX 音乐格式渲染器，将 MDX/PDX 文件直接转换为 WAV 文件（62500 Hz、16-bit stereo）。
基于 **MXDRVg V1.50a** © 2000 GORRY，从 X68k MXDRV music driver version 2.06+17 Rel.X5-S 移植而来。

## 驱动类型

mdx2wav 自动识别三种 MXDRV 驱动变体：

| 驱动 | 通道数 | PCM 方式 | 说明 |
|------|--------|---------|------|
| **MXDRV** | 9 (8 FM + 1 PCM) | MSM6258 ADPCM | 原版驱动，单通道固定频率 ADPCM |
| **MXDRVm** | 16 (8 FM + 8 PCM) | PCM8 / PCM8PP | Mercury Unit 软件混音，8 通道 PCM 复音 |
| **MXDRVp** | 16 (8 FM + 8 PCM) | PCM8PP + Variable | 扩展驱动（Rennsou1_2006），可变频率 PCM 回放 |

PCM 驱动类型（PCM8PP 或 PCM8A）通过预扫描 MDX 中的 `$ED` 模式码自动检测。

---

## 关于 MXDRVp

MXDRVp 是在 MXDRV 基础上开发的 PCM8PP 扩展驱动，为 Mercury Unit 增加了 **可变频率回放（Variable Frequency PCM）** 能力。该驱动由 **Rennsou1_2006** 设计并实现。

详细的技术文档请参阅 **[MXDRVp](MXDRVp.md)**。

## 用法

### 拖入即用

将 MDX 文件拖到 `mdx2wav.exe` 上，自动输出同名 `.wav` 文件。

### 命令行

```shell
# 默认输出 WAV
mdx2wav song.mdx                          # -> song.wav
mdx2wav -o output.wav song.mdx            # 指定输出文件名
```

### Raw PCM 模式（向后兼容）

```shell
# -r: 输出原始 PCM 到 stdout，配合 ffmpeg 使用
mdx2wav -r song.mdx | ffmpeg -f s16le -ar 62500 -ac 2 -i - song.ogg

# Linux 实时播放
mdx2wav -r song.mdx | aplay -f S16_LE -r 62500 -c 2
```

### 提取元数据

```shell
mdx2wav -i song.mdx
```

输出格式：

```json
{
  "title": ,
  "pdx_filename":,
  "use_driver":,
  "channels":,
  "timer_b":,
  "tempo_bpm":,
  "duration_ms":,
  "total_clock":,
  "format":
}
```

### 命令行选项

```
  -o <file>  指定输出 WAV 文件名
  -r         Raw 模式，输出 16bit stereo PCM 到 stdout（无 WAV 头）
  -d <sec>   限制最大播放时长，0 = 无限制（默认: 300）
  -e <type>  YM2151 模拟引擎: nuked 或 ymfm（默认: ymfm）
  -f         启用淡出（默认: 开启）
  -i         输出 JSON 格式元数据到 stdout
  -l <loop>  循环次数限制（默认: 2）
  -m         测量播放时长（秒）
  -v         显示版本信息
  -V         详细模式，输出调试日志到 stderr
```

## 编译

### 依赖

- CMake ≥ 3.5
- GCC / MinGW（支持 C++11）

### Linux

```shell
git clone https://github.com/mitsuman/mdx2wav.git
cd mdx2wav
mkdir build && cd build
cmake -DCMAKE_BUILD_TYPE=Release ..
make -j$(nproc)
```

### Windows (MSYS2 MinGW)

```shell
# 在 MSYS2 MinGW32 或 MinGW64 Shell 中
cd mdx2wav
mkdir build && cd build
cmake -DCMAKE_BUILD_TYPE=Release -G "MSYS Makefiles" ..
make -j4
```

Debug 模式会启用 `PCM8_DEBUG` 宏，输出 PCM 混音诊断信息：

```shell
cmake -DCMAKE_BUILD_TYPE=Debug ..
```

## 项目结构

```
mdx2wav/
├── src/
│   └── mdx2wav.cpp          # 主程序：MDX/PDX 加载、渲染循环、ReplayGain
├── gamdx/
│   ├── mxdrvg/
│   │   ├── mxdrvg.h          # 公共 API 头文件
│   │   ├── mxdrvg_core.h     # MXDRV 核心（从 X68K 汇编移植）
│   │   ├── opm_delegate.*    # OPM 引擎抽象层
│   │   ├── opm_nuked.*       # Nuked OPM 引擎适配器
│   │   └── opm_ymfm.*        # YMFM 引擎适配器
│   ├── pcm8/
│   │   ├── global.h           # PCM8PP/PCM8A 采样率表、模式码映射
│   │   ├── pcm8.h/cpp         # PCM8 单通道：ADPCM 解码、Variable 频率
│   │   └── x68pcm8.h/cpp      # X68PCM8 混音器：8通道合成、音量控制
│   ├── nuked/                 # Nuked OPM cycle-accurate YM2151 模拟
│   └── ymfm/                  # YMFM YM2151 模拟引擎
└── CMakeLists.txt
```

## 许可证

Apache License 2.0 — 详见 [LICENSE](LICENSE)

## 致谢

- 原始 MXDRV：milk., K.MAEKAWA, Missy.M, Yatsube (1988-92)
- MXDRVg Win32 移植：GORRY (2000-2011)
- mdx2wav：@__mtm (2014)
- MDXWin：PCM 回放参考实现
- Nuked OPM：Nuke.YKT
- YMFM：Aaron Giles

