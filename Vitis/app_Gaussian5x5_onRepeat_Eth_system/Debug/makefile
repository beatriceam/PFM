#
# This file was created by Xilinx Vitis tool.
#


#
# tools
#

RM = rm -f
RMDIR = rm -rf

#
# variables
#

SDCARD_GEN = sdcard_gen
GENERATE_BIF = ::scw::generate_bif

XPFM_PATH = C:/Users/bazoubel/Documents/workspace_Gaussian5x5_onRepeat_Eth/plat_Gaussian5x5_onRepeat_Eth/export/plat_Gaussian5x5_onRepeat_Eth/plat_Gaussian5x5_onRepeat_Eth.xpfm
SYS_CONFIG = plat_Gaussian5x5_onRepeat_Eth
DOMAINS = standalone_domain
BIF_PATH = C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth_system/Debug/system.bif

BITSTREAM_PATH = C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth/_ide/bitstream/xsa_gaussian5x5_v1_0_obusy34_all120.bit
SD_FILES = --elf C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth/Debug/app_Gaussian5x5_onRepeat_Eth.elf,ps7_cortexa9_0

#
# primary build targets
#

all: sd_card

sd_card:
	-@echo Generating bif file for the system project
	generate_system_bif.bat 63317 ${XPFM_PATH} ${DOMAINS} ${BIF_PATH}
	${SDCARD_GEN} --xpfm ${XPFM_PATH} --sys_config ${SYS_CONFIG} --bif ${BIF_PATH} --bitstream ${BITSTREAM_PATH} ${SD_FILES}

clean:
	$(RMDIR) ./_sds
	${RMDIR} sd_card
	${RM} system.bif

.PHONY: all sd_card clean


