`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 04/01/2020 10:53:27 AM
// Design Name: 
// Module Name: imageControl
// Project Name: 
// Target Devices: 
// Tool Versions: 
// Description: 
// 
// Dependencies: 
// 
// Revision:
// Revision 0.01 - File Created
// Additional Comments:
// 
//////////////////////////////////////////////////////////////////////////////////

module imageControl(
input                    i_clk,
input                    i_rst,
input                    i_aux_reset,
input [7:0]              i_pixel_data,
input                    i_pixel_data_valid,
output reg [199:0]        o_pixel_data,
output                   o_pixel_data_valid,
output reg               o_intr,
output reg               o_busy,
output reg [2:0]         currentRdLineBuffer,
output reg [8:0]         rdLineCounter,
output reg [12:0]        totalPixelCounter
);

reg [8:0] pixelCounter;
reg [2:0] currentWrLineBuffer;
reg [5:0] lineBuffDataValid;
reg [5:0] lineBuffRdData;
wire [39:0] lb0data;
wire [39:0] lb1data;
wire [39:0] lb2data;
wire [39:0] lb3data;
wire [39:0] lb4data;
wire [39:0] lb5data;
reg [8:0] rdCounter;
reg rd_line_buffer;
reg rdState;
reg current_busy_state;
reg last_busy_state;
reg system_reset;
reg [1:0] resetCounter;

localparam IDLE = 'b0,
           RD_BUFFER = 'b1;

assign o_pixel_data_valid = rd_line_buffer;

always @(*)
begin
    system_reset <= i_aux_reset;
end

always @(posedge i_clk)
begin
    if(i_rst || system_reset)
    begin
        totalPixelCounter <= 13'b0000000000000;
    end
    else
    begin
        if(i_pixel_data_valid & !rd_line_buffer)
            totalPixelCounter <= totalPixelCounter + 1;
        else if(!i_pixel_data_valid & rd_line_buffer)
            totalPixelCounter <= totalPixelCounter - 1;
    end
end

always @(posedge i_clk)
begin
    if(i_rst || system_reset)
    begin
        rdState <= IDLE;
        rd_line_buffer <= 1'b0;
        o_intr <= 1'b0;
    end
    else
    begin
        case(rdState)
            IDLE:begin
                o_intr <= 1'b0;
                if(totalPixelCounter >= 2560)
                begin
                    rd_line_buffer <= 1'b1;
                    rdState <= RD_BUFFER;
                end
            end
            RD_BUFFER:begin
                if(rdCounter == 511)
                begin
                    rdState <= IDLE;
                    rd_line_buffer <= 1'b0;
                    o_intr <= 1'b1;
                end
            end
        endcase
    end
end
    
always @(posedge i_clk)
begin
    if(i_rst || system_reset)
        pixelCounter <= 9'b000000000;
    else 
    begin
        if(i_pixel_data_valid)
            pixelCounter <= pixelCounter + 1;
    end
end


always @(posedge i_clk)
begin
    if(i_rst || system_reset)
        currentWrLineBuffer <= 3'b000;
    else
    begin
        if(pixelCounter == 511 & i_pixel_data_valid)
            if (currentWrLineBuffer == 5)
            begin
                currentWrLineBuffer <= 0;
            end
            else 
            begin
                currentWrLineBuffer <= currentWrLineBuffer+1;            
            end
    end
end


always @(*)
begin
    lineBuffDataValid = 6'b000000;
    lineBuffDataValid[currentWrLineBuffer] = i_pixel_data_valid;
end

always @(posedge i_clk)
begin
    if(i_rst || system_reset)
        rdCounter <= 9'b000000000;
    else 
    begin
        if(rd_line_buffer)
            rdCounter <= rdCounter + 1;
    end
end

always @(posedge i_clk)
begin
    if(i_rst || system_reset)
    begin
        currentRdLineBuffer <= 3'b000;
    end
    else
    begin
        if(rdCounter == 511 & rd_line_buffer)
        begin
            if (currentRdLineBuffer == 5)
            begin
                currentRdLineBuffer <= 0;
            end
            else 
            begin
                currentRdLineBuffer <= currentRdLineBuffer+1;       
            end
        end
    end
end

// Lógica aqui
always @(posedge i_clk)
begin
    if(i_rst || system_reset)
    begin
        rdLineCounter <= 9'b000000000;
        o_busy <= 1'b0;
    end
    else
    begin
        if(rdCounter == 511 & rd_line_buffer)
        begin
            rdLineCounter <= rdLineCounter+1; 
        end
        
        //if(rdLineCounter >= (510))
        if(rdLineCounter == (507) && rdCounter == 511) //512-1-4
        begin
            rdLineCounter <= 0;
        end
        
        if(rdLineCounter > 0 )
        begin
            o_busy<= 1'b1;
        end
        else 
        begin
            o_busy <= 1'b0;            
        end
    end
end


//always @(posedge i_clk)
//begin
//    last_busy_state <= current_busy_state;
//    current_busy_state <= o_busy;
//end

//always @(posedge i_clk)
//begin
//    if (last_busy_state && !current_busy_state)
//    begin
//        system_reset <= 1'b1;
//    end
    
//end



//always @(posedge i_clk)
//begin
//    if(i_rst || resetCounter >= 2)
//    begin
//        system_reset <= 1'b0;
//        resetCounter <= 2'b00;
//    end
//    else 
//    begin
//        if(system_reset)
//            resetCounter <= resetCounter+1;
//    end
//end

always @(*)
begin
    case(currentRdLineBuffer)
        0:begin
            o_pixel_data = {lb4data,lb3data,lb2data,lb1data,lb0data};
        end
        1:begin
            o_pixel_data = {lb5data,lb4data,lb3data,lb2data,lb1data};
        end
        2:begin
            o_pixel_data = {lb0data,lb5data,lb4data,lb3data,lb2data};
        end
        3:begin
            o_pixel_data = {lb1data,lb0data,lb5data,lb4data,lb3data};
        end
        4:begin
            o_pixel_data = {lb2data,lb1data,lb0data,lb5data,lb4data};
        end
        5:begin
            o_pixel_data = {lb3data,lb2data,lb1data,lb0data,lb5data};
        end
    endcase
end

always @(*)
begin
    case(currentRdLineBuffer)
        0:begin
            lineBuffRdData[0] = rd_line_buffer;
            lineBuffRdData[1] = rd_line_buffer;
            lineBuffRdData[2] = rd_line_buffer;
            lineBuffRdData[3] = rd_line_buffer;
            lineBuffRdData[4] = rd_line_buffer;
            lineBuffRdData[5] = 1'b0;
        end
       1:begin
            lineBuffRdData[0] = 1'b0;
            lineBuffRdData[1] = rd_line_buffer;
            lineBuffRdData[2] = rd_line_buffer;
            lineBuffRdData[3] = rd_line_buffer;
            lineBuffRdData[4] = rd_line_buffer;
            lineBuffRdData[5] = rd_line_buffer;
        end
       2:begin
            lineBuffRdData[0] = rd_line_buffer;
            lineBuffRdData[1] = 1'b0;
            lineBuffRdData[2] = rd_line_buffer;
            lineBuffRdData[3] = rd_line_buffer;
            lineBuffRdData[4] = rd_line_buffer;
            lineBuffRdData[5] = rd_line_buffer;
       end  
      3:begin
            lineBuffRdData[0] = rd_line_buffer;
            lineBuffRdData[1] = rd_line_buffer;
            lineBuffRdData[2] = 1'b0;
            lineBuffRdData[3] = rd_line_buffer;
            lineBuffRdData[4] = rd_line_buffer;
            lineBuffRdData[5] = rd_line_buffer;
       end 
      4:begin
            lineBuffRdData[0] = rd_line_buffer;
            lineBuffRdData[1] = rd_line_buffer;
            lineBuffRdData[2] = rd_line_buffer;
            lineBuffRdData[3] = 1'b0;
            lineBuffRdData[4] = rd_line_buffer;
            lineBuffRdData[5] = rd_line_buffer;
       end    
      5:begin
            lineBuffRdData[0] = rd_line_buffer;
            lineBuffRdData[1] = rd_line_buffer;
            lineBuffRdData[2] = rd_line_buffer;
            lineBuffRdData[3] = rd_line_buffer;
            lineBuffRdData[4] = 1'b0;
            lineBuffRdData[5] = rd_line_buffer;
       end       
    endcase
end
    
    
    
lineBuffer lB0(
    .i_clk(i_clk),
    .i_rst(i_rst),
    .i_data(i_pixel_data),
    .i_data_valid(lineBuffDataValid[0]),
    .o_data(lb0data),
    .i_rd_data(lineBuffRdData[0])
 ); 
 
 lineBuffer lB1(
     .i_clk(i_clk),
     .i_rst(i_rst),
     .i_data(i_pixel_data),
     .i_data_valid(lineBuffDataValid[1]),
     .o_data(lb1data),
     .i_rd_data(lineBuffRdData[1])
  ); 
  
  lineBuffer lB2(
      .i_clk(i_clk),
      .i_rst(i_rst),
      .i_data(i_pixel_data),
      .i_data_valid(lineBuffDataValid[2]),
      .o_data(lb2data),
      .i_rd_data(lineBuffRdData[2])
   ); 
   
   lineBuffer lB3(
       .i_clk(i_clk),
       .i_rst(i_rst),
       .i_data(i_pixel_data),
       .i_data_valid(lineBuffDataValid[3]),
       .o_data(lb3data),
       .i_rd_data(lineBuffRdData[3])
    );    
    
   lineBuffer lB4(
       .i_clk(i_clk),
       .i_rst(i_rst),
       .i_data(i_pixel_data),
       .i_data_valid(lineBuffDataValid[4]),
       .o_data(lb4data),
       .i_rd_data(lineBuffRdData[4])
    );    
    
   lineBuffer lB5(
       .i_clk(i_clk),
       .i_rst(i_rst),
       .i_data(i_pixel_data),
       .i_data_valid(lineBuffDataValid[5]),
       .o_data(lb5data),
       .i_rd_data(lineBuffRdData[5])
    );    
endmodule
