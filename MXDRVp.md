# 关于 MXDRVp

MXDRVp 是在 MXDRV 基础上开发的 PCM8PP 扩展驱动，为 Mercury Unit 增加了 **可变频率回放（Variable Frequency PCM）** 能力。该驱动由 **Rennsou1_2006** 设计并实现。

## 前言

Sharp X68000 的 **MXDRV** 是 X68k 平台上最为广泛使用的 FM 音乐驱动程序之一。随后社区推出的 **Mercury Unit (M-Unit)** PCM 扩展驱动——**PCM8++（PCM8PP）**（v0.83d, たにぃ 1994-1996）——将 PCM 通道从原版 MXDRV 的 1 通道 MSM6258 ADPCM 扩展至 8 通道软件混音复音，并引入了多种 PCM 数据格式（ADPCM / 16-bit PCM / 8-bit PCM）与多档采样率支持。

然而，PCM8PP 规范中定义的部分高级模式码（如 `$07` Through 模式与 `$28` ADPCM 0 Hz 模式）在原版 MXDRV/MXDRVm 驱动中并未被实际使用。这些模式码在 PCM8PP 采样率表中对应的回放频率为 **0 Hz**，原意为特殊控制用途或预留位，不承载常规的固定频率 PCM 回放语义。

**MXDRVp** 正是为弥补这一空白而设计的扩展驱动。它利用 PCM8PP 规范中上述"频率为零"的模式码，实现了 **可变频率回放（Variable Frequency PCM）** ——使单个 PCM 样本能够以不同音高回放，从而实现PCM通道的音高控制能力。

## 原版 MXDRV 中未被使用的 PCM8PP 模式码

PCM8PP v0.83d 的技术规范定义了 64 个模式码（`$00`–`$3F`），通过 MML 的 `$ED` 命令设置 PCM 通道的数据格式与采样率。其中：

- **`$07`（Through 模式）**：PCM8PP 采样率表中为 `0 Hz`（16-bit PCM 类型）
- **`$28`**：PCM8PP 采样率表中为 `0 Hz`（ADPCM 类型）

这两个模式码在 MXDRV 中从未被使用。它们在 PCM8PP 的采样率映射表（`ADPCMRATEADDTBL`）中对应频率值为零，不执行常规回放。

> **注意**：模式码 `$07` 的含义因 PCM 驱动而异。在 **PCM8A** 驱动（philly, 1993-1997）中，`$07` 被定义为 **ADPCM 20.8 kHz** 回放模式，是一个有效的固定频率模式，不具备 Variable 语义。因此，MXDRVp 的 Variable 模式仅在 **PCM8PP 驱动** 环境下生效。

## 技术原理

MXDRVp 通过 **单样本多音高回放** 解决了传统 MXDRVm 中"每个音高需要一个独立 PDX 样本槽位"的限制：

1.  **Variable 模式激活**：`$ED $07`（Through）或 `$ED $28` 将 PCM 通道的内部回放频率设为 0，触发 Variable 模式（`AdpcmRate = 0`）
2.  **基准样本捕获**：该通道首次触发的非空 PDX 槽位被自动记录为基准样本（base），包括数据地址、数据长度与音符编号
3.  **音高重计算**：后续音符不再从 PDX 槽位读取新数据，而是复用基准样本，依据十二平均律计算新的回放采样率：
    ```
    回放频率 = 基准频率 × 2^((当前音符 - 基准音符) / 12)
    ```
4.  **空槽位透明重定向**：当 MDX 触发一个空的 PDX 槽位时，Variable 模式自动将其重定向至基准样本数据，避免静音

## 元数据支持

当 PDX 文件由 `wav2pdx` 工具生成时，会在文件尾部附加 `PDXr` 元数据块，记录原始 WAV 的采样率信息。mdx2wav 在加载 PDX 后解析此元数据，将精确的基准采样率（如 12000 Hz、22050 Hz 等）传递给 Variable 模式引擎，确保音高计算的准确性。

若 PDX 文件不包含 `PDXr` 元数据，则默认以 15625 Hz 作为基准采样率。

## 自动识别

mdx2wav 通过预扫描 MDX 数据中的 `$ED` 命令自动判定驱动类型：

-   通道数 > 9（16 通道 = Mercury Unit 扩展）→ MXDRVm 或 MXDRVp
-   检测到 Variable 模式码（`$ED $07` 或 `$ED $28`）→ MXDRVp
-   `$ED $07` 仅在 PCM8PP 驱动下被视为 Variable（PCM8A 驱动下 `$07` 是 ADPCM 20.8 kHz）

## X68000 实机可行性

MXDRVp 的 Variable 模式在技术层面是一项 **纯软件扩展**，不依赖任何额外硬件，也不修改 Mercury Unit 的硬件接口。理论上，只要 X68000 实机配备 Mercury Unit 并运行 PCM8PP 驱动，MXDRVp 的数据格式即可被解析。

然而在实际运行中，Variable 模式需要在每次音符触发时实时计算十二平均律频率（涉及浮点指数运算或高精度定点近似），且需维护基准样本状态的上下文切换。X68000 的 MC68000 CPU（10 MHz）在同时处理 8 通道 FM 合成中断、PCM 混音与 Variable 频率重计算时，可能面临严重的 CPU 时间预算不足。

因此，MXDRVp 的 Variable 模式 **主要面向离线渲染场景**（如本项目 mdx2wav 的用途），而非 X68000 实机实时回放。在现代 PC 上通过模拟器回放则不受此限制。