/*
 * Copyright (C) 2009-2017 TeraTerm Project
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHORS ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace TeraTrem
{
	partial struct codemap
	{
		/*
		 * Map of Unicode Symbols to Dec Special Characters
		 */
		public static codemap[] mapUnicodeSymbolToDecSp = {
			/*
			 * Latin-1 supplement
			 *   http://www.unicode.org/charts/PDF/U0080.pdf
			 */
			new codemap(0x00B7, 0x047E),	// Middle dot

			/*
			 * General punctuation
			 *   http://www.unicode.org/charts/PDF/U2000.pdf
			 */
			new codemap(0x2022, 0x027E),	// Bullet
			new codemap(0x2024, 0x047E),	// One dot leader
			new codemap(0x2027, 0x027E),	// Hyphenation point

			/*
			 * Mathematical operators
			 *   http://www.unicode.org/charts/PDF/U2200.pdf
			 */
			new codemap(0x2219, 0x047E),	// Bullet operator

			/*
			 * Box drawing
			 *   http://www.unicode.org/charts/PDF/U2500.pdf
			 */
			new codemap(0x2500, 0x0171),	// Box drawings light horizontal
			new codemap(0x2501, 0x0171),	// Box drawings heavy horizontal
			new codemap(0x2502, 0x0178),	// Box drawings light vertical
			new codemap(0x2503, 0x0178),	// Box drawings heavy vertical
			new codemap(0x2504, 0x0171),	// Box drawings light triple dash horizontal
			new codemap(0x2505, 0x0171),	// Box drawings heavy triple dash horizontal
			new codemap(0x2506, 0x0178),	// Box drawings light triple dash vertical
			new codemap(0x2507, 0x0178),	// Box drawings heavy triple dash vertical
			new codemap(0x2508, 0x0171),	// Box drawings light quadruple dash horizontal
			new codemap(0x2509, 0x0171),	// Box drawings heavy quadruple dash horizontal
			new codemap(0x250A, 0x0178),	// Box drawings light quadruple dash vertical
			new codemap(0x250B, 0x0178),	// Box drawings heavy quadruple dash vertical
			new codemap(0x250C, 0x016C),	// Box drawings light down and right
			new codemap(0x250D, 0x016C),	// Box drawings down light and right heavy
			new codemap(0x250E, 0x016C),	// Box drawings down heavy and right light
			new codemap(0x250F, 0x016C),	// Box drawings heavy down and right
			new codemap(0x2510, 0x016B),	// Box drawings light down and left
			new codemap(0x2511, 0x016B),	// Box drawings down light and left heavy
			new codemap(0x2512, 0x016B),	// Box drawings down heavy and left light
			new codemap(0x2513, 0x016B),	// Box drawings heavy down and left
			new codemap(0x2514, 0x016D),	// Box drawings light up and right
			new codemap(0x2515, 0x016D),	// Box drawings up light and right heavy
			new codemap(0x2516, 0x016D),	// Box drawings up heavy and right light
			new codemap(0x2517, 0x016D),	// Box drawings heavy up and right
			new codemap(0x2518, 0x016A),	// Box drawings light up and left
			new codemap(0x2519, 0x016A),	// Box drawings up light and left heavy
			new codemap(0x251A, 0x016A),	// Box drawings up heavy and left light
			new codemap(0x251B, 0x016A),	// Box drawings heavy up and left
			new codemap(0x251C, 0x0174),	// Box drawings light vertical and right
			new codemap(0x251D, 0x0174),	// Box drawings vertical light and right heavy
			new codemap(0x251E, 0x0174),	// Box drawings up heavy and right down light
			new codemap(0x251F, 0x0174),	// Box drawings down heavy and right up light
			new codemap(0x2520, 0x0174),	// Box drawings vertical heavy and right light
			new codemap(0x2521, 0x0174),	// Box drawings down light and right up heavy
			new codemap(0x2522, 0x0174),	// Box drawings up light and right down heavy
			new codemap(0x2523, 0x0174),	// Box drawings heavy vertical and right
			new codemap(0x2524, 0x0175),	// Box drawings light vertical and left
			new codemap(0x2525, 0x0175),	// Box drawings vertical light and left heavy
			new codemap(0x2526, 0x0175),	// Box drawings up heavy and left down light
			new codemap(0x2527, 0x0175),	// Box drawings down heavy and left up light
			new codemap(0x2528, 0x0175),	// Box drawings vertical heavy and left light
			new codemap(0x2529, 0x0175),	// Box drawings down light and left up heavy
			new codemap(0x252A, 0x0175),	// Box drawings up light and left down heavy
			new codemap(0x252B, 0x0175),	// Box drawings heavy vertical and left
			new codemap(0x252C, 0x0177),	// Box drawings light down and horizontal
			new codemap(0x252D, 0x0177),	// Box drawings left heavy and right down light
			new codemap(0x252E, 0x0177),	// Box drawings right heavy and left down light
			new codemap(0x252F, 0x0177),	// Box drawings down light and horizontal heavy
			new codemap(0x2530, 0x0177),	// Box drawings down heavy and horizontal light
			new codemap(0x2531, 0x0177),	// Box drawings right light and left down heavy
			new codemap(0x2532, 0x0177),	// Box drawings left light and right down heavy
			new codemap(0x2533, 0x0177),	// Box drawings heavy down and horizontal
			new codemap(0x2534, 0x0176),	// Box drawings light up and horizontal
			new codemap(0x2535, 0x0176),	// Box drawings left heavy and right up light
			new codemap(0x2536, 0x0176),	// Box drawings right heavy and left up light
			new codemap(0x2537, 0x0176),	// Box drawings up light and horizontal heavy
			new codemap(0x2538, 0x0176),	// Box drawings up heavy and horizontal light
			new codemap(0x2539, 0x0176),	// Box drawings right light and left up heavy
			new codemap(0x253A, 0x0176),	// Box drawings left light and right up heavy
			new codemap(0x253B, 0x0176),	// Box drawings right up and horizontal
			new codemap(0x253C, 0x016e),	// Box drawings light vertical and horizontal
			new codemap(0x253D, 0x016e),	// Box drawings left heavy and right vertical light
			new codemap(0x253E, 0x016e),	// Box drawings right heavy and left vertical light
			new codemap(0x253F, 0x016e),	// Box drawings vertical light and horizontal heavy
			new codemap(0x2540, 0x016e),	// Box drawings up heavy and down horizontal light
			new codemap(0x2541, 0x016e),	// Box drawings down heavy and up horizontal light
			new codemap(0x2542, 0x016e),	// Box drawings vertical heavy and horizontal light
			new codemap(0x2543, 0x016e),	// Box drawings left up heavy and right down light
			new codemap(0x2544, 0x016e),	// Box drawings right up heavy and left down light
			new codemap(0x2545, 0x016e),	// Box drawings left down heavy and right up light
			new codemap(0x2546, 0x016e),	// Box drawings right down heavy and left up light
			new codemap(0x2547, 0x016e),	// Box drawings down light and up horizontal heavy
			new codemap(0x2548, 0x016e),	// Box drawings up light and up horizontal heavy
			new codemap(0x2549, 0x016e),	// Box drawings right light and left vertical heavy
			new codemap(0x254A, 0x016e),	// Box drawings left light and right vertical heavy
			new codemap(0x254B, 0x016e),	// Box drawings heavy vertical and horizontal
			new codemap(0x254C, 0x0171),	// Box drawings light double dash horizontal
			new codemap(0x254D, 0x0171),	// Box drawings heavy double dash horizontal
			new codemap(0x254E, 0x0178),	// Box drawings light double dash vertical
			new codemap(0x254F, 0x0178),	// Box drawings heavy double dash vertical
			new codemap(0x2550, 0x0171),	// Box drawings double horizontal
			new codemap(0x2551, 0x0178),	// Box drawings double vertical
			new codemap(0x2552, 0x016C),	// Box drawings down single and right double
			new codemap(0x2553, 0x016C),	// Box drawings down double and right single
			new codemap(0x2554, 0x016C),	// Box drawings double down and right
			new codemap(0x2555, 0x016B),	// Box drawings down single and left double
			new codemap(0x2556, 0x016B),	// Box drawings down double and left single
			new codemap(0x2557, 0x016B),	// Box drawings double down and left
			new codemap(0x2558, 0x016D),	// Box drawings up single and right double
			new codemap(0x2559, 0x016D),	// Box drawings up double and right single
			new codemap(0x255A, 0x016D),	// Box drawings double up and right
			new codemap(0x255B, 0x016A),	// Box drawings up single and left double
			new codemap(0x255C, 0x016A),	// Box drawings up double and left single
			new codemap(0x255D, 0x016A),	// Box drawings double up and left
			new codemap(0x255E, 0x0174),	// Box drawings vertical single and right double
			new codemap(0x255F, 0x0174),	// Box drawings vertical double and right single
			new codemap(0x2560, 0x0174),	// Box drawings double vertical and right
			new codemap(0x2561, 0x0175),	// Box drawings vertical single and left double
			new codemap(0x2562, 0x0175),	// Box drawings vertical double and left single
			new codemap(0x2563, 0x0175),	// Box drawings double vertical and left
			new codemap(0x2564, 0x0177),	// Box drawings down single and horizontal double
			new codemap(0x2565, 0x0177),	// Box drawings down double and horizontal single
			new codemap(0x2566, 0x0177),	// Box drawings double down and horizontal
			new codemap(0x2567, 0x0176),	// Box drawings up single and horizontal double
			new codemap(0x2568, 0x0176),	// Box drawings up double and horizontal single
			new codemap(0x2569, 0x0176),	// Box drawings double up and horizontal
			new codemap(0x256A, 0x016E),	// Box drawings double vertical single and horizontal double
			new codemap(0x256B, 0x016E),	// Box drawings double vertical double and horizontal single
			new codemap(0x256C, 0x016E),	// Box drawings double vertical and horizontal
			new codemap(0x256D, 0x016C),	// Box drawings light arc down and right
			new codemap(0x256E, 0x016B),	// Box drawings light arc down and left
			new codemap(0x256F, 0x016A),	// Box drawings light arc up and left
			new codemap(0x2570, 0x016D),	// Box drawings light arc left and right
		/*
			new codemap(0x2571, 0x0000),	// Box drawings light diagonal upper right to lower left
			new codemap(0x2572, 0x0000),	// Box drawings light diagonal upper left to lower right
			new codemap(0x2573, 0x0000),	// Box drawings light diagonal cross
			new codemap(0x2574, 0x0000),	// Box drawings light left
			new codemap(0x2575, 0x0000),	// Box drawings light up
			new codemap(0x2576, 0x0000),	// Box drawings light right
			new codemap(0x2577, 0x0000),	// Box drawings light down
			new codemap(0x2578, 0x0000),	// Box drawings heavy left
			new codemap(0x2579, 0x0000),	// Box drawings heavy up
			new codemap(0x257A, 0x0000),	// Box drawings heavy right
			new codemap(0x257B, 0x0000),	// Box drawings heavy down
		 */
			new codemap(0x257C, 0x0171),	// Box drawings light left and heavy right
			new codemap(0x257D, 0x0178),	// Box drawings light up and heavy down
			new codemap(0x257E, 0x0171),	// Box drawings heavy left and light right
			new codemap(0x257F, 0x0178),	// Box drawings heavy up and light down

			/*
			 * Block Elements and Shade Characters
			 *   http://www.unicode.org/charts/PDF/U2580.pdf
			 */
		//	new codemap(0x2588, 0x0460),	// Full block
			new codemap(0x2591, 0x0261),	// Light shade (25%)
			new codemap(0x2592, 0x0261),	// Medium shade (50%)
			new codemap(0x2593, 0x0261),	// Dark shade (75%)

			/*
			 * Geometric Shapes
			 *   http://www.unicode.org/charts/PDF/U25A0.pdf
			 */
			new codemap(0x25AA, 0x027E),	// Black small square
			new codemap(0x25AE, 0x0260),	// Black vertical rectangle

			/*
			 * Miscellaneous symbols and arrows
			 *   http://www.unicode.org/charts/PDF/U2B00.pdf
			 */
			new codemap(0x2B1D, 0x027E),	// Black very small square
		};
	}
}