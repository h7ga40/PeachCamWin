// -*- mode:c++; tab-width:2; indent-tabs-mode:nil; c-basic-offset:2 -*-
/*
 *  Copyright 2010 ZXing authors All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include "Code93Reader.h"
#include <zxing/oned/OneDResultPoint.h>
#include <zxing/common/Array.h>
#include <zxing/ReaderException.h>
#include <zxing/FormatException.h>
#include <zxing/NotFoundException.h>
#include <zxing/ChecksumException.h>
#include <math.h>
#include <limits.h>

using std::vector;
using std::string;
using zxing::Ref;
using zxing::Result;
using zxing::String;
using zxing::NotFoundException;
using zxing::ChecksumException;
using zxing::oned::Code93Reader;

// VC++
using zxing::BitArray;

namespace {
char const ALPHABET[] =
"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%abcd*";
string ALPHABET_STRING(ALPHABET);

/**
 * These represent the encodings of characters, as patterns of wide and narrow bars.
 * The 9 least-significant bits of each int correspond to the pattern of wide and narrow.
 */
int const CHARACTER_ENCODINGS[] = {
  0x114, 0x148, 0x144, 0x142, 0x128, 0x124, 0x122, 0x150, 0x112, 0x10A, // 0-9
  0x1A8, 0x1A4, 0x1A2, 0x194, 0x192, 0x18A, 0x168, 0x164, 0x162, 0x134, // A-J
  0x11A, 0x158, 0x14C, 0x146, 0x12C, 0x116, 0x1B4, 0x1B2, 0x1AC, 0x1A6, // K-T
  0x196, 0x19A, 0x16C, 0x166, 0x136, 0x13A, // U-Z
  0x12E, 0x1D4, 0x1D2, 0x1CA, 0x16E, 0x176, 0x1AE, // - - %
  0x126, 0x1DA, 0x1D6, 0x132, 0x15E, // Control chars? $-*
};
int const CHARACTER_ENCODINGS_LENGTH =
(int)sizeof(CHARACTER_ENCODINGS) / sizeof(CHARACTER_ENCODINGS[0]);
const int ASTERISK_ENCODING = CHARACTER_ENCODINGS[47];
}

Code93Reader::Code93Reader()
{
	decodeRowResult.reserve(20);
	counters.resize(6);
}

int Code93Reader::decodeRow(int rowNumber, Ref<BitArray> row, Ref<Result> &rresult)
{
	int ret;
	Range start;
	if ((ret = findAsteriskPattern(row, start)) < 0)
		return ret;
	  // Read off white space    
	int nextStart = row->getNextSet(start[1]);
	int end = row->getSize();

	vector<int> &theCounters(counters);
	{ // Arrays.fill(counters, 0);
		int size = theCounters.size();
		theCounters.resize(0);
		theCounters.resize(size); }
	string &result(decodeRowResult);
	result.clear();

	char decodedChar;
	int lastStart;
	do {
		if ((ret = recordPattern(row, nextStart, theCounters)) < 0)
			return ret;
		int pattern = toPattern(theCounters);
		if (pattern < 0) {
			return -1;
		}
		decodedChar = patternToChar(pattern);
		result.append(1, decodedChar);
		lastStart = nextStart;
		for (int i = 0, e = theCounters.size(); i < e; ++i) {
			nextStart += theCounters[i];
		}
		// Read off white space
		nextStart = row->getNextSet(nextStart);
	} while (decodedChar != '*');
	result.resize(result.length() - 1); // remove asterisk

	// Look for whitespace after pattern:
	int lastPatternSize = 0;
	for (int i = 0, e = theCounters.size(); i < e; i++) {
		lastPatternSize += theCounters[i];
	}

	// Should be at least one more black module
	if (nextStart == end || !row->get(nextStart)) {
		return -1;
	}

	if (result.length() < 2) {
	  // false positive -- need at least 2 checksum digits
		return -1;
	}

	checkChecksums(result);
	// Remove checksum digits
	result.resize(result.length() - 2);

	Ref<String> resultString;
	if ((ret = decodeExtended(result, resultString)) < 0)
		return ret;

	float left = (float)(start[1] + start[0]) / 2.0f;
	float right = lastStart + lastPatternSize / 2.0f;

	ArrayRef< Ref<ResultPoint> > resultPoints(2);
	resultPoints[0] =
		Ref<OneDResultPoint>(new OneDResultPoint(left, (float)rowNumber));
	resultPoints[1] =
		Ref<OneDResultPoint>(new OneDResultPoint(right, (float)rowNumber));

	rresult = new Result(
		resultString,
		ArrayRef<char>(),
		resultPoints,
		BarcodeFormat::CODE_93);
	return 0;
}

int Code93Reader::findAsteriskPattern(Ref<BitArray> row, Code93Reader::Range &result)
{
	int width = row->getSize();
	int rowOffset = row->getNextSet(0);

	{ // Arrays.fill(counters, 0);
		int size = counters.size();
		counters.resize(0);
		counters.resize(size); }
	vector<int> &theCounters(counters);

	int patternStart = rowOffset;
	bool isWhite = false;
	int patternLength = theCounters.size();

	int counterPosition = 0;
	for (int i = rowOffset; i < width; i++) {
		if (row->get(i) ^ isWhite) {
			theCounters[counterPosition]++;
		}
		else {
			if (counterPosition == patternLength - 1) {
				if (toPattern(theCounters) == ASTERISK_ENCODING) {
					result = Range(patternStart, i);
					return 0;
				}
				patternStart += theCounters[0] + theCounters[1];
				for (int y = 2; y < patternLength; y++) {
					theCounters[y - 2] = theCounters[y];
				}
				theCounters[patternLength - 2] = 0;
				theCounters[patternLength - 1] = 0;
				counterPosition--;
			}
			else {
				counterPosition++;
			}
			theCounters[counterPosition] = 1;
			isWhite = !isWhite;
		}
	}
	return -1;
}

int Code93Reader::toPattern(vector<int> &counters)
{
	int max = counters.size();
	int sum = 0;
	for (int i = 0, e = counters.size(); i < e; ++i) {
		sum += counters[i];
	}
	int pattern = 0;
	for (int i = 0; i < max; i++) {
		int scaled = int(counters[i] * 9.0f / sum);
		if (scaled < 1 || scaled > 4) {
			return -1;
		}
		if ((i & 0x01) == 0) {
			for (int j = 0; j < scaled; j++) {
				pattern = (pattern << 1) | 0x01;
			}
		}
		else {
			pattern <<= scaled;
		}
	}
	return pattern;
}

char Code93Reader::patternToChar(int pattern)
{
	for (int i = 0; i < CHARACTER_ENCODINGS_LENGTH; i++) {
		if (CHARACTER_ENCODINGS[i] == pattern) {
			return ALPHABET[i];
		}
	}
	return -1;
}

int Code93Reader::decodeExtended(string const &encoded, Ref<String> &result)
{
	int length = encoded.length();
	string decoded;
	for (int i = 0; i < length; i++) {
		char c = encoded[i];
		if (c >= 'a' && c <= 'd') {
			if (i >= length - 1) {
				return -1;
			}
			char next = encoded[i + 1];
			char decodedChar = '\0';
			switch (c) {
			case 'd':
			  // +A to +Z map to a to z
				if (next >= 'A' && next <= 'Z') {
					decodedChar = (char)(next + 32);
				}
				else {
					return -1;
				}
				break;
			case 'a':
			  // $A to $Z map to control codes SH to SB
				if (next >= 'A' && next <= 'Z') {
					decodedChar = (char)(next - 64);
				}
				else {
					return -1;
				}
				break;
			case 'b':
			  // %A to %E map to control codes ESC to US
				if (next >= 'A' && next <= 'E') {
					decodedChar = (char)(next - 38);
				}
				else if (next >= 'F' && next <= 'J') {
			   // %F to %J map to ; < = > ?
					decodedChar = (char)(next - 11);
				}
				else if (next >= 'K' && next <= 'O') {
			   // %K to %O map to [ \ ] ^ _
					decodedChar = (char)(next + 16);
				}
				else if (next >= 'P' && next <= 'S') {
			   // %P to %S map to { | } ~
					decodedChar = (char)(next + 43);
				}
				else if (next >= 'T' && next <= 'Z') {
			   // %T to %Z all map to DEL (127)
					decodedChar = 127;
				}
				else {
					return -1;
				}
				break;
			case 'c':
			  // /A to /O map to ! to , and /Z maps to :
				if (next >= 'A' && next <= 'O') {
					decodedChar = (char)(next - 32);
				}
				else if (next == 'Z') {
					decodedChar = ':';
				}
				else {
					return -1;
				}
				break;
			}
			decoded.append(1, decodedChar);
			// bump up i again since we read two characters
			i++;
		}
		else {
			decoded.append(1, c);
		}
	}
	result = new String(decoded);
	return 0;
}

int Code93Reader::checkChecksums(string const &result)
{
	int length = result.length();
	if (checkOneChecksum(result, length - 2, 20) < 0)
		return -1;
	if (checkOneChecksum(result, length - 1, 15) < 0)
		return -1;
	return 0;
}

int Code93Reader::checkOneChecksum(string const &result,
	int checkPosition,
	int weightMax)
{
	int weight = 1;
	int total = 0;
	for (int i = checkPosition - 1; i >= 0; i--) {
		total += weight * ALPHABET_STRING.find_first_of(result[i]);
		if (++weight > weightMax) {
			weight = 1;
		}
	}
	if (result[checkPosition] != ALPHABET[total % 47]) {
		return -1;
	}
	return 0;
}
