// Nuked OPM (Nuke.YKT, LGPL-2.1) 适配器实现
// 将 Nuked OPM 的 cycle-accurate YM2151 桥接到 gamdx 的 OPM_Delegate 接口
//
// 修改记录 (Rennsou1_2006, 2026):
//   - 混合寄存器写入方案:
//       key-on (0x08) 走 OPM_Write + OPM_Clock 管线（OPM_KeyOn1 需要 slot 轮转）
//       其他寄存器直接操作 opm_t 内部字段（零额外时钟推进）
//   - CYCLES_PER_SAMPLE = 32（匹配 Nuked OPM 的 32-slot 内部周期）
//   - Count/GetNextEvent 使用独立定时器计数器模型（避免与 Mix 双重推进）

#include "opm_nuked.h"
#include <cmath>
#include <cstring>

// Nuked OPM 的 OPM_Clock 代表内部时钟 φ (= 主时钟 M / 2)
// chip->cycles 范围 0-31，每 32 个 OPM_Clock = 一个完整采样周期
// 芯片采样率 = M / 2 / 32 = M / 64
static const int CYCLES_PER_SAMPLE = 32;
static const int CLOCK_DIVIDER     = 64;

// ---------------------------------------------------------------------------
//  构造与析构
// ---------------------------------------------------------------------------

OPMNuked::OPMNuked()
  : m_clock(0)
  , m_rate(0)
  , m_volume(16384)
  , m_chip_rate(0)
  , m_resample_accum(0)
  , m_prev_irq(0)
  , m_callback(nullptr)
  , m_timer_clocks_a(-1)
  , m_timer_clocks_b(-1)
  , m_timer_period_a(0)
  , m_timer_period_b(0)
  , m_timer_enabled_a(false)
  , m_timer_enabled_b(false)
  , m_timer_irq_en_a(false)
  , m_timer_irq_en_b(false)
  , m_reg_timer_a(0)
  , m_reg_timer_b(0)
{
  memset(&m_chip, 0, sizeof(m_chip));
  m_last_out[0] = m_last_out[1] = 0;
}

OPMNuked::~OPMNuked() {}

// ---------------------------------------------------------------------------
//  初始化
// ---------------------------------------------------------------------------

bool OPMNuked::Init(uint c, uint r, bool /*f*/)
{
  m_clock = c;
  m_rate  = r;
  OPM_Reset(&m_chip, opm_flags_none);

  m_chip_rate      = m_clock / CLOCK_DIVIDER;
  m_resample_accum = 0;
  m_last_out[0]    = 0;
  m_last_out[1]    = 0;
  m_prev_irq       = 0;

  m_timer_clocks_a  = -1;
  m_timer_clocks_b  = -1;
  m_timer_period_a  = 0;
  m_timer_period_b  = 0;
  m_timer_enabled_a = false;
  m_timer_enabled_b = false;
  m_timer_irq_en_a  = false;
  m_timer_irq_en_b  = false;
  m_reg_timer_a     = 0;
  m_reg_timer_b     = 0;

  SetVolume(0);
  return true;
}

// ---------------------------------------------------------------------------
//  重置
// ---------------------------------------------------------------------------

void OPMNuked::Reset()
{
  OPM_Reset(&m_chip, opm_flags_none);

  m_resample_accum  = 0;
  m_last_out[0]     = 0;
  m_last_out[1]     = 0;
  m_prev_irq        = 0;

  m_timer_clocks_a  = -1;
  m_timer_clocks_b  = -1;
  m_timer_period_a  = 0;
  m_timer_period_b  = 0;
  m_timer_enabled_a = false;
  m_timer_enabled_b = false;
  m_timer_irq_en_a  = false;
  m_timer_irq_en_b  = false;
  m_reg_timer_a     = 0;
  m_reg_timer_b     = 0;
}

// ---------------------------------------------------------------------------
//  生成一个芯片采样（完整的 32-slot 轮转）
// ---------------------------------------------------------------------------

void OPMNuked::clockOneSample()
{
  int32_t buf[2];
  uint8_t sh1, sh2, so;
  for (int i = 0; i < CYCLES_PER_SAMPLE; i++)
    OPM_Clock(&m_chip, buf, &sh1, &sh2, &so);
  m_last_out[0] = m_chip.dac_output[0];
  m_last_out[1] = m_chip.dac_output[1];
}

// ---------------------------------------------------------------------------
//  定时器周期更新
// ---------------------------------------------------------------------------

void OPMNuked::updateTimerPeriods()
{
  // Timer A: 64 × (1024 - TA) 个主时钟
  // Timer B: 1024 × (256 - TB) 个主时钟
  m_timer_period_a = 64LL * (1024 - m_reg_timer_a);
  m_timer_period_b = 1024LL * (256 - m_reg_timer_b);
}

// ---------------------------------------------------------------------------
//  寄存器写入 — 混合方案
//
//  Key-on (0x08):
//    走 OPM_Write + OPM_Clock 管线。OPM_KeyOn1 在每个 OPM_Clock 周期
//    只处理 mode_kon_channel 匹配的通道，因此需要完整 32-slot 轮转
//    确保 key 状态被锁存。直接写入会导致多通道 key-on 丢失。
//
//  其他寄存器 (0x00-0x07, 0x09-0xFF):
//    直接操作 opm_t 内部字段。逻辑参照 OPM_DoRegWrite (opm.c:1683-1937)。
//    零额外 OPM_Clock 调用 = 无相位/包络/LFO 偏移。
// ---------------------------------------------------------------------------

void OPMNuked::SetReg(uint addr, uint data)
{
  const uint8_t a = addr & 0xFF;
  const uint8_t d = data & 0xFF;

  // ── 独立定时器同步 ──
  switch (a) {
    case 0x10:
      m_reg_timer_a = (m_reg_timer_a & 0x03) | (d << 2);
      updateTimerPeriods();
      break;
    case 0x11:
      m_reg_timer_a = (m_reg_timer_a & 0x3FC) | (d & 0x03);
      updateTimerPeriods();
      break;
    case 0x12:
      m_reg_timer_b = d;
      updateTimerPeriods();
      break;
    case 0x14: {
      const bool old_a = m_timer_enabled_a;
      const bool old_b = m_timer_enabled_b;
      m_timer_enabled_a = (d & 0x01) != 0;
      m_timer_enabled_b = (d & 0x02) != 0;
      m_timer_irq_en_a  = (d & 0x04) != 0;
      m_timer_irq_en_b  = (d & 0x08) != 0;
      if ( m_timer_enabled_a && !old_a) m_timer_clocks_a = m_timer_period_a;
      if (!m_timer_enabled_a)           m_timer_clocks_a = -1;
      if ( m_timer_enabled_b && !old_b) m_timer_clocks_b = m_timer_period_b;
      if (!m_timer_enabled_b)           m_timer_clocks_b = -1;
      break;
    }
    default:
      break;
  }

  // ── Key-on (0x08): OPM_Write + 完整 slot 轮转 ──
  if (a == 0x08) {
    int32_t out[2];
    uint8_t sh1, sh2, so;
    OPM_Write(&m_chip, 0, a);
    for (int i = 0; i < CYCLES_PER_SAMPLE; i++)
      OPM_Clock(&m_chip, out, &sh1, &sh2, &so);
    OPM_Write(&m_chip, 1, d);
    for (int i = 0; i < CYCLES_PER_SAMPLE; i++)
      OPM_Clock(&m_chip, out, &sh1, &sh2, &so);
    return;
  }

  // ── Mode 寄存器 (0x00-0x1F, 除 0x08) ──
  if (a < 0x20) {
    switch (a) {
      case 0x01:
        for (int i = 0; i < 8; i++)
          m_chip.mode_test[i] = (d >> i) & 0x01;
        break;
      case 0x0f:
        m_chip.noise_en   = d >> 7;
        m_chip.noise_freq = d & 0x1f;
        break;
      case 0x10:
        m_chip.timer_a_reg = (m_chip.timer_a_reg & 0x03)  | (d << 2);
        break;
      case 0x11:
        m_chip.timer_a_reg = (m_chip.timer_a_reg & 0x3fc) | (d & 0x03);
        break;
      case 0x12:
        m_chip.timer_b_reg = d;
        break;
      case 0x14:
        m_chip.mode_csm      = (d >> 7) & 1;
        m_chip.timer_irqb    = (d >> 3) & 1;
        m_chip.timer_irqa    = (d >> 2) & 1;
        m_chip.timer_resetb  = (d >> 5) & 1;
        m_chip.timer_reseta  = (d >> 4) & 1;
        m_chip.timer_loadb   = (d >> 1) & 1;
        m_chip.timer_loada   = (d >> 0) & 1;
        break;
      case 0x18:
        m_chip.lfo_freq_hi   = d >> 4;
        m_chip.lfo_freq_lo   = d & 0x0f;
        m_chip.lfo_frq_update = 1;
        break;
      case 0x19:
        if (d & 0x80) m_chip.lfo_pmd = d & 0x7f;
        else          m_chip.lfo_amd = d;
        break;
      case 0x1b:
        m_chip.lfo_wave = d & 0x03;
        m_chip.io_ct1   = (d >> 6) & 0x01;
        m_chip.io_ct2   = d >> 7;
        break;
      default:
        break;
    }
    return;
  }

  // ── Channel 寄存器 (0x20-0x3F) ──
  if (a < 0x40) {
    const uint8_t ch = a & 0x07;
    switch (a & 0x18) {
      case 0x00:  // RL, FB, CONNECT
        m_chip.ch_rl[ch]      = d >> 6;
        m_chip.ch_fb[ch]      = (d >> 3) & 0x07;
        m_chip.ch_connect[ch] = d & 0x07;
        break;
      case 0x08:  // KC
        m_chip.ch_kc[ch] = d & 0x7f;
        break;
      case 0x10:  // KF
        m_chip.ch_kf[ch] = d >> 2;
        break;
      case 0x18:  // PMS, AMS
        m_chip.ch_pms[ch] = (d >> 4) & 0x07;
        m_chip.ch_ams[ch] = d & 0x03;
        break;
    }
    return;
  }

  // ── Slot 寄存器 (0x40-0xFF) ──
  const uint8_t slot = a & 0x1f;
  switch (a & 0xe0) {
    case 0x40:  // DT1, MUL
      m_chip.sl_dt1[slot] = (d >> 4) & 0x07;
      m_chip.sl_mul[slot] = d & 0x0f;
      break;
    case 0x60:  // TL
      m_chip.sl_tl[slot] = d & 0x7f;
      break;
    case 0x80:  // KS, AR
      m_chip.sl_ks[slot] = d >> 6;
      m_chip.sl_ar[slot] = d & 0x1f;
      break;
    case 0xa0:  // AMS-EN, D1R
      m_chip.sl_am_e[slot] = d >> 7;
      m_chip.sl_d1r[slot]  = d & 0x1f;
      break;
    case 0xc0:  // DT2, D2R
      m_chip.sl_dt2[slot] = d >> 6;
      m_chip.sl_d2r[slot] = d & 0x1f;
      break;
    case 0xe0:  // D1L, RR
      m_chip.sl_d1l[slot] = d >> 4;
      m_chip.sl_rr[slot]  = d & 0x0f;
      break;
    default:
      break;
  }
}

// ---------------------------------------------------------------------------
//  读取状态
// ---------------------------------------------------------------------------

uint OPMNuked::ReadStatus()
{
  return OPM_Read(&m_chip, 0);
}

// ---------------------------------------------------------------------------
//  音量设定
// ---------------------------------------------------------------------------

void OPMNuked::SetVolume(int db)
{
  if (db > 20) db = 20;
  m_volume = (db > -192) ? (int)(16384.0 * pow(10.0, db / 40.0)) : 0;
}

// ---------------------------------------------------------------------------
//  音声合成（重采样: chip_rate → output_rate）
// ---------------------------------------------------------------------------

void OPMNuked::Mix(short* buffer, int nsamples)
{
  if (nsamples <= 0 || m_chip_rate == 0) return;

  for (int i = 0; i < nsamples; i++) {
    m_resample_accum += m_chip_rate;
    while (m_resample_accum >= m_rate) {
      m_resample_accum -= m_rate;
      clockOneSample();
    }

    // 音量缩放 + 16-bit 钳位
    int32_t left  = (m_last_out[0] * m_volume) >> 14;
    int32_t right = (m_last_out[1] * m_volume) >> 14;
    if (left  < -32768) left  = -32768; else if (left  > 32767) left  = 32767;
    if (right < -32768) right = -32768; else if (right > 32767) right = 32767;

    // 混合到缓冲区（累加模式）
    int32_t s0 = buffer[i * 2]     + left;
    int32_t s1 = buffer[i * 2 + 1] + right;
    if (s0 < -32768) s0 = -32768; else if (s0 > 32767) s0 = 32767;
    if (s1 < -32768) s1 = -32768; else if (s1 > 32767) s1 = 32767;
    buffer[i * 2]     = (short)s0;
    buffer[i * 2 + 1] = (short)s1;
  }
}

// ---------------------------------------------------------------------------
//  定时器处理（独立计数器模型，不调用 OPM_Clock）
// ---------------------------------------------------------------------------

bool OPMNuked::Count(int32 us)
{
  if (m_clock == 0 || us <= 0) return false;

  const int64_t clocks = (int64_t)us * m_clock / 1000000;
  bool fired = false;

  // Timer A
  if (m_timer_clocks_a >= 0) {
    m_timer_clocks_a -= clocks;
    while (m_timer_clocks_a <= 0) {
      fired = true;
      if (m_timer_irq_en_a && m_callback) m_callback();
      if (m_timer_enabled_a) {
        m_timer_clocks_a += m_timer_period_a;
        if (m_timer_clocks_a <= 0 && m_timer_period_a > 0)
          m_timer_clocks_a = m_timer_period_a;
      } else {
        m_timer_clocks_a = -1;
        break;
      }
    }
  }

  // Timer B
  if (m_timer_clocks_b >= 0) {
    m_timer_clocks_b -= clocks;
    while (m_timer_clocks_b <= 0) {
      fired = true;
      if (m_timer_irq_en_b && m_callback) m_callback();
      if (m_timer_enabled_b) {
        m_timer_clocks_b += m_timer_period_b;
        if (m_timer_clocks_b <= 0 && m_timer_period_b > 0)
          m_timer_clocks_b = m_timer_period_b;
      } else {
        m_timer_clocks_b = -1;
        break;
      }
    }
  }

  return fired;
}

// ---------------------------------------------------------------------------
//  获取下一事件时间（微秒）
// ---------------------------------------------------------------------------

int32 OPMNuked::GetNextEvent()
{
  if (m_clock == 0) return 0x7FFFFFFF;

  int64_t min_clocks = 0x7FFFFFFFLL;
  if (m_timer_clocks_a >= 0 && m_timer_clocks_a < min_clocks)
    min_clocks = m_timer_clocks_a;
  if (m_timer_clocks_b >= 0 && m_timer_clocks_b < min_clocks)
    min_clocks = m_timer_clocks_b;

  if (min_clocks >= 0x7FFFFFFFLL) return 0x7FFFFFFF;
  int64_t us = min_clocks * 1000000 / m_clock;
  return (int32)(us > 0 ? us : 1);
}

// ---------------------------------------------------------------------------
//  IRQ 回调
// ---------------------------------------------------------------------------

void OPMNuked::SetIrqCallback(CALLBACK *cb)
{
  m_callback = cb;
}
