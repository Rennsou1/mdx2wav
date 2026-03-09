# mdx2wav

Sharp X68000 MDX 音乐格式离线渲染器，将 MDX/PDX 文件转换为 16-bit stereo 62500 Hz raw PCM 流。
基于 **MXDRVg V1.50a** © 2000 GORRY，从 X68k MXDRV music driver version 2.06+17 Rel.X5-S 移植而来。

## 特性

- **FM 合成**：YM2151 (OPM) 精确模拟，支持 fmgen 和 YMFM 两种引擎
- **PCM 回放**：完整的 PCM8A / PCM8PP 驱动模拟，支持 ADPCM / PCM16 / PCM8 多格式
- **元数据提取**：JSON 格式输出曲目信息（标题、驱动类型、BPM、时长等）
- **ReplayGain**：两遍渲染峰值归一化，自动音量平衡
- **原生采样率**：62500 Hz 输出，保留 X68000 原始音质特征
- **可变频率 PCM**：MXDRVp Variable 模式，单样本多音高回放

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

mdx2wav 将 raw PCM 数据输出到 stdout，需配合其他工具使用。

### 转换为 WAV

```shell
mdx2wav song.mdx | ffmpeg -f s16le -ar 62500 -ac 2 -i - song.wav
```

### 转换为 OGG

```shell
mdx2wav song.mdx | ffmpeg -f s16le -ar 62500 -ac 2 -i - -ab 192k song.ogg
```

### 实时播放 (Linux)

```shell
mdx2wav song.mdx | aplay -f S16_LE -r 62500 -c 2
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
  -d <sec>   限制最大播放时长，0 = 无限制（默认: 300）
  -e <type>  YM2151 模拟引擎: fmgen 或 ymfm（默认: fmgen）
  -f         启用淡出
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
│   │   └── opm_ymfm.*        # YMFM 引擎适配器
│   ├── pcm8/
│   │   ├── global.h           # PCM8PP/PCM8A 采样率表、模式码映射
│   │   ├── pcm8.h/cpp         # PCM8 单通道：ADPCM 解码、Variable 频率
│   │   └── x68pcm8.h/cpp      # X68PCM8 混音器：8通道合成、音量控制
│   ├── fmgen/                 # fmgen YM2151 模拟引擎
│   └── ymfm/                  # YMFM YM2151 模拟引擎
└── CMakeLists.txt
```

## 许可证

Apache License 2.0 — 详见 [LICENSE](LICENSE)

## 致谢

- 原始 MXDRV：milk., K.MAEKAWA, Missy.M, Yatsube (1988-92)
- MXDRVg Win32 移植：GORRY (2000-2011)
- mdx2wav：@__mtm (2014)
- fmgen：cisc
- YMFM：Aaron Giles
