# このフォルダについて

FTDIのLibMPSSEのファイルを置きます。

下記のサイトの*LibMPSSE-SPI*のリンクから、ライブラリをダウンロードします。
https://www.ftdichip.com/Support/SoftwareExamples/MPSSE/LibMPSSE-SPI.htm

展開したファイルを、下記のようなフォルダ構成になるよう配置します。
```
LibMPSSE
├─include
│  │  libMPSSE_spi.h
│  └─windows
│          ftd2xx.h
├─lib
│  └─windows
│      ├─i386
│      │      libMPSSE.lib
│      │
│      └─x64
│              libMPSSE.lib
```
