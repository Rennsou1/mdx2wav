// YMFM (Aaron Giles, BSD-3) 适配器实现
// 将 YMFM 的 ym2151 桥接到 gamdx 的 OPM_Delegate 接口

#include "opm_ymfm.h"
#include <cmath>
#include <cstring>

// ---------------------------------------------------------------------------
//  构造与析构
// ---------------------------------------------------------------------------

OPMYmfm::OPMYmfm()
  : m_chip(nullptr)
  , m_clock(0)
  , m_rate(0)
  , m_volume(16384)
  , m_callback(nullptr)
{
  m_timer_clocks[0] = m_timer_clocks[1] = -1;
  m_timer_period[0] = m_timer_period[1] = 0;
  m_chip_rate = 0;
  m_resample_accum = 0;
  m_last_out[0] = m_last_out[1] = 0;
  memset(m_keyon_state, 0, sizeof(m_keyon_state));
}

OPMYmfm::~OPMYmfm()
{
  delete m_chip;
}

// ---------------------------------------------------------------------------
//  初始化
// ---------------------------------------------------------------------------

bool OPMYmfm::Init(uint c, uint r, bool /*f*/)
{
  m_clock = c;
  m_rate  = r;
  m_chip  = new ymfm::ym2151(*this);
  m_chip->reset();
  // YM2151: prescale=2, 芯片内部采样率 = clock / (64 * prescale) = clock / 128
  m_chip_rate = m_chip->sample_rate(m_clock);
  m_resample_accum = 0;
  m_last_out[0] = m_last_out[1] = 0;
  SetVolume(0);
  return true;
}

// ---------------------------------------------------------------------------
//  重置
// ---------------------------------------------------------------------------

void OPMYmfm::Reset()
{
  if (m_chip) {
    m_chip->reset();
  }
  m_timer_clocks[0] = m_timer_clocks[1] = -1;
  m_timer_period[0] = m_timer_period[1] = 0;
  m_resample_accum = 0;
  m_last_out[0] = m_last_out[1] = 0;
  memset(m_keyon_state, 0, sizeof(m_keyon_state));
}

// ---------------------------------------------------------------------------
//  寄存器写入
//  YMFM 使用 write(0, addr) + write(1, data) 两步写入
// ---------------------------------------------------------------------------

void OPMYmfm::SetReg(uint addr, uint data)
{
  if (!m_chip) return;

  // 处理 key-on/off 寄存器 (0x08) 的包络重置问题
  //
  // 问题背景:
  //   YMFM 的 clock_keystate() 仅在 key 状态发生 0→1 或 1→0 变化时
  //   才触发 start_attack() 或 start_release()。
  //   MXDRV 在同一个定时器中断内可能连续发送:
  //     1) key-off (opmask=0)  → m_keyon_live = 0
  //     2) key-on  (opmask≠0) → m_keyon_live = 非0
  //   由于两次写入之间没有 generate() 调用，YMFM 从未处理中间的 off 状态，
  //   clock_keystate 看到的最终状态与之前相同（都是 key-on），不会重启包络。
  //
  // 修复策略:
  //   对每次 key-on 写入，先强制插入 key-off + 一次 generate 刷新，
  //   确保 YMFM 内部状态正确经历 on→off→on 转换，从而触发包络重置。
  //   这与真实 YM2151 硬件行为一致（硬件上写入之间有 busy 等待时间差）。
  if ((addr & 0xFF) == 0x08) {
    uint8_t ch = data & 0x07;
    uint8_t opmask = (data >> 3) & 0x0F;

    if (opmask != 0) {
      // 收到 key-on: 先强制刷新 key-off 状态以确保包络重置
      // 覆盖两种情况:
      //   1) key-on→key-on（无中间 key-off）
      //   2) key-off→key-on（同一帧内，key-off 未被 generate 消化）
      m_chip->write(0, 0x08);
      m_chip->write(1, ch);  // opmask=0 = 全部 key-off
      // 运行一个芯片周期以刷新 key-off → start_release()
      ymfm::ym2151::output_data dummy;
      dummy.clear();
      m_chip->generate(&dummy, 1);
    }

    m_keyon_state[ch] = opmask;
  }

  m_chip->write(0, addr & 0xFF);
  m_chip->write(1, data & 0xFF);
}

// ---------------------------------------------------------------------------
//  读取状态
// ---------------------------------------------------------------------------

uint OPMYmfm::ReadStatus()
{
  if (m_chip) {
    return m_chip->read_status();
  }
  return 0;
}

// ---------------------------------------------------------------------------
//  音量设定
//  公式与 fmgen/MAME 完全一致: volume = 16384 * 10^(db/40)
// ---------------------------------------------------------------------------

void OPMYmfm::SetVolume(int db)
{
  if (db > 20) db = 20;
  if (db > -192)
    m_volume = (int)(16384.0 * pow(10.0, db / 40.0));
  else
    m_volume = 0;
}

// ---------------------------------------------------------------------------
//  音声合成
//  YMFM 输出 int32 立体声采样，需要缩放后累加到 int16 缓冲区
// ---------------------------------------------------------------------------

void OPMYmfm::Mix(short* buffer, int nsamples)
{
  if (!m_chip || nsamples <= 0 || m_chip_rate == 0) return;

  // 重采样: chip rate (e.g. 31250Hz) → output rate (e.g. 62500Hz)
  // 使用分数累加器实现 nearest-neighbor 插值
  for (int i = 0; i < nsamples; i++) {
    // 检查是否需要生成新的芯片采样
    m_resample_accum += m_chip_rate;
    while (m_resample_accum >= m_rate) {
      m_resample_accum -= m_rate;
      // 生成一个芯片采样
      ymfm::ym2151::output_data output;
      output.clear();
      m_chip->generate(&output, 1);
      m_last_out[0] = output.data[0];
      m_last_out[1] = output.data[1];
    }

    // 应用音量缩放（与 fmgen 一致: IStoSample = (s * fmvolume) >> 14）
    int32_t left  = (m_last_out[0] * m_volume) >> 14;
    int32_t right = (m_last_out[1] * m_volume) >> 14;

    // 钳位到 int16
    if (left  < -32768) left  = -32768;
    if (left  >  32767) left  =  32767;
    if (right < -32768) right = -32768;
    if (right >  32767) right =  32767;

    // 累加到输出缓冲区（StoreSample 语义）
    int32_t s0 = buffer[i * 2 + 0] + left;
    int32_t s1 = buffer[i * 2 + 1] + right;
    if (s0 < -32768) s0 = -32768;
    if (s0 >  32767) s0 =  32767;
    if (s1 < -32768) s1 = -32768;
    if (s1 >  32767) s1 =  32767;
    buffer[i * 2 + 0] = (short)s0;
    buffer[i * 2 + 1] = (short)s1;
  }
}

// ---------------------------------------------------------------------------
//  定时器处理
//  YMFM 通过 ymfm_set_timer 回调通知定时器启动/停止
//  gamdx 通过 Count(us) 推进时间，GetNextEvent() 查询下一事件
// ---------------------------------------------------------------------------

bool OPMYmfm::Count(int32 us)
{
  if (m_clock == 0) return false;

  // 将微秒转换为时钟数
  // clocks = us * clock / 1000000
  int64_t clocks = (int64_t)us * m_clock / 1000000;

  bool fired = false;
  for (int t = 0; t < 2; t++) {
    if (m_timer_clocks[t] < 0) continue;  // 定时器未启动

    m_timer_clocks[t] -= clocks;
    while (m_timer_clocks[t] <= 0) {
      // 定时器到期，触发引擎回调
      m_engine->engine_timer_expired(t);
      fired = true;

      // 重新装载（YMFM 内部会在 engine_timer_expired 中重新调用 ymfm_set_timer）
      // 如果定时器被重新设置，m_timer_clocks[t] 已被更新
      if (m_timer_clocks[t] < 0) break;  // 定时器被停止
    }
  }

  return fired;
}

int32 OPMYmfm::GetNextEvent()
{
  if (m_clock == 0) return 0x7FFFFFFF;

  int64_t min_clocks = 0x7FFFFFFFLL;
  for (int t = 0; t < 2; t++) {
    if (m_timer_clocks[t] >= 0 && m_timer_clocks[t] < min_clocks) {
      min_clocks = m_timer_clocks[t];
    }
  }

  if (min_clocks >= 0x7FFFFFFFLL) return 0x7FFFFFFF;

  // 转换为微秒
  int64_t us = min_clocks * 1000000 / m_clock;
  if (us <= 0) us = 1;
  return (int32)us;
}

// ---------------------------------------------------------------------------
//  IRQ 回调
// ---------------------------------------------------------------------------

void OPMYmfm::SetIrqCallback(CALLBACK *cb)
{
  m_callback = cb;
}

// ---------------------------------------------------------------------------
//  ymfm_interface 回调实现
// ---------------------------------------------------------------------------

void OPMYmfm::ymfm_set_timer(uint32_t tnum, int32_t duration_in_clocks)
{
  if (tnum >= 2) return;

  if (duration_in_clocks < 0) {
    // 取消定时器
    m_timer_clocks[tnum] = -1;
    m_timer_period[tnum] = 0;
  } else {
    // 设置定时器
    m_timer_clocks[tnum] = duration_in_clocks;
    m_timer_period[tnum] = duration_in_clocks;
  }
}

void OPMYmfm::ymfm_update_irq(bool asserted)
{
  if (asserted && m_callback) {
    m_callback();
  }
}

void OPMYmfm::ymfm_sync_mode_write(uint8_t data)
{
  // 直接转发到引擎
  m_engine->engine_mode_write(data);
}

void OPMYmfm::ymfm_sync_check_interrupts()
{
  // 直接转发到引擎
  m_engine->engine_check_interrupts();
}
