#include "opm_delegate.h"
#include "opm_nuked.h"
#include "opm_ymfm.h"

OPM_Delegate *OPM_Delegate::getNuked() {
  return new OPMNuked();
}

OPM_Delegate *OPM_Delegate::getYmfm() {
  return new OPMYmfm();
}
