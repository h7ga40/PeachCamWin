/* mbed Microcontroller Library
 * Copyright (c) 2006-2013 ARM Limited
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
#ifndef MBED_PINNAMES_H
#define MBED_PINNAMES_H

#include "cmsis.h"

#ifdef __cplusplus
extern "C" {
#endif

#define PIN_INPUT PinDirection_PIN_INPUT
#define PIN_OUTPUT PinDirection_PIN_OUTPUT

#define PORT_SHIFT  4

#define P0_0 PinName_P0_0
#define P0_1 PinName_P0_1
#define P0_2 PinName_P0_2
#define P0_3 PinName_P0_3
#define P0_4 PinName_P0_4
#define P0_5 PinName_P0_5
#define _P0_6 PinName__P0_6
#define _P0_7 PinName__P0_7
#define _P0_8 PinName__P0_8
#define _P0_9 PinName__P0_9
#define _P0_10 PinName__P0_10
#define _P0_11 PinName__P0_11
#define _P0_12 PinName__P0_12
#define _P0_13 PinName__P0_13
#define _P0_14 PinName__P0_14
#define _P0_15 PinName__P0_15

#define P1_0 PinName_P1_0
#define P1_1 PinName_P1_1
#define P1_2 PinName_P1_2
#define P1_3 PinName_P1_3
#define P1_4 PinName_P1_4
#define P1_5 PinName_P1_5
#define P1_6 PinName_P1_6
#define P1_7 PinName_P1_7
#define P1_8 PinName_P1_8
#define P1_9 PinName_P1_9
#define P1_10 PinName_P1_10
#define P1_11 PinName_P1_11
#define P1_12 PinName_P1_12
#define P1_13 PinName_P1_13
#define P1_14 PinName_P1_14
#define P1_15 PinName_P1_15
 
#define P2_0 PinName_P2_0
#define P2_1 PinName_P2_1
#define P2_2 PinName_P2_2
#define P2_3 PinName_P2_3
#define P2_4 PinName_P2_4
#define P2_5 PinName_P2_5
#define P2_6 PinName_P2_6
#define P2_7 PinName_P2_7
#define P2_8 PinName_P2_8
#define P2_9 PinName_P2_9
#define P2_10 PinName_P2_10
#define P2_11 PinName_P2_11
#define P2_12 PinName_P2_12
#define P2_13 PinName_P2_13
#define P2_14 PinName_P2_14
#define P2_15 PinName_P2_15
 
#define P3_0 PinName_P3_0
#define P3_1 PinName_P3_1
#define P3_2 PinName_P3_2
#define P3_3 PinName_P3_3
#define P3_4 PinName_P3_4
#define P3_5 PinName_P3_5
#define P3_6 PinName_P3_6
#define P3_7 PinName_P3_7
#define P3_8 PinName_P3_8
#define P3_9 PinName_P3_9
#define P3_10 PinName_P3_10
#define P3_11 PinName_P3_11
#define P3_12 PinName_P3_12
#define P3_13 PinName_P3_13
#define P3_14 PinName_P3_14
#define P3_15 PinName_P3_15
 
#define P4_0 PinName_P4_0
#define P4_1 PinName_P4_1
#define P4_2 PinName_P4_2
#define P4_3 PinName_P4_3
#define P4_4 PinName_P4_4
#define P4_5 PinName_P4_5
#define P4_6 PinName_P4_6
#define P4_7 PinName_P4_7
#define P4_8 PinName_P4_8
#define P4_9 PinName_P4_9
#define P4_10 PinName_P4_10
#define P4_11 PinName_P4_11
#define P4_12 PinName_P4_12
#define P4_13 PinName_P4_13
#define P4_14 PinName_P4_14
#define P4_15 PinName_P4_15
 
#define P5_0 PinName_P5_0
#define P5_1 PinName_P5_1
#define P5_2 PinName_P5_2
#define P5_3 PinName_P5_3
#define P5_4 PinName_P5_4
#define P5_5 PinName_P5_5
#define P5_6 PinName_P5_6
#define P5_7 PinName_P5_7
#define P5_8 PinName_P5_8
#define P5_9 PinName_P5_9
#define P5_10 PinName_P5_10
#define P5_11 PinName_P5_11
#define P5_12 PinName_P5_12
#define P5_13 PinName_P5_13
#define P5_14 PinName_P5_14
#define P5_15 PinName_P5_15
 
#define P6_0 PinName_P6_0
#define P6_1 PinName_P6_1
#define P6_2 PinName_P6_2
#define P6_3 PinName_P6_3
#define P6_4 PinName_P6_4
#define P6_5 PinName_P6_5
#define P6_6 PinName_P6_6
#define P6_7 PinName_P6_7
#define P6_8 PinName_P6_8
#define P6_9 PinName_P6_9
#define P6_10 PinName_P6_10
#define P6_11 PinName_P6_11
#define P6_12 PinName_P6_12
#define P6_13 PinName_P6_13
#define P6_14 PinName_P6_14
#define P6_15 PinName_P6_15
 
#define P7_0 PinName_P7_0
#define P7_1 PinName_P7_1
#define P7_2 PinName_P7_2
#define P7_3 PinName_P7_3
#define P7_4 PinName_P7_4
#define P7_5 PinName_P7_5
#define P7_6 PinName_P7_6
#define P7_7 PinName_P7_7
#define P7_8 PinName_P7_8
#define P7_9 PinName_P7_9
#define P7_10 PinName_P7_10
#define P7_11 PinName_P7_11
#define P7_12 PinName_P7_12
#define P7_13 PinName_P7_13
#define P7_14 PinName_P7_14
#define P7_15 PinName_P7_15
 
#define P8_0 PinName_P8_0
#define P8_1 PinName_P8_1
#define P8_2 PinName_P8_2
#define P8_3 PinName_P8_3
#define P8_4 PinName_P8_4
#define P8_5 PinName_P8_5
#define P8_6 PinName_P8_6
#define P8_7 PinName_P8_7
#define P8_8 PinName_P8_8
#define P8_9 PinName_P8_9
#define P8_10 PinName_P8_10
#define P8_11 PinName_P8_11
#define P8_12 PinName_P8_12
#define P8_13 PinName_P8_13
#define P8_14 PinName_P8_14
#define P8_15 PinName_P8_15
 
#define P9_0 PinName_P9_0
#define P9_1 PinName_P9_1
#define P9_2 PinName_P9_2
#define P9_3 PinName_P9_3
#define P9_4 PinName_P9_4
#define P9_5 PinName_P9_5
#define P9_6 PinName_P9_6
#define P9_7 PinName_P9_7
#define P9_8 PinName_P9_8
#define P9_9 PinName_P9_9
#define P9_10 PinName_P9_10
#define P9_11 PinName_P9_11
#define P9_12 PinName_P9_12
#define P9_13 PinName_P9_13
#define P9_14 PinName_P9_14
#define P9_15 PinName_P9_15
 
#define P10_0 PinName_P10_0
#define P10_1 PinName_P10_1
#define P10_2 PinName_P10_2
#define P10_3 PinName_P10_3
#define P10_4 PinName_P10_4
#define P10_5 PinName_P10_5
#define P10_6 PinName_P10_6
#define P10_7 PinName_P10_7
#define P10_8 PinName_P10_8
#define P10_9 PinName_P10_9
#define P10_10 PinName_P10_10
#define P10_11 PinName_P10_11
#define P10_12 PinName_P10_12
#define P10_13 PinName_P10_13
#define P10_14 PinName_P10_14
#define P10_15 PinName_P10_15

#define P11_0 PinName_P11_0
#define P11_1 PinName_P11_1
#define P11_2 PinName_P11_2
#define P11_3 PinName_P11_3
#define P11_4 PinName_P11_4
#define P11_5 PinName_P11_5
#define P11_6 PinName_P11_6
#define P11_7 PinName_P11_7
#define P11_8 PinName_P11_8
#define P11_9 PinName_P11_9
#define P11_10 PinName_P11_10
#define P11_11 PinName_P11_11
#define P11_12 PinName_P11_12
#define P11_13 PinName_P11_13
#define P11_14 PinName_P11_14
#define P11_15 PinName_P11_15
 
// mbed Pin Names
#define LED1 PinName_LED1
#define LED2 PinName_LED2
#define LED3 PinName_LED3
#define LED4 PinName_LED4

#define LED_RED  PinName_LED_RED
#define LED_GREEN PinName_LED_GREEN
#define LED_BLUE PinName_LED_BLUE
#define LED_USER PinName_LED_USER

#define USBTX PinName_USBTX
#define USBRX PinName_USBRX

// Arduiono Pin Names
#define D0 PinName_D0
#define D1 PinName_D1
#define D2 PinName_D2
#define D3 PinName_D3
#define D4 PinName_D4
#define D5 PinName_D5
#define D6 PinName_D6
#define D7 PinName_D7
#define D8 PinName_D8
#define D9 PinName_D9
#define D10 PinName_D10
#define D11 PinName_D11
#define D12 PinName_D12
#define D13 PinName_D13
#define D14 PinName_D14
#define D15 PinName_D15

#define A0 PinName_A0
#define A1 PinName_A1
#define A2 PinName_A2
#define A3 PinName_A3
#define A4 PinName_A4
#define A5 PinName_A5

#define I2C_SCL PinName_I2C_SCL
#define I2C_SDA PinName_I2C_SDA

#define USER_BUTTON0 PinName_USER_BUTTON0
// Standardized button names
#define BUTTON1 PinName_BUTTON1

// Not connected
#define NC PinName_NC

#define PullUp PinMode_PullUp
#define PullDown PinMode_PullDown
#define PullNone PinMode_PullNone
#define OpenDrain PinMode_OpenDrain
#define PullDefault PinMode_PullDefault

#define PINGROUP(pin) (((pin)>>PORT_SHIFT)&0x0f)
#define PINNO(pin) ((pin)&0x0f)

#ifdef __cplusplus
}
#endif

#endif
