// -*- mode:c++; tab-width:2; indent-tabs-mode:nil; c-basic-offset:2 -*-
#ifndef __ONED_READER_H__
#define __ONED_READER_H__

/*
 *  OneDReader.h
 *  ZXing
 *
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

#include <zxing/Reader.h>

namespace zxing {
namespace oned {

class OneDReader : public Reader {
private:
	int doDecode(Ref<BinaryBitmap> image, DecodeHints hints, Ref<Result> &result);

protected:

	struct Range {
	private:
		int data[2];
	public:
		Range() {}
		Range(int zero, int one)
		{
			data[0] = zero;
			data[1] = one;
		}
		int &operator [] (int index)
		{
			return data[index];
		}
		int const &operator [] (int index) const
		{
			return data[index];
		}
	};

	static float patternMatchVariance(std::vector<int> &counters,
		std::vector<int> const &pattern,
		float maxIndividualVariance);
	static float patternMatchVariance(std::vector<int> &counters,
		int const pattern[],
		float maxIndividualVariance);

public:

	OneDReader();
	virtual int decode(Ref<BinaryBitmap> image, DecodeHints hints, Ref<Result> &result);

	// Implementations must not throw any exceptions. If a barcode is not found on this row,
	// a empty ref should be returned e.g. return Ref<Result>();
	virtual int decodeRow(int rowNumber, Ref<BitArray> row, Ref<Result> &result) = 0;

	static int recordPattern(Ref<BitArray> row,
		int start,
		std::vector<int> &counters);
	virtual ~OneDReader();
};

}
}

#endif
