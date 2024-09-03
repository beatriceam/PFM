################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
LD_SRCS += \
../src/lscript.ld 

C_SRCS += \
../src/imageProcess.c \
../src/main.c \
../src/platform_zynq.c 

OBJS += \
./src/imageProcess.o \
./src/main.o \
./src/platform_zynq.o 

C_DEPS += \
./src/imageProcess.d \
./src/main.d \
./src/platform_zynq.d 


# Each subdirectory must supply rules for building sources it contributes
src/%.o: ../src/%.c
	@echo 'Building file: $<'
	@echo 'Invoking: ARM v7 gcc compiler'
	arm-none-eabi-gcc -Wall -O0 -g3 -c -fmessage-length=0 -MT"$@" -mcpu=cortex-a9 -mfpu=vfpv3 -mfloat-abi=hard -IC:/Users/bazoubel/Documents/workspace_Gaussian5x5_onRepeat_Eth/plat_Gaussian5x5_onRepeat_Eth/export/plat_Gaussian5x5_onRepeat_Eth/sw/plat_Gaussian5x5_onRepeat_Eth/standalone_domain/bspinclude/include -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


