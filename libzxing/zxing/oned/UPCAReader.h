// -*- mode:c++; tab-width:2; indent-tabs-mode:nil; c-basic-offset:2 -*-
#ifndef __UPCA_READER_H__
#define __UPCA_READER_H__
/*
 *  UPCAReader.h
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

#include <zxing/oned/EAN13Reader.h>
#include <zxing/DecodeHints.h>

namespace zxing {
namespace oned {

class UPCAReader : public UPCEANReader {

private:
	EAN13Reader ean13Reader;
	static int maybeReturnResult(Ref<Result> result, Ref<Result> &result2);

public:
	UPCAReader();

	int decodeMiddle(Ref<BitArray> row, Range const &startRange, std::string &resultString);

	int decodeRow(int rowNumber, Ref<BitArray> row, Ref<Result> &result);
	int decodeRow(int rowNumber, Ref<BitArray> row, Range const &startGuardRange, Ref<Result> &result);
	int decode(Ref<BinaryBitmap> image, DecodeHints hints, Ref<Result> &result);

	BarcodeFormat getBarcodeFormat();
};

}
}

#endif
