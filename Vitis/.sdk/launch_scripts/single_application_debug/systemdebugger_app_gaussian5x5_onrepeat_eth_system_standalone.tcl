connect -url tcp:127.0.0.1:3121
targets -set -nocase -filter {name =~"APU*"}
rst -system
after 3000
targets -set -filter {jtag_cable_name =~ "Digilent JTAG-SMT2 210251A08870" && level==0} -index 1
fpga -file C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth/_ide/bitstream/xsa_gaussian5x5_v1_0_obusy34_all120.bit
targets -set -nocase -filter {name =~"APU*"}
loadhw -hw C:/Users/bazoubel/Documents/workspace_Gaussian5x5_onRepeat_Eth/plat_Gaussian5x5_onRepeat_Eth/export/plat_Gaussian5x5_onRepeat_Eth/hw/xsa_gaussian5x5_v1_0_obusy36_all120.xsa -mem-ranges [list {0x40000000 0xbfffffff}]
configparams force-mem-access 1
targets -set -nocase -filter {name =~"APU*"}
source C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth/_ide/psinit/ps7_init.tcl
ps7_init
ps7_post_config
targets -set -nocase -filter {name =~ "*A9*#0"}
dow C:/Users/bazoubel/Desktop/workspace_Gaussian5x5_onRepeat_Eth/app_Gaussian5x5_onRepeat_Eth/Debug/app_Gaussian5x5_onRepeat_Eth.elf
configparams force-mem-access 0
bpadd -addr &main
