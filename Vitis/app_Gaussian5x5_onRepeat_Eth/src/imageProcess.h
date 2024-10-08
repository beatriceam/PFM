#ifndef IMGPROCESS_H_
#define IMGPROCESS_H_

#include <stdlib.h>
#include <stdio.h>
#include "xil_types.h"
#include "xil_cache.h"
#include "xscugic.h"
#include "xgpio.h"
#include "xparameters.h"
#include "xaxidma.h"
#include "xaxivdma.h"

#define RDLINECOUNTER_BASEADDR XPAR_O_RDLINECOUNTER_BASEADDR
#define TOTALPIXELCOUNTER_BASEADDR XPAR_O_TOTALPIXELCOUNTER_BASEADDR
#define I_AUX_RESET_BASEADDR XPAR_I_AUX_RESET_BASEADDR

#define FULLSCREEN_H_SIZE 1920
#define FULLSCREEN_V_SIZE 1080
#define FRAME_SIZE FULLSCREEN_H_SIZE*FULLSCREEN_V_SIZE*3

#define IMAGE_H_SIZE 512
#define IMAGE_V_SIZE 512
#define IMAGE_SIZE IMAGE_H_SIZE*IMAGE_V_SIZE

#define O_BUSY_0_INTR 64U

typedef struct{
	char *imageDataPointer;
	char *filteredImageDataPointer;
	XAxiDma *DmaCtrlPointer;
	XScuGic *IntrCtrlPointer;
	u32 imageHSize;
	u32 imageVSize;
	u32 done;
}imgProcess;

int initImgProcessSystem(imgProcess *imgProcessInstance,u32 axiDmaBaseAddress,XScuGic *Intc);
int startImageProcessing(imgProcess *imgProcessInstance);
u32 checkIdle(u32 baseAddress,u32 offset);
int drawImage(u32 displayHSize,u32 displayVSize,u32 imageHSize,u32 imageVSize,u32 hOffset, u32 vOffset,int numColors,char *imagePointer,char *videoFramePointer);

#endif
