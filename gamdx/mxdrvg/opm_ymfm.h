// YMFM (Aaron Giles, BSD-3) 适配器
// 将 YMFM 的 ym2151 桥接到 gamdx 的 OPM_Delegate 接口
#pragma once

#include "opm_delegate.h"
#include "../ymfm/ymfm_opm.h"

class OPMYmfm : public OPM_Delegate, public ymfm::ymfm_interface {
public:
  OPMYmfm();
  virtual ~OPMYmfm();

  // OPM_Delegate 接口
  void Reset() override;
  bool Count(int32 us) override;
  int32 GetNextEvent() override;
  bool Init(uint c, uint r, bool f) override;
  void SetReg(uint addr, uint data) override;
  uint ReadStatus() override;
  void Mix(short* buffer, int nsamples) override;
  void SetVolume(int db) override;
  void SetIrqCallback(CALLBACK *cb) override;

  // ymfm_interface 回调
  void ymfm_set_timer(uint32_t tnum, int32_t duration_in_clocks) override;
  void ymfm_update_irq(bool asserted) override;
  void ymfm_sync_mode_write(uint8_t data) override;
  void ymfm_sync_check_interrupts() override;

private:
  ymfm::ym2151  *m_chip;        // YMFM 芯片实例
  uint32_t       m_clock;       // 芯片时钟 (Hz)
  uint32_t       m_rate;        // 输出采样率 (Hz)
  int            m_volume;      // 音量缩放 (16384 = 0dB)

  // 定时器状态
  int64_t        m_timer_clocks[2];   // 剩余时钟数 (-1 = 停止)
  uint32_t       m_timer_period[2];   // 定时器周期 (时钟数)

  // 重采样状态 (chip rate → output rate)
  uint32_t       m_chip_rate;         // 芯片内部采样率
  uint32_t       m_resample_accum;    // 分数累加器
  int32_t        m_last_out[2];       // 最近一次 generate 输出

  // key-on 状态跟踪（解决 key-off + key-on 合并问题）
  uint8_t        m_keyon_state[8];    // 每通道当前 key-on 状态

  // IRQ 回调
  CALLBACK      *m_callback;
};
