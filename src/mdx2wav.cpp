// mdx2wav — MDX to WAV converter
// Original Copyright 2014 mitsuman (@__mtm), Apache License 2.0
// Modified by Rennsou1_2006 (2026):
//   直接 WAV 文件输出（拖入 MDX 即用）、ReplayGain 峰值归一化、
//   JSON 元数据提取、Variable 频率 PCM (MXDRVp)、62500Hz 原生输出
#include <cctype>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <io.h>

#include "../gamdx/mxdrvg/mxdrvg.h"

#define VERSION "2.0"

static bool verbose = false;

static constexpr int MAGIC_OFFSET = 10;

// @param mode 0:tolower, 1:toupper, 2:normal
void strcpy_cnv(char *dst, const char *src, int mode) {
  while (int c = *src++) {
    *dst++ =
      mode == 0 ? tolower(c) :
      mode == 1 ? toupper(c) :
      c;
  }
  *dst = 0;
}

static bool read_file(const char *name, int *fsize, uint8_t **fdata, int offset) {
  *fdata = nullptr;
  *fsize = 0;

  int fd = open(name, O_RDONLY | O_BINARY);
  if (fd == -1) {
    if (verbose) {
      fprintf(stderr, "cannot open %s\n", name);
    }
    return false;
  }

  struct stat st;
  if (fstat(fd, &st) == -1) {
    if (verbose) {
      fprintf(stderr, "cannot fstat %s\n", name);
    }
    st.st_size = 128 * 1024; // set tentative file size
  }

  int size = st.st_size;
  if (size == 0) {
    fprintf(stderr, "Invalid file size %s\n", name);
    close(fd);
    return false;
  }

  auto *data = new uint8_t[size + offset];
  size = read(fd, data + offset, size);

  close(fd);

  *fdata = data;
  *fsize = size + offset;
  return true;
}

// MDX 元数据结构
struct MdxInfo {
  char pdx_filename[256];  // PDX 文件名（从 MDX 头部提取）
  int num_channels;        // 通道数: 9=MXDRV(8FM+1PCM), 16=MXDRVm/MXDRVp(8FM+8PCM)
  int pdx_sample_rate;     // PDX 元数据中的采样率 (Hz)，-1 表示未设置
  int has_variable;        // 预扫描检测到 Variable 模式码（MXDRVp 标志）
};

static bool LoadMDX(const char *mdx_name, char *title, int title_len, MdxInfo *info) {
  uint8_t *mdx_buf = nullptr, *pdx_buf = nullptr;
  int mdx_size = 0, pdx_size = 0;

  // 初始化 info
  if (info) {
    info->pdx_filename[0] = 0;
    info->num_channels = 0;
    info->pdx_sample_rate = -1;
    info->has_variable = 0;
  }

  // Load MDX file
  if (!read_file(mdx_name, &mdx_size, &mdx_buf, MAGIC_OFFSET)) {
    fprintf(stderr, "Cannot open/read %s.\n", mdx_name);
    return false;
  }

  // Skip title.
  int pos = MAGIC_OFFSET;
  {
    char *ptitle = title;
    while (pos < mdx_size && --title_len > 0) {
      *ptitle++ = mdx_buf[pos];
      if (mdx_buf[pos] == 0x0d && mdx_buf[pos + 1] == 0x0a)
        break;
      pos++;
    }
    *ptitle = 0;
  }

  while (pos < mdx_size) {
    uint8_t c = mdx_buf[pos++];
    if (c == 0x1a) break;
  }

  char *pdx_name = (char*) mdx_buf + pos;

  // 保存原始 PDX 文件名（在被修改前拷贝）
  if (info && *pdx_name) {
    strncpy(info->pdx_filename, pdx_name, sizeof(info->pdx_filename) - 1);
    info->pdx_filename[sizeof(info->pdx_filename) - 1] = 0;
  }

  while (pos < mdx_size) {
    uint8_t c = mdx_buf[pos++];
    if (c == 0) break;
  }

  if (pos >= mdx_size) {
    delete[] mdx_buf;  // 内存泄漏修复：失败路径清理
    return false;
  }

  // Get mdx path.
  if (*pdx_name) {
    char pdx_path[FILENAME_MAX];
    strncpy(pdx_path, mdx_name, sizeof(pdx_path));

    int pdx_name_start = 0;
    for (int i = strlen(pdx_path) - 1; i > 0; i--) {
      if (pdx_path[i - 1] == '/' || pdx_path[i - 1] == '\\') {
        pdx_name_start = i;
        break;
      }
    }

    // 溢出检查：pdx_name 长度 + 扩展名 ".pdx" 必须在 pdx_path 剩余空间内
    if (pdx_name_start + strlen(pdx_name) + 4 >= sizeof(pdx_path)) {
      delete[] mdx_buf;  // 内存泄漏修复
      return false;
    }

    // remove .pdx from pdx_name
    {
      int pdx_name_len = strlen(pdx_name);
      if (pdx_name_len > 4) {
        if (pdx_name[pdx_name_len - 4] == '.') {
          pdx_name[pdx_name_len - 4] = 0;
        }
      }
    }

    // Make pdx path.
    for (int i = 0; i < 3 * 2; i++) {
      strcpy_cnv(pdx_path + pdx_name_start, pdx_name, i % 3);
      strcpy_cnv(pdx_path + pdx_name_start + strlen(pdx_name), ".pdx", i / 3);
      if (verbose) {
        fprintf(stderr, "try to open pdx:%s\n", pdx_path);
      }
      if (read_file(pdx_path, &pdx_size, &pdx_buf, MAGIC_OFFSET)) {
        break;
      }
    }
  }



  // Convert mdx to MXDRVG readable structure.
  int mdx_body_pos = pos;

  // ===== 通道数检测（参照 MDXWin）=====
  // MDX body 格式: [2B voice offset] [N×2B channel MML offsets] [MML data] [voice data]
  // 第一个通道 MML 偏移值（position 2-3, big-endian）指向第一个通道的 MML 数据起始
  // 由于所有偏移 word 紧密排列: first_channel_offset = 2 + num_channels * 2
  // 所以 num_channels = (first_channel_offset - 2) / 2
  if (info && mdx_body_pos + 4 <= mdx_size) {
    int first_ch_offset = (mdx_buf[mdx_body_pos + 2] << 8) | mdx_buf[mdx_body_pos + 3];
    if (first_ch_offset >= 2) {
      info->num_channels = (first_ch_offset - 2) / 2;
    }
  }

  if (verbose) {
    fprintf(stderr, "mdx body pos  :0x%x\n", mdx_body_pos - MAGIC_OFFSET);
    fprintf(stderr, "mdx body size :0x%x\n", mdx_size - mdx_body_pos - MAGIC_OFFSET);
    if (info) {
      fprintf(stderr, "channels      :%d\n", info->num_channels);
    }
  }

  uint8_t *mdx_head = mdx_buf + mdx_body_pos - MAGIC_OFFSET;
  mdx_head[0] = 0x00;
  mdx_head[1] = 0x00;
  mdx_head[2] = (pdx_buf ? 0 : 0xff);
  mdx_head[3] = (pdx_buf ? 0 : 0xff);
  mdx_head[4] = 0;
  mdx_head[5] = 0x0a;
  mdx_head[6] = 0x00;
  mdx_head[7] = 0x08;
  mdx_head[8] = 0x00;
  mdx_head[9] = 0x00;

  if (pdx_buf) {
    pdx_buf[0] = 0x00;
    pdx_buf[1] = 0x00;
    pdx_buf[2] = 0x00;
    pdx_buf[3] = 0x00;
    pdx_buf[4] = 0x00;
    pdx_buf[5] = 0x0a;
    pdx_buf[6] = 0x00;
    pdx_buf[7] = 0x02;
    pdx_buf[8] = 0x00;
    pdx_buf[9] = 0x00;
  }

  if (verbose) {
    fprintf(stderr, "instrument pos:0x%x\n", mdx_body_pos - 10 + (mdx_head[10] << 8) + mdx_head[11]);
  }

  int pdx_sample_rate = -1;  // 用于保存解析出的采样率

  // ── PDX 采样率元数据解析 ──
  // 检查 PDX buffer 尾部是否有 "rXDP" 签名（wav2pdx 扩展格式）
  // 格式: "PDXr"(4B) + version(2B) + count(2B) + entries(N×6B) + "rXDP"(4B)
  // 每个 entry: slot_index(2B BE) + sample_rate(4B BE)
  if (pdx_buf && pdx_size > MAGIC_OFFSET + 12) {
    uint8_t *pdx_data = pdx_buf + MAGIC_OFFSET;
    int pdx_data_size = pdx_size - MAGIC_OFFSET;

    // 检查尾部 4 字节 "rXDP" 签名
    if (pdx_data_size >= 12 &&
        pdx_data[pdx_data_size - 4] == 'r' &&
        pdx_data[pdx_data_size - 3] == 'X' &&
        pdx_data[pdx_data_size - 2] == 'D' &&
        pdx_data[pdx_data_size - 1] == 'P') {

      // 从尾部向前扫描寻找 "PDXr" magic
      // 元数据块最大: 4 + 2 + 2 + 96×6 + 4 = 588 字节
      int scan_start = pdx_data_size - 588;
      if (scan_start < 0) scan_start = 0;

      for (int pos = pdx_data_size - 12; pos >= scan_start; pos--) {
        if (pdx_data[pos] == 'P' && pdx_data[pos+1] == 'D' &&
            pdx_data[pos+2] == 'X' && pdx_data[pos+3] == 'r') {
          // 找到 magic，解析 version 和 count
          int ver = (pdx_data[pos+4] << 8) | pdx_data[pos+5];
          int cnt = (pdx_data[pos+6] << 8) | pdx_data[pos+7];

          if (ver == 1 && cnt > 0 && cnt <= 96) {
            // 验证块大小是否一致: 4+2+2+cnt*6+4
            int expected_size = 4 + 2 + 2 + cnt * 6 + 4;
            if (pos + expected_size == pdx_data_size) {
              // 读取第一个条目的采样率（当前 Variable 模式仅使用一个全局基准）
              int entry_offset = pos + 8;
              // int slot_idx = (pdx_data[entry_offset] << 8) | pdx_data[entry_offset + 1];
              int sample_rate = (pdx_data[entry_offset + 2] << 24) |
                                (pdx_data[entry_offset + 3] << 16) |
                                (pdx_data[entry_offset + 4] << 8)  |
                                 pdx_data[entry_offset + 5];

              if (verbose) {
                fprintf(stderr, "PDX metadata: %d entries, first rate=%d Hz\n", cnt, sample_rate);
              }

              // 保存提取到的采样率，延后设置
              pdx_sample_rate = sample_rate;
            }
          }
          break;  // 找到 magic 就停止扫描
        }
      }
    }
  }

  MXDRVG_SetData(mdx_head, mdx_size, pdx_buf, pdx_size);

  // 查询预扫描检测到的 Variable 模式标志（必须在 SetData 之后）
  if (info) {
    info->has_variable = MXDRVG_HasVariableMode();
  }

  // 必须在 MXDRVG_SetData 之后设置，因为 SetData 内部会调用 Init() 重置基准采样率
  if (pdx_sample_rate > 0) {
    if (verbose) {
      fprintf(stderr, "Setting Variable Base Rate to %d Hz\n", pdx_sample_rate);
    }
    MXDRVG_SetPCM8VariableBaseRate(pdx_sample_rate);
    if (info) {
      info->pdx_sample_rate = pdx_sample_rate;
    }
  }

  delete []mdx_buf;
  delete []pdx_buf;

  return true;
}

static void version() {
  printf(
    "mdx2wav version " VERSION "\n"
    "Copyright 2014 @__mtm\n"
    " based on MDXDRVg V1.50a (C) 2000 GORRY.\n"
    "  converted from X68k MXDRV music driver version 2.06+17 Rel.X5-S\n"
    "   (c)1988-92 milk.,K.MAEKAWA, Missy.M, Yatsube\n"
    );
}

static void help() {
  printf(
    "Usage: mdx2wav [options] <file>\n"
    "Convert MDX file to WAV (default) or raw PCM.\n"
    "  By default, writes <input>.wav to the same directory.\n"
    "  Drag and drop an MDX file onto mdx2wav.exe to convert.\n"
    "\n"
    "Options:\n"
    "  -o <file> : specify output WAV filename.\n"
    "  -r        : raw mode, output 16bit stereo PCM to stdout (no WAV header).\n"
    "  -d <sec>  : limit song duration. 0 means nolimit. (default:300)\n"
    "  -e <type> : set ym2151 emulation type, nuked or ymfm. (default:ymfm)\n"
    "  -f        : enable fadeout. (default:on)\n"
    "  -i        : output song info as JSON to stdout and exit.\n"
    "  -l <loop> : set loop limit. (default:2)\n"
    "  -m        : measure play time as sec.\n"
    "  -v        : print version.\n"
    "  -V        : verbose, write debug log to stderr.\n"
    );
}

// ── WAV 文件头写入（标准 44 字节 RIFF PCM 格式）──
// 参数: 采样率、通道数 (2=stereo)、每采样位深 (16)、PCM 数据总字节数
static void write_wav_header(FILE *fp, uint32_t sample_rate, uint16_t channels,
                             uint16_t bits_per_sample, uint32_t data_size) {
  uint32_t byte_rate = sample_rate * channels * (bits_per_sample / 8);
  uint16_t block_align = channels * (bits_per_sample / 8);
  uint32_t riff_size = 36 + data_size;  // 文件总大小 - 8

  // RIFF 头
  fwrite("RIFF", 1, 4, fp);
  fwrite(&riff_size, 4, 1, fp);
  fwrite("WAVE", 1, 4, fp);

  // fmt 子块
  fwrite("fmt ", 1, 4, fp);
  uint32_t fmt_size = 16;
  fwrite(&fmt_size, 4, 1, fp);
  uint16_t audio_format = 1;  // PCM
  fwrite(&audio_format, 2, 1, fp);
  fwrite(&channels, 2, 1, fp);
  fwrite(&sample_rate, 4, 1, fp);
  fwrite(&byte_rate, 4, 1, fp);
  fwrite(&block_align, 2, 1, fp);
  fwrite(&bits_per_sample, 2, 1, fp);

  // data 子块
  fwrite("data", 1, 4, fp);
  fwrite(&data_size, 4, 1, fp);
}

// ── 从输入文件名生成输出 WAV 路径 ──
// "path/to/song.mdx" → "path/to/song.wav"
static void make_wav_filename(char *out, size_t out_size, const char *mdx_name) {
  strncpy(out, mdx_name, out_size - 1);
  out[out_size - 1] = '\0';

  // 找到最后一个 '.' 的位置
  char *dot = nullptr;
  for (char *p = out; *p; p++) {
    if (*p == '.') dot = p;
  }

  if (dot && (size_t)(dot - out) + 4 < out_size) {
    strcpy(dot, ".wav");
  } else {
    // 无扩展名或空间不足：直接追加
    size_t len = strlen(out);
    if (len + 4 < out_size) {
      strcpy(out + len, ".wav");
    }
  }
}



// ── 辅助：重置播放状态（消除 PlayAt 后重复设置 Variable/PCM8Volume 的代码）──
static void ResetPlayback(int loop, int fadeout, const MdxInfo &info) {
  MXDRVG_PlayAt(0, loop, fadeout);
  // PlayAt 内部重新初始化 PCM8，必须重新设置 Variable 基准采样率
  if (info.pdx_sample_rate > 0) {
    MXDRVG_SetPCM8VariableBaseRate(info.pdx_sample_rate);
  }
  // 恢复 OPM/PCM 平衡比
  MXDRVG_SetPCM8Volume(-11);
}


int main(int argc, char **argv) {
#ifdef _WIN32
  // Windows: stdout 默认文本模式会在 0x0a 前插入 0x0d，破坏二进制 PCM 输出
  _setmode(_fileno(stdout), _O_BINARY);
  _setmode(_fileno(stderr), _O_BINARY);
#endif
  constexpr int SAMPLE_RATE = 62500;  // X68K 原生采样率，不做重采样
  int filter_mode = 0;

  bool measure_play_time = false;
  bool get_info = false;
  bool raw_mode = false;  // -r: 原始 PCM 输出到 stdout（向后兼容）
  char output_path[FILENAME_MAX] = "";  // -o: 指定输出文件名
  float max_song_duration = 300.0f;
  int loop = 2;
  int fadeout = 1;  // 默认启用淡出
  char ym2151_type[8] = "ymfm";  // 默认 YMFM 引擎

  int opt;
  while ((opt = getopt(argc, argv, "d:e:fil:mo:rvV")) != -1) {
    switch (opt) {
      case 'd':
        max_song_duration = atof(optarg);
        break;
      case 'e':
        strncpy(ym2151_type, optarg, sizeof(ym2151_type));
        break;
      case 'f':
        fadeout = 1;
        break;
      case 'i':
        get_info = true;
        break;
      case 'l':
        loop = atoi(optarg);
        break;
      case 'm':
        measure_play_time = true;
        break;
      case 'o':
        strncpy(output_path, optarg, sizeof(output_path) - 1);
        break;
      case 'r':
        raw_mode = true;
        break;
      case 'v':
        version();
        return 0;
      case 'V':
        verbose = true;
        break;
      default:
        help();
        return 0;
    }
  }

  constexpr int AUDIO_BUF_SAMPLES = SAMPLE_RATE / 100; // 10ms

  const char *mdx_name = argv[optind];
  if (mdx_name == nullptr || *mdx_name == 0) {
    help();
    return 0;
  }

  if (0 == strcmp(ym2151_type, "nuked")) {
    MXDRVG_SetEmulationType(MXDRVG_YM2151TYPE_NUKED);
  } else if (0 == strcmp(ym2151_type, "ymfm")) {
    MXDRVG_SetEmulationType(MXDRVG_YM2151TYPE_YMFM);
  } else {
    fprintf(stderr, "Invalid ym2151 emulation type: %s.\n", ym2151_type);
    return -1;
  }

  MXDRVG_Start(SAMPLE_RATE, filter_mode, 256 * 1024, 1024 * 1024);
  MXDRVG_TotalVolume(256);

  char title[256];
  MdxInfo mdx_info;

  if (!LoadMDX(mdx_name, title, sizeof(title), &mdx_info)) {
    return -1;
  }

  // ===== PCM8Volume 说明 =====
  // MDXWin 使用 PCM8Volume (0.65/0.9) 作为逐通道浮点乘数
  // gamdx 的 PCM8.SetVolume(dB) 控制的是 PCM 混音总线全局增益
  // 两者架构不同，不能直接映射。保持 MXDRVG_Start 中的默认 0dB
  // 音量平衡交由 ReplayGain 峰值归一化自动处理

  // ===== 元数据提取（-i 选项）=====
  // 参照 MDXWin CMDXFile.cs: timer_b 通过 MML 命令 0xff 设置
  // 初始默认值 200，需要在播放处理后读取
  // MXDRVG_MeasurePlayTime 会完整走一遍 MML，@t 会被处理
  volatile MXDRVG_WORK_GLOBAL *gwork =
    (volatile MXDRVG_WORK_GLOBAL *)MXDRVG_GetWork(MXDRVG_WORKADR_GLOBAL);

  float song_duration = MXDRVG_MeasurePlayTime(loop, fadeout) / 1000.0f;
  // Warning: MXDRVG_MeasurePlayTime calls MXDRVG_End internaly,
  //          thus we need to call MXDRVG_PlayAt due to reset playing status.

  // MeasurePlayTime 后读取 timer_b 和 total clock
  // 注意: MeasurePlayTime 内部调用 End 前 @t 已被处理
  int timer_b = gwork->L001e0c;  // @t: Timer-B 値（MML 处理后的最终值）
  unsigned long total_clock = gwork->PLAYTIME;

  // ===== OPM/PCM 音量平衡（参照 MDXWin）=====
  // MDXWin: OPM 和 PCM 在同一 float[-1,1] 空间 1:1 混合，PCM 额外 ×0.65
  // gamdx: fmgen/MAME 均使用 db/40 公式: mVolume = 16384 × 10^(db/40)
  //   OPM SetVolume(-12) → mVolume = 16384 × 10^(-0.3) = 8798
  //   PCM 目标 mVolume = 8798 × 1.00 = 8798
  //   db = 40 × log10(8798/16384) = -11
  ResetPlayback(loop, fadeout, mdx_info);

  // 输出 JSON 元数据到 stdout（参照 MDXWin GetVisualGlobal）
  if (get_info) {
    // 参照 MDXWin SetTimerB:
    //   micros = (1024 * (256 - TimerB)) / 4
    //   TempoClock = micros / 1000000 秒
    //   BPM = 60 / (TempoClock * 48)
    double tempo_bpm = 0.0;
    if (timer_b < 256) {
      double micros = (1024.0 * (256 - timer_b)) / 4.0;
      double tempo_clock_sec = micros / 1000000.0;
      tempo_bpm = 60.0 / (tempo_clock_sec * 48.0);
    }

    // 驱动类型判断（参照 MDXWin）
    // MXDRV:  9ch (8FM+1ADPCM)，原版驱动
    // MXDRVm: 16ch (8FM+8PCM)，PCM8/PCM8PP 多通道
    // MXDRVp: 16ch + Variable 模式码，可变频率 PCM 支持
    const char *use_driver = "MXDRV";
    if (mdx_info.num_channels > 9) {
      if (mdx_info.has_variable) {
        use_driver = "MXDRVp";  // 可变频率 PCM 支持
      } else {
        use_driver = "MXDRVm";  // 标准 PCM8 多通道
      }
    }

    // JSON 输出（手动构建，避免引入 JSON 库）
    // 对 title 中的特殊字符进行转义
    char escaped_title[1024];
    {
      int si = 0, di = 0;
      while (title[si] && di < (int)sizeof(escaped_title) - 6) {
        unsigned char ch = (unsigned char)title[si];
        if (ch == '"') {
          escaped_title[di++] = '\\';
          escaped_title[di++] = '"';
        } else if (ch == '\\') {
          escaped_title[di++] = '\\';
          escaped_title[di++] = '\\';
        } else if (ch == '\n') {
          escaped_title[di++] = '\\';
          escaped_title[di++] = 'n';
        } else if (ch == '\r') {
          escaped_title[di++] = '\\';
          escaped_title[di++] = 'r';
        } else if (ch == '\t') {
          escaped_title[di++] = '\\';
          escaped_title[di++] = 't';
        } else {
          escaped_title[di++] = ch;
        }
        si++;
      }
      escaped_title[di] = 0;
    }

    printf(
      "{\n"
      "  \"title\": \"%s\",\n"
      "  \"pdx_filename\": \"%s\",\n"
      "  \"use_driver\": \"%s\",\n"
      "  \"channels\": %d,\n"
      "  \"timer_b\": %d,\n"
      "  \"tempo_bpm\": %.1f,\n"
      "  \"duration_ms\": %d,\n"
      "  \"total_clock\": %lu,\n"
      "  \"format\": \"62500Hz, 16bit, stereo\"\n"
      "}\n",
      escaped_title,
      mdx_info.pdx_filename,
      use_driver,
      mdx_info.num_channels,
      timer_b,
      tempo_bpm,
      (int)(song_duration * 1000),
      total_clock
    );
    MXDRVG_End();
    return 0;
  }

  if (measure_play_time) {
    printf("%d\n", (int) ceilf(song_duration));
    return 0;
  }

  if (verbose) {
    fprintf(stderr, "loop:%d fadeout:%d song_duration:%f\n", loop, fadeout, song_duration);
  }

  if (max_song_duration < song_duration) {
    song_duration = max_song_duration;
  }

  short *audio_buf = new short [AUDIO_BUF_SAMPLES * 2];

  // ===== ReplayGain: 两遍渲染峰值归一化 =====
  // 第一遍: 渲染全曲找到峰值，同时计算总采样数（用于 WAV 头）
  short peak = 0;
  uint32_t total_samples = 0;  // 总立体声采样数
  for (int i = 0; song_duration == 0.0f || 1.0f * i * AUDIO_BUF_SAMPLES / SAMPLE_RATE < song_duration; i++) {
    if (MXDRVG_GetTerminated()) break;
    int len = MXDRVG_GetPCM(audio_buf, AUDIO_BUF_SAMPLES);
    if (len <= 0) break;
    total_samples += len;
    for (int j = 0; j < len * 2; j++) {
      short abs_val = (audio_buf[j] < 0) ? -audio_buf[j] : audio_buf[j];
      if (abs_val > peak) peak = abs_val;
    }
  }

  // 计算 ReplayGain 补偿
  // 目标: 峰值占 int16 范围的 90%（留 10% headroom）
  int replayGainVol = 256;  // 默认无增益
  if (peak > 0) {
    float target_peak = 32767.0f * 0.9f;
    float gain = target_peak / peak;
    // 限制最大增益为 4倍（防止极小音量文件的过度放大）
    if (gain > 4.0f) gain = 4.0f;
    replayGainVol = (int)(256.0f * gain);
    if (verbose) {
      fprintf(stderr, "ReplayGain: peak=%d gain=%.2f vol=%d\n", peak, gain, replayGainVol);
    }
  }

  // ── 确定输出目标 ──
  FILE *out_fp = nullptr;
  bool writing_wav = false;  // 是否写 WAV 文件（vs raw stdout）

  if (raw_mode) {
    // -r: 原始 PCM 输出到 stdout（向后兼容旧管道用法）
    out_fp = stdout;
    writing_wav = false;
  } else {
    // 默认: 写 WAV 文件
    if (output_path[0] == '\0') {
      // 未指定 -o: 自动从输入文件名生成
      make_wav_filename(output_path, sizeof(output_path), mdx_name);
    }
    out_fp = fopen(output_path, "wb");
    if (!out_fp) {
      fprintf(stderr, "Cannot create output file: %s\n", output_path);
      delete[] audio_buf;
      MXDRVG_End();
      return -1;
    }
    writing_wav = true;
    fprintf(stderr, "Rendering: %s\n", output_path);
  }

  // WAV 头写入（data_size = 总采样数 × 2通道 × 2字节）
  uint32_t pcm_data_size = total_samples * 2 * sizeof(short);
  if (writing_wav) {
    write_wav_header(out_fp, SAMPLE_RATE, 2, 16, pcm_data_size);
  }

  // 第二遍: 重新渲染并输出
  ResetPlayback(loop, fadeout, mdx_info);
  MXDRVG_TotalVolume(replayGainVol);

  for (int i = 0; song_duration == 0.0f || 1.0f * i * AUDIO_BUF_SAMPLES / SAMPLE_RATE < song_duration; i++) {
    if (MXDRVG_GetTerminated()) break;
    int len = MXDRVG_GetPCM(audio_buf, AUDIO_BUF_SAMPLES);
    if (len <= 0) break;
    fwrite(audio_buf, len, 4, out_fp);
  }

  // 关闭输出
  if (writing_wav && out_fp) {
    fclose(out_fp);
    float dur = (float)total_samples / SAMPLE_RATE;
    fprintf(stderr, "Done: %.1fs, %u samples -> %s\n", dur, total_samples, output_path);
  }

  MXDRVG_End();
  delete[] audio_buf;

  return 0;
}
