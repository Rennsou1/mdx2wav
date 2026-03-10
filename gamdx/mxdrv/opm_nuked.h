// Nuked OPM (Nuke.YKT, LGPL-2.1) 适配器
// 将 Nuked OPM 的 cycle-accurate YM2151 桥接到 gamdx 的 OPM_Delegate 接口
//
// 修改记录 (Rennsou1_2006, 2026):
//   - 混合寄存器写入: key-on 走 OPM_Write 管线，其他直接操作 opm_t
//   - CYCLES_PER_SAMPLE = 32（匹配内部 32-slot 周期）
//   - Count/GetNextEvent 使用独立定时器计数器模型
#pragma once

#include "opm_delegate.h"
#include "../nuked/opm.h"

class OPMNuked : public OPM_Delegate {
public:
  OPMNuked();
  virtual ~OPMNuked();

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

private:
  opm_t          m_chip;           // Nuked OPM 芯片实例（值类型，非指针）
  uint32_t       m_clock;          // 芯片主时钟 (Hz)，通常 4000000
  uint32_t       m_rate;           // 输出采样率 (Hz)，通常 62500
  int            m_volume;         // 音量缩放 (16384 = 0dB)

  // 重采样状态 (chip rate → output rate)
  uint32_t       m_chip_rate;      // 芯片内部采样率 = clock / 64
  uint32_t       m_resample_accum; // 分数累加器
  int32_t        m_last_out[2];    // 最近一次完整采样输出（左/右）

  // IRQ 状态
  uint8_t        m_prev_irq;       // 上一次 IRQ 状态（保留供未来使用）
  CALLBACK      *m_callback;       // IRQ 回调

  // ── 独立定时器管理（不依赖 OPM_Clock 推进） ──
  int64_t        m_timer_clocks_a;  // Timer A 剩余时钟数（<0 = 未启动）
  int64_t        m_timer_clocks_b;  // Timer B 剩余时钟数（<0 = 未启动）
  int64_t        m_timer_period_a;  // Timer A 周期（主时钟数）
  int64_t        m_timer_period_b;  // Timer B 周期（主时钟数）
  bool           m_timer_enabled_a; // Timer A load 位
  bool           m_timer_enabled_b; // Timer B load 位
  bool           m_timer_irq_en_a;  // Timer A IRQ 使能
  bool           m_timer_irq_en_b;  // Timer B IRQ 使能
  uint16_t       m_reg_timer_a;     // Timer A 寄存器值 (10-bit)
  uint8_t        m_reg_timer_b;     // Timer B 寄存器值 (8-bit)

  // 辅助方法
  void clockOneSample();           // 运行 32 个 OPM_Clock，生成一个采样
  void updateTimerPeriods();       // 根据寄存器值更新定时器周期
};
