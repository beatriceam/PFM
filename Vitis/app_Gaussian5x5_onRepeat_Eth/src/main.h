#ifndef MAIN_H_
#define MAIN_H_
#include <stdbool.h>
#include <stdint.h>

#include "lwip/tcp.h"
#include "lwip/err.h"

#include "images.h"
#include "imageProcess.h"


#define RECV_BUFFER_SIZE  IMAGE_SIZE  // Tamanho do buffer de recep��o
#define SEND_BUFFER_SIZE  IMAGE_SIZE  // Tamanho do buffer de envio
#define MAX_DATA_SEND 8192
#define EXPECTED_SIZE IMAGE_SIZE

enum ProcessState {
    INIT_PROCESS,
    WAIT_AFTER_START_FIRST,
    DRAW_FIRST_IMAGE,
    WAIT_FIRST_IMAGE,
    DRAW_FIRST_FILTERED_IMAGE,
    WAIT_FIRST_FILTERED_IMAGE,
    PROCESS_SECOND_IMAGE,
    WAIT_AFTER_START_SECOND,
    DRAW_SECOND_IMAGE,
    WAIT_SECOND_IMAGE,
    DRAW_SECOND_FILTERED_IMAGE,
    WAIT_SECOND_FILTERED_IMAGE,
    RESET_PROCESS,
    WAIT_AFTER_RESET
};

void resetDMA(XAxiDma *dmaInstance);
int initIntrController(XScuGic *Intc);
static void ReadErrorCallBack(void *CallbackRef, u32 Mask);
static void ReadCallBack(void *CallbackRef, u32 Mask);
static int SetupVideoIntrSystem(XAxiVdma *AxiVdmaPtr, u16 ReadIntrId, XScuGic *Intc);
void resetAndStartProcessing(imgProcess *imgProcessInstance,XGpio *gpioReset, char *imageData);
void print_app_header();
int start_application();
int transfer_data();
void tcp_fasttmr(void);
void tcp_slowtmr(void);
void lwip_init();
void start_transfer();

uint8_t data_send[MAX_DATA_SEND];

char recv_data_buffer[RECV_BUFFER_SIZE];
char send_data_buffer[SEND_BUFFER_SIZE];
unsigned int received_bytes = 0;
unsigned int send_index = 0;
bool receiving = false;
bool transfering = false;
struct tcp_pcb *global_tpcb;
XGpio rdLineCounterGpio;
XGpio totalPixelCounterGpio;
XGpio auxResetGpio;
u32 rdLineCounterValue;
u32 totalPixelCounterValue;
char Buffer[FRAME_SIZE];
extern volatile int TcpFastTmrFlag;
extern volatile int TcpSlowTmrFlag;
static struct netif server_netif;
struct netif *echo_netif;
int active_connections = 0;
int data_received = 0;
int buffer_full_timeout = 0;  // Timeout para buffer cheio
const int max_buffer_full_cycles = 500000;  // Limite de ciclos de timeout


#endif
