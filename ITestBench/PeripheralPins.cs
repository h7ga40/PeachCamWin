/* mbed Microcontroller Library
 * Copyright (c) 2006-2015 ARM Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestBench
{
	public enum UARTName
	{
		UART0,
		UART1,
		UART2,
		UART3,
		UART4,
		UART5,
		UART6,
		UART7,
	}

	public enum PWMName
	{
		PWM_PWM1A = 0,
		PWM_PWM1B,
		PWM_PWM1C,
		PWM_PWM1D,
		PWM_PWM1E,
		PWM_PWM1F,
		PWM_PWM1G,
		PWM_PWM1H,
		PWM_PWM2A,
		PWM_PWM2B,
		PWM_PWM2C,
		PWM_PWM2D,
		PWM_PWM2E,
		PWM_PWM2F,
		PWM_PWM2G,
		PWM_PWM2H,
		PWM_TIOC0A = 0x20,
		PWM_TIOC0C,
		PWM_TIOC1A,
		PWM_TIOC2A,
		PWM_TIOC3A,
		PWM_TIOC3C,
		PWM_TIOC4A,
		PWM_TIOC4C,
	}

	public enum ADCName
	{
		AN0 = 0,
		AN1 = 1,
		AN2 = 2,
		AN3 = 3,
		AN4 = 4,
		AN5 = 5,
		AN6 = 6,
		AN7 = 7,
	}

	public enum DACName
	{
	}

	public enum SPIName
	{
		SPI_0 = 0,
		SPI_1,
		SPI_2,
		SPI_3,
		SPI_4,
	}

	public enum I2CName
	{
		I2C_0 = 0,
		I2C_1,
		I2C_2,
		I2C_3,
	}

	public enum CANName
	{
		CAN_0 = 0,
		CAN_1,
		CAN_2,
		CAN_3,
		CAN_4,
	}

	public enum IRQNo
	{
		IRQ0, IRQ1,
		IRQ2, IRQ3,
		IRQ4, IRQ5,
		IRQ6, IRQ7,
	}

	public enum I2SName
	{
		I2S0,
	}

	//[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct PinFunc
	{
		PinName pin;
		int function;
		int pm;

		public PinFunc(PinName pin, int function, int pm)
		{
			this.pin = pin;
			this.function = function;
			this.pm = pm;
		}

		/************PINMAP***************/
		public static PinFunc[] PIPC_0_tbl = {
		//   pin      func     pm
			new PinFunc(PinName.P4_0   ,  2      ,  -1), /* TIOC0A */
			new PinFunc(PinName.P5_0   ,  6      ,  -1), /* TIOC0A */
			new PinFunc(PinName.P7_0   ,  7      ,  -1), /* TIOC0A */
			new PinFunc(PinName.P10_4  ,  2      ,  -1), /* TIOC0A */
			new PinFunc(PinName.P4_1   ,  2      ,  -1), /* TIOC0B */
			new PinFunc(PinName.P5_1   ,  6      ,  -1), /* TIOC0B */
			new PinFunc(PinName.P7_1   ,  7      ,  -1), /* TIOC0B */
			new PinFunc(PinName.P10_5  ,  2      ,  -1), /* TIOC0B */
			new PinFunc(PinName.P4_2   ,  2      ,  -1), /* TIOC0C */
			new PinFunc(PinName.P5_5   ,  6      ,  -1), /* TIOC0C */
			new PinFunc(PinName.P7_2   ,  7      ,  -1), /* TIOC0C */
			new PinFunc(PinName.P10_6  ,  2      ,  -1), /* TIOC0C */
			new PinFunc(PinName.P4_3   ,  2      ,  -1), /* TIOC0D */
			new PinFunc(PinName.P5_7   ,  6      ,  -1), /* TIOC0D */
			new PinFunc(PinName.P7_3   ,  7      ,  -1), /* TIOC0D */
			new PinFunc(PinName.P10_7  ,  2      ,  -1), /* TIOC0D */
			new PinFunc(PinName.P2_11  ,  5      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P6_0   ,  5      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P7_4   ,  7      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P8_8   ,  5      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P9_7   ,  4      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P10_8  ,  2      ,  -1), /* TIOC1A */
			new PinFunc(PinName.P2_12  ,  8      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P5_2   ,  6      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P6_1   ,  5      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P7_5   ,  7      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P8_9   ,  5      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P10_9  ,  2      ,  -1), /* TIOC1B */
			new PinFunc(PinName.P2_1   ,  6      ,  -1), /* TIOC2A */
			new PinFunc(PinName.P6_2   ,  6      ,  -1), /* TIOC2A */
			new PinFunc(PinName.P7_6   ,  7      ,  -1), /* TIOC2A */
			new PinFunc(PinName.P8_14  ,  4      ,  -1), /* TIOC2A */
			new PinFunc(PinName.P10_10 ,  2      ,  -1), /* TIOC2A */
			new PinFunc(PinName.P2_2   ,  6      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P6_3   ,  6      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P7_7   ,  7      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P8_15  ,  4      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P10_11 ,  2      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P10_11 ,  2      ,  -1), /* TIOC2B */
			new PinFunc(PinName.P3_4   ,  6      ,  -1), /* TIOC3A */
			new PinFunc(PinName.P7_8   ,  7      ,  -1), /* TIOC3A */
			new PinFunc(PinName.P8_10  ,  4      ,  -1), /* TIOC3A */
			new PinFunc(PinName.P3_5   ,  6      ,  -1), /* TIOC3B */
			new PinFunc(PinName.P7_9   ,  7      ,  -1), /* TIOC3B */
			new PinFunc(PinName.P8_11  ,  4      ,  -1), /* TIOC3B */
			new PinFunc(PinName.P3_6   ,  6      ,  -1), /* TIOC3C */
			new PinFunc(PinName.P5_3   ,  6      ,  -1), /* TIOC3C */
			new PinFunc(PinName.P7_10  ,  7      ,  -1), /* TIOC3C */
			new PinFunc(PinName.P8_12  ,  4      ,  -1), /* TIOC3C */
			new PinFunc(PinName.P3_7   ,  6      ,  -1), /* TIOC3D */
			new PinFunc(PinName.P5_4   ,  6      ,  -1), /* TIOC3D */
			new PinFunc(PinName.P7_11  ,  7      ,  -1), /* TIOC3D */
			new PinFunc(PinName.P8_13  ,  4      ,  -1), /* TIOC3D */
			new PinFunc(PinName.P3_8   ,  6      ,  -1), /* TIOC4A */
			new PinFunc(PinName.P4_4   ,  3      ,  -1), /* TIOC4A */
			new PinFunc(PinName.P7_12  ,  7      ,  -1), /* TIOC4A */
			new PinFunc(PinName.P11_0  ,  2      ,  -1), /* TIOC4A */
			new PinFunc(PinName.P3_9   ,  6      ,  -1), /* TIOC4B */
			new PinFunc(PinName.P4_5   ,  3      ,  -1), /* TIOC4B */
			new PinFunc(PinName.P7_13  ,  7      ,  -1), /* TIOC4B */
			new PinFunc(PinName.P11_1  ,  2      ,  -1), /* TIOC4B */
			new PinFunc(PinName.P3_10  ,  6      ,  -1), /* TIOC4C */
			new PinFunc(PinName.P4_6   ,  3      ,  -1), /* TIOC4C */
			new PinFunc(PinName.P7_14  ,  7      ,  -1), /* TIOC4C */
			new PinFunc(PinName.P11_2  ,  2      ,  -1), /* TIOC4C */
			new PinFunc(PinName.P3_11  ,  6      ,  -1), /* TIOC4D */
			new PinFunc(PinName.P4_7   ,  3      ,  -1), /* TIOC4D */
			new PinFunc(PinName.P7_15  ,  7      ,  -1), /* TIOC4D */
			new PinFunc(PinName.P11_3  ,  2      ,  -1), /* TIOC4D */
			new PinFunc(PinName.P5_7   ,  1      ,  1 ), /* TXOUT0M   */
			new PinFunc(PinName.P5_6   ,  1      ,  1 ), /* TXOUT0P   */
			new PinFunc(PinName.P5_5   ,  1      ,  1 ), /* TXOUT1M   */
			new PinFunc(PinName.P5_4   ,  1      ,  1 ), /* TXOUT1P   */
			new PinFunc(PinName.P5_3   ,  1      ,  1 ), /* TXOUT2M   */
			new PinFunc(PinName.P5_2   ,  1      ,  1 ), /* TXOUT2P   */
			new PinFunc(PinName.P5_1   ,  1      ,  1 ), /* TXCLKOUTM */
			new PinFunc(PinName.P5_0   ,  1      ,  1 ), /* TXCLKOUTP */
			new PinFunc(PinName.P2_11  ,  4      ,  0 ), /* SSITxD0 */
			new PinFunc(PinName.P4_7   ,  5      ,  0 ), /* SSITxD0 */
			new PinFunc(PinName.P7_4   ,  6      ,  0 ), /* SSITxD1 */
			new PinFunc(PinName.P10_15 ,  2      ,  0 ), /* SSITxD1 */
			new PinFunc(PinName.P4_15  ,  6      ,  0 ), /* SSITxD3 */
			new PinFunc(PinName.P7_11  ,  2      ,  0 ), /* SSITxD3 */
			new PinFunc(PinName.P2_7   ,  4      ,  0 ), /* SSITxD5 */
			new PinFunc(PinName.P4_11  ,  5      ,  0 ), /* SSITxD5 */
			new PinFunc(PinName.P8_10  ,  8      ,  0 ), /* SSITxD5 */
			new PinFunc(PinName.P3_7   ,  8      ,  0 ), /* WDTOVF */
			new PinFunc(PinName.NC     ,  0      ,  -1)
		};
	}

	//[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct PinMap
	{
		PinName pin;
		int peripheral;
		int function;

		public PinMap(PinName pin, int peripheral, int function)
		{
			this.pin = pin;
			this.peripheral = (int)peripheral;
			this.function = function;
		}

		public PinMap(PinName pin, PinName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, IRQNo peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, UARTName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, PWMName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, ADCName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, SPIName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, I2CName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, CANName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public PinMap(PinName pin, I2SName peripheral, int function) :
			this(pin, (int)peripheral, function)
		{
		}

		public static PinMap[] IRQ = {
			new PinMap(PinName.P1_0,  IRQNo.IRQ0, 4), new PinMap(PinName.P1_1,  IRQNo.IRQ1, 4), new PinMap(PinName.P1_2,  IRQNo.IRQ2, 4),
			new PinMap(PinName.P1_3,  IRQNo.IRQ3, 4), new PinMap(PinName.P1_4,  IRQNo.IRQ4, 4), new PinMap(PinName.P1_5,  IRQNo.IRQ5, 4),
			new PinMap(PinName.P1_6,  IRQNo.IRQ6, 4), new PinMap(PinName.P1_7,  IRQNo.IRQ7, 4), new PinMap(PinName.P1_8,  IRQNo.IRQ2, 3),
			new PinMap(PinName.P1_9,  IRQNo.IRQ3, 3), new PinMap(PinName.P1_10, IRQNo.IRQ4, 3), new PinMap(PinName.P1_11, IRQNo.IRQ5, 3), // 11
			new PinMap(PinName.P2_0,  IRQNo.IRQ5, 6), new PinMap(PinName.P2_12, IRQNo.IRQ6, 6), new PinMap(PinName.P2_13, IRQNo.IRQ7, 8),
			new PinMap(PinName.P2_14, IRQNo.IRQ0, 8), new PinMap(PinName.P2_15, IRQNo.IRQ1, 8), // 16
			new PinMap(PinName.P3_0,  IRQNo.IRQ2, 3), new PinMap(PinName.P3_1,  IRQNo.IRQ6, 3), new PinMap(PinName.P3_3,  IRQNo.IRQ4, 3),
			new PinMap(PinName.P3_9,  IRQNo.IRQ6, 8), // 20
			new PinMap(PinName.P4_8,  IRQNo.IRQ0, 8), new PinMap(PinName.P4_9,  IRQNo.IRQ1, 8), new PinMap(PinName.P4_10, IRQNo.IRQ2, 8),
			new PinMap(PinName.P4_11, IRQNo.IRQ3, 8), new PinMap(PinName.P4_12, IRQNo.IRQ4, 8), new PinMap(PinName.P4_13, IRQNo.IRQ5, 8),
			new PinMap(PinName.P4_14, IRQNo.IRQ6, 8), new PinMap(PinName.P4_15, IRQNo.IRQ7, 8), // 28
			new PinMap(PinName.P5_6,  IRQNo.IRQ6, 6), new PinMap(PinName.P5_8,  IRQNo.IRQ0, 2), new PinMap(PinName.P5_9,  IRQNo.IRQ2, 4), // 31
			new PinMap(PinName.P6_0,  IRQNo.IRQ5, 6), new PinMap(PinName.P6_1,  IRQNo.IRQ4, 4), new PinMap(PinName.P6_2,  IRQNo.IRQ7, 4),
			new PinMap(PinName.P6_3,  IRQNo.IRQ2, 4), new PinMap(PinName.P6_4,  IRQNo.IRQ3, 4), new PinMap(PinName.P6_8,  IRQNo.IRQ0, 8),
			new PinMap(PinName.P6_9,  IRQNo.IRQ1, 8), new PinMap(PinName.P6_10, IRQNo.IRQ2, 8), new PinMap(PinName.P6_11, IRQNo.IRQ3, 8),
			new PinMap(PinName.P6_12, IRQNo.IRQ4, 8), new PinMap(PinName.P6_13, IRQNo.IRQ5, 8), new PinMap(PinName.P6_14, IRQNo.IRQ6, 8),
			new PinMap(PinName.P6_15, IRQNo.IRQ7, 8), // 44
			new PinMap(PinName.P7_8,  IRQNo.IRQ1, 8), new PinMap(PinName.P7_9,  IRQNo.IRQ0, 8), new PinMap(PinName.P7_10, IRQNo.IRQ2, 8),
			new PinMap(PinName.P7_11, IRQNo.IRQ3, 8), new PinMap(PinName.P7_12, IRQNo.IRQ4, 8), new PinMap(PinName.P7_13, IRQNo.IRQ5, 8),
			new PinMap(PinName.P7_14, IRQNo.IRQ6, 8), // 51
			new PinMap(PinName.P8_2,  IRQNo.IRQ0, 5), new PinMap(PinName.P8_3,  IRQNo.IRQ1, 6), new PinMap(PinName.P8_7,  IRQNo.IRQ5, 4),
			new PinMap(PinName.P9_1,  IRQNo.IRQ0, 4), // 55
			new PinMap(PinName.P11_12,IRQNo.IRQ3, 3), new PinMap(PinName.P11_15,IRQNo.IRQ1, 3), // 57
			new PinMap(PinName.NC,    PinName.NC,   0)
		};

		public static PinMap[] PinMap_ADC = {
			new PinMap(PinName.P1_8,  ADCName.AN0, 1),
			new PinMap(PinName.P1_9,  ADCName.AN1, 1),
			new PinMap(PinName.P1_10, ADCName.AN2, 1),
			new PinMap(PinName.P1_11, ADCName.AN3, 1),
			new PinMap(PinName.P1_12, ADCName.AN4, 1),
			new PinMap(PinName.P1_13, ADCName.AN5, 1),
			new PinMap(PinName.P1_14, ADCName.AN6, 1),
			new PinMap(PinName.P1_15, ADCName.AN7, 1),
			new PinMap(PinName.NC   , PinName.NC , 0)
		};

		public static PinMap[] PinMap_DAC = {
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2C_SDA = {
			new PinMap(PinName.P1_1 , I2CName.I2C_0, 1),
			new PinMap(PinName.P1_3 , I2CName.I2C_1, 1),
			new PinMap(PinName.P1_7 , I2CName.I2C_3, 1),
			new PinMap(PinName.NC   , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2C_SCL = {
			new PinMap(PinName.P1_0 , I2CName.I2C_0, 1),
			new PinMap(PinName.P1_2 , I2CName.I2C_1, 1),
			new PinMap(PinName.P1_6 , I2CName.I2C_3, 1),
			new PinMap(PinName.NC   , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_UART_TX = {
			new PinMap(PinName.P2_14 , UARTName.UART0, 6),
			new PinMap(PinName.P2_5  , UARTName.UART1, 6),
			new PinMap(PinName.P4_12 , UARTName.UART1, 7),
			new PinMap(PinName.P6_3  , UARTName.UART2, 7),
			new PinMap(PinName.P4_14 , UARTName.UART2, 7),
			new PinMap(PinName.P5_3  , UARTName.UART3, 5),
			new PinMap(PinName.P8_8  , UARTName.UART3, 7),
			new PinMap(PinName.P5_0  , UARTName.UART4, 5),
			new PinMap(PinName.P8_14 , UARTName.UART4, 7),
			new PinMap(PinName.P8_13 , UARTName.UART5, 5),
			new PinMap(PinName.P11_10, UARTName.UART5, 3),
			new PinMap(PinName.P6_6  , UARTName.UART5, 5),
			new PinMap(PinName.P5_6  , UARTName.UART6, 5),
			new PinMap(PinName.P11_1 , UARTName.UART6, 4),
			new PinMap(PinName.P7_4  , UARTName.UART7, 4),
			new PinMap(PinName.NC    , PinName.NC    , 0)
		};

		public static PinMap[] PinMap_UART_RX = {
			new PinMap(PinName.P2_15 , UARTName.UART0, 6),
			new PinMap(PinName.P2_6  , UARTName.UART1, 6),
			new PinMap(PinName.P4_13 , UARTName.UART1, 7),
			new PinMap(PinName.P6_2  , UARTName.UART2, 7),
			new PinMap(PinName.P4_15 , UARTName.UART2, 7),
			new PinMap(PinName.P5_4  , UARTName.UART3, 5),
			new PinMap(PinName.P8_9  , UARTName.UART3, 7),
			new PinMap(PinName.P5_1  , UARTName.UART4, 5),
			new PinMap(PinName.P8_15 , UARTName.UART4, 7),
			new PinMap(PinName.P8_11 , UARTName.UART5, 5),
			new PinMap(PinName.P11_11, UARTName.UART5, 3),
			new PinMap(PinName.P6_7  , UARTName.UART5, 5),
			new PinMap(PinName.P5_7  , UARTName.UART6, 5),
			new PinMap(PinName.P11_2 , UARTName.UART6, 4),
			new PinMap(PinName.P7_5  , UARTName.UART7, 4),
			new PinMap(PinName.NC    , PinName.NC    , 0)
		};

		public static PinMap[] PinMap_UART_CTS = {
			new PinMap(PinName.P2_3  , UARTName.UART1, 6),
			new PinMap(PinName.P11_7 , UARTName.UART5, 3),
			new PinMap(PinName.P7_6  , UARTName.UART7, 4),
			new PinMap(PinName.NC    , PinName.NC    , 0)
		};

		public static PinMap[] PinMap_UART_RTS = {
			new PinMap(PinName.P2_7  , UARTName.UART1, 6),
			new PinMap(PinName.P11_8 , UARTName.UART5, 3),
			new PinMap(PinName.P7_7  , UARTName.UART7, 4),
			new PinMap(PinName.NC    , PinName.NC    , 0)
		};

		public static PinMap[] PinMap_SPI_SCLK = {
			new PinMap(PinName.P10_12, SPIName.SPI_0, 4),
			new PinMap(PinName.P4_4  , SPIName.SPI_1, 2),
			new PinMap(PinName.P6_4  , SPIName.SPI_1, 7),
			new PinMap(PinName.P11_12, SPIName.SPI_1, 2),
			new PinMap(PinName.P8_3  , SPIName.SPI_2, 3),
			new PinMap(PinName.P5_0  , SPIName.SPI_3, 8),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_SPI_MOSI = {
			new PinMap(PinName.P10_14, SPIName.SPI_0, 4),
			new PinMap(PinName.P4_6  , SPIName.SPI_1, 2),
			new PinMap(PinName.P6_6  , SPIName.SPI_1, 7),
			new PinMap(PinName.P11_14, SPIName.SPI_1, 2),
			new PinMap(PinName.P8_5  , SPIName.SPI_2, 3),
			new PinMap(PinName.P5_2  , SPIName.SPI_3, 8),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_SPI_MISO = {
			new PinMap(PinName.P10_15, SPIName.SPI_0, 4),
			new PinMap(PinName.P4_7  , SPIName.SPI_1, 2),
			new PinMap(PinName.P6_7  , SPIName.SPI_1, 7),
			new PinMap(PinName.P11_15, SPIName.SPI_1, 2),
			new PinMap(PinName.P8_6  , SPIName.SPI_2, 3),
			new PinMap(PinName.P5_3  , SPIName.SPI_3, 8),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_SPI_SSEL = {
			new PinMap(PinName.P10_13, SPIName.SPI_0, 4),
			new PinMap(PinName.P4_5  , SPIName.SPI_1, 2),
			new PinMap(PinName.P6_5  , SPIName.SPI_1, 7),
			new PinMap(PinName.P11_13, SPIName.SPI_1, 2),
			new PinMap(PinName.P8_4  , SPIName.SPI_2, 3),
			new PinMap(PinName.P5_1  , SPIName.SPI_3, 8),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_PWM = {
			new PinMap(PinName.P2_1  , PWMName.PWM_TIOC2A, 6),
			new PinMap(PinName.P2_11 , PWMName.PWM_TIOC1A, 5),
			new PinMap(PinName.P3_8  , PWMName.PWM_TIOC4A, 6),
			new PinMap(PinName.P3_10 , PWMName.PWM_TIOC4C, 6),
			new PinMap(PinName.P4_0  , PWMName.PWM_TIOC0A, 2),
			new PinMap(PinName.P4_4  , PWMName.PWM_TIOC4A, 3),
			new PinMap(PinName.P4_6  , PWMName.PWM_TIOC4C, 3),
			new PinMap(PinName.P5_0  , PWMName.PWM_TIOC0A, 6),
			new PinMap(PinName.P5_5  , PWMName.PWM_TIOC0C, 6),
			new PinMap(PinName.P7_2  , PWMName.PWM_TIOC0C, 7),
			new PinMap(PinName.P7_4  , PWMName.PWM_TIOC1A, 7),
			new PinMap(PinName.P7_6  , PWMName.PWM_TIOC2A, 7),
			new PinMap(PinName.P7_12 , PWMName.PWM_TIOC4A, 7),
			new PinMap(PinName.P7_14 , PWMName.PWM_TIOC4C, 7),
			new PinMap(PinName.P8_8  , PWMName.PWM_TIOC1A, 5),
			new PinMap(PinName.P8_14 , PWMName.PWM_TIOC2A, 4),
			new PinMap(PinName.P11_0 , PWMName.PWM_TIOC4A, 2),
			new PinMap(PinName.P11_2 , PWMName.PWM_TIOC4C, 2),
			new PinMap(PinName.P4_4  , PWMName.PWM_PWM2E , 4),
			new PinMap(PinName.P3_2  , PWMName.PWM_PWM2C , 7),
			new PinMap(PinName.P4_6  , PWMName.PWM_PWM2G , 4),
			new PinMap(PinName.P4_7  , PWMName.PWM_PWM2H , 4),
			new PinMap(PinName.P8_14 , PWMName.PWM_PWM1G , 6),
			new PinMap(PinName.P8_15 , PWMName.PWM_PWM1H , 6),
			new PinMap(PinName.P8_13 , PWMName.PWM_PWM1F , 6),
			new PinMap(PinName.P8_11 , PWMName.PWM_PWM1D , 6),
			new PinMap(PinName.P8_8  , PWMName.PWM_PWM1A , 6),
			new PinMap(PinName.P10_0 , PWMName.PWM_PWM2A , 3),
			new PinMap(PinName.P8_12 , PWMName.PWM_PWM1E , 6),
			new PinMap(PinName.P8_9  , PWMName.PWM_PWM1B , 6),
			new PinMap(PinName.P8_10 , PWMName.PWM_PWM1C , 6),
			new PinMap(PinName.P4_5  , PWMName.PWM_PWM2F , 4),
			new PinMap(PinName.NC    , PinName.NC        , 0)
		};

		public static PinMap[] PinMap_CAN_RD = {
			new PinMap(PinName.P7_8  , CANName.CAN_0, 4),
			new PinMap(PinName.P9_1  , CANName.CAN_0, 3),
			new PinMap(PinName.P1_4  , CANName.CAN_1, 3),
			new PinMap(PinName.P5_9  , CANName.CAN_1, 5),
			new PinMap(PinName.P7_11 , CANName.CAN_1, 4),
			new PinMap(PinName.P11_12, CANName.CAN_1, 1),
			new PinMap(PinName.P4_9  , CANName.CAN_2, 6),
			new PinMap(PinName.P6_4  , CANName.CAN_2, 3),
			new PinMap(PinName.P7_2  , CANName.CAN_2, 5),
			new PinMap(PinName.P2_12 , CANName.CAN_3, 5),
			new PinMap(PinName.P4_2  , CANName.CAN_3, 4),
			new PinMap(PinName.P1_5  , CANName.CAN_4, 3),
			new PinMap(PinName.P2_14 , CANName.CAN_4, 5),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_CAN_TD = {
			new PinMap(PinName.P7_9  , CANName.CAN_0, 4),
			new PinMap(PinName.P9_0  , CANName.CAN_0, 3),
			new PinMap(PinName.P5_10 , CANName.CAN_1, 5),
			new PinMap(PinName.P7_10 , CANName.CAN_1, 4),
			new PinMap(PinName.P11_13, CANName.CAN_1, 1),
			new PinMap(PinName.P4_8  , CANName.CAN_2, 6),
			new PinMap(PinName.P6_5  , CANName.CAN_2, 3),
			new PinMap(PinName.P7_3  , CANName.CAN_2, 5),
			new PinMap(PinName.P2_13 , CANName.CAN_3, 5),
			new PinMap(PinName.P4_3  , CANName.CAN_3, 4),
			new PinMap(PinName.P4_11 , CANName.CAN_4, 6),
			new PinMap(PinName.P8_10 , CANName.CAN_4, 5),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2S_SCK = {
			new PinMap(PinName.P4_4  , I2SName.I2S0   , 1),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2S_WS = {
			new PinMap(PinName.P4_5  , I2SName.I2S0   , 1),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2S_TX = {
			new PinMap(PinName.P4_7  , I2SName.I2S0   , 1),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2S_RX = {
			new PinMap(PinName.P4_6  , I2SName.I2S0   , 1),
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static PinMap[] PinMap_I2S_AUDIO_CLK = {
			new PinMap(PinName.NC    , PinName.NC   , 0)
		};

		public static void Pinout(PinName pin, PinMap[] map)
		{
			if (pin == PinName.NC) {
				return;
			}

			foreach (var item in map) {
				if (item.pin == pin) {
					PinFunction(pin, item.function);
					PinMode(pin, TestBench.PinMode.PullNone);
					return;
				}
			}
			throw new Exception($"could not pinout {pin}");
		}

		public static int Merge(int a, int b)
		{
			// both are the same (inc both NC)
			if (a == b) {
				return a;
			}

			// one (or both) is not connected
			if (a == (int)PinName.NC) {
				return b;
			}
			if (b == (int)PinName.NC) {
				return a;
			}

			// mis-match error case
			throw new Exception($"pinmap mis-match {a}");
		}

		public static int FindPeripheral(PinName pin, PinMap[] map)
		{
			foreach (var item in map) {
				if (item.pin == pin) {
					return item.peripheral;
				}
			}
			return (int)PinName.NC;
		}

		public static int Peripheral(PinName pin, PinMap[] map)
		{
			int peripheral = (int)PinName.NC;

			if (pin == PinName.NC) {
				return (int)PinName.NC;
			}
			peripheral = FindPeripheral(pin, map);
			if ((int)PinName.NC == peripheral) { // no mapping available
				throw new Exception($"pinmap not found for peripheral {peripheral}");
			}
			return peripheral;
		}

		public static int FindFunction(PinName pin, PinMap[] map)
		{
			foreach (var item in map) {
				if (item.pin == pin) {
					return item.function;
				}
			}
			return (int)PinName.NC;
		}

		public static int Function(PinName pin, PinMap[] map)
		{
			int function = (int)PinName.NC;

			if (pin == PinName.NC) {
				return (int)PinName.NC;
			}
			function = FindFunction(pin, map);
			if ((int)PinName.NC == function) { // no mapping available
				throw new Exception($"pinmap not found for function {function}");
			}
			return function;
		}

		/* If set pin name here, setting of the "pin" is just one time */
		static PinName gpio_multi_guard = PinName.NC;

		public static void PinFunction(PinName pin, int function)
		{
			if (pin == PinName.NC) return;

			int n = ((int)pin) >> 4;
			int bitmask = 1 << (((int)pin) & 0xf);

			if (gpio_multi_guard != pin) {
#if false
				if (function == 0) {
					// means GPIO mode
					*PMC(n) &= ~bitmask;
				}
				else {
					int pipc_data = 1;

					// alt-function mode
					--function;

					if (function & (1 << 2)) { *PFCAE(n) |= bitmask; } else { *PFCAE(n) &= ~bitmask; }
					if (function & (1 << 1)) { *PFCE(n) |= bitmask; } else { *PFCE(n) &= ~bitmask; }
					if (function & (1 << 0)) { *PFC(n) |= bitmask; } else { *PFC(n) &= ~bitmask; }

					foreach (var Pipc_0_func in PinFunc.PIPC_0_tbl) {
						if ((Pipc_0_func->pin == pin) && ((Pipc_0_func->function - 1) == function)) {
							pipc_data = 0;
							if (Pipc_0_func->pm == 0) {
								*PMSR(n) = (bitmask << 16) | 0;
							}
							else if (Pipc_0_func->pm == 1) {
								*PMSR(n) = (bitmask << 16) | bitmask;
							}
							else {
								// Do Nothing
							}
							break;
						}
						Pipc_0_func++;
					}
					if (pipc_data == 1) {
						*PIPC(n) |= bitmask;
					}
					else {
						*PIPC(n) &= ~bitmask;
					}

					if (P1_0 <= pin && pin <= P1_7 && function == 0) {
						*PBDC(n) |= bitmask;
					}
					*PMC(n) |= bitmask;
				}
#endif
			}
			else {
				gpio_multi_guard = PinName.NC;
			}
		}

		public static void PinMode(PinName pin, PinMode pullNone)
		{
			//if (pin == PinName.NC) return;
		}
	}
}
