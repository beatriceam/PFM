`timescale 1ns / 1ps

module tb_imageControl3;

    // Inputs
    reg i_clk;
    reg i_rst;
    reg i_aux_reset;
    reg [7:0] i_pixel_data;
    reg i_pixel_data_valid;

    // Outputs
    wire [199:0] o_pixel_data;
    wire o_pixel_data_valid;
    wire o_intr;
    wire o_busy;
    wire [2:0] currentRdLineBuffer;
    wire [8:0] rdLineCounter;
    wire [12:0] totalPixelCounter;
    
    // Instantiate the Unit Under Test (UUT)
    imageControl uut (
        .i_clk(i_clk), 
        .i_rst(i_rst), 
        .i_aux_reset(i_aux_reset), 
        .i_pixel_data(i_pixel_data), 
        .i_pixel_data_valid(i_pixel_data_valid), 
        .o_pixel_data(o_pixel_data), 
        .o_pixel_data_valid(o_pixel_data_valid), 
        .o_intr(o_intr), 
        .o_busy(o_busy),
        .currentRdLineBuffer(currentRdLineBuffer),
        .rdLineCounter(rdLineCounter),
        .totalPixelCounter(totalPixelCounter)
    );

    // Clock generation
    initial begin
        i_clk = 0;
        forever #5 i_clk = ~i_clk; // 100 MHz clock
    end

    // Test stimulus
    initial begin
        // Initialize Inputs
        i_rst = 1;
        i_pixel_data = 0;
        i_pixel_data_valid = 0;
        i_aux_reset = 1;

        // Wait for global reset
        #100;
        i_rst = 0;
        i_aux_reset = 0;
        // Send 6 lines of 512 pixels each before waiting for the first interrupt
        insert_multiple_pixel_data_lines(6);


        // Repeat as needed for more lines of data
        repeat (506) begin
            wait(o_intr);
            #1000;
            insert_pixel_data_line();
        end
        
        
        #100000;
        i_aux_reset = 1;
        #50000;
        i_aux_reset = 0;
        #100000;
        
        // Sending the second image / Envio da segunda imagem
        // Send 6 lines of 512 pixels each before waiting for the first interrupt
        insert_multiple_pixel_data_lines(6);


        // Repeat as needed for more lines of data
        repeat (506) begin
            wait(o_intr);
            #2000;
            insert_pixel_data_line();
        end
        
        #100000;
        i_aux_reset = 1;
        #50000;
        i_aux_reset = 0;
        #100000;
        
        // Sending the third image / Envio da terceir imagem
        // Send 6 lines of 512 pixels each before waiting for the first interrupt
        insert_multiple_pixel_data_lines(6);


        // Repeat as needed for more lines of data
        repeat (506) begin
            wait(o_intr);
            #2000;
            insert_pixel_data_line();
        end
        
    end

    // Task to insert multiple lines of pixel data
    task insert_multiple_pixel_data_lines;
    input integer num_lines;
    integer i, j;
    begin
        for (j = 0; j < num_lines; j = j + 1) begin
            for (i = 0; i < 512; i = i + 1) begin
                @(posedge i_clk);
                i_pixel_data = i % 256;  // Example pixel data, you can customize this
                i_pixel_data_valid <= 1;
            end
            @(posedge i_clk);
            i_pixel_data_valid <= 0; // Deassert the valid signal after each pixel
        end
    end
    endtask

    // Task to insert a single line of pixel data
    task insert_pixel_data_line;
    integer i;
    begin
        for (i = 0; i < 512; i = i + 1) begin
            @(posedge i_clk);
            i_pixel_data = i % 256;  // Example pixel data, you can customize this
            i_pixel_data_valid <= 1;
        end
        @(posedge i_clk);
        i_pixel_data_valid <= 0; // Deassert the valid signal after each pixel
    end
    endtask
      
endmodule
