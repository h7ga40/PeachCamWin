# PeachCam simulator Windows Application

[PeachCam](https://github.com/h7ga40/PeachCam)をWindowsアプリとして、
Visual Studioでコーディングとデバッグをするプロジェクトです。

デバイスへの書き込みの時間をなくし、
デバイスの限られた資源で実現できないデバッグコードを可能にするなど、
Visual Studioの開発環境を利用することで、
PeachCam／[HoikuCam](https://github.com/h7ga40/PeachCam)の開発を楽にするのを目的としています。

Windowsアプリケーションではデバイスを模擬するためのコードや入出力を表示するためのコードが必要になりますが、
C#を使うことでGUIやテストコード実装の手間が減ることを期待しています。


## PeachCam

PeachCamは、[がじぇるね](http://gadget.renesas.com)の[GR-PEACH](http://gadget.renesas.com/ja/product/peach.html)を使用したカメラで、
[WIRELESS CAMERA シールド](https://www.core.co.jp/product/m2m/gr-peach/audio-camera.html)と
[4.3インチ LCDシールド](https://www.core.co.jp/product/m2m/gr-peach/gr-lcd.html)を取り付け、
[FLiR開発キット](https://www.switch-science.com/catalog/2107/)をWIRELESS CAMERA シールドのPMOD端子に
取り付けた構成となっています。

Windowsのシミュレーションでは、PCのカメラをWIRELESS CAMERA シールドのカメラの代わりに使用します。
PCとFLiR開発キットとの接続には、[Adafruit FT232H Breakout](https://www.adafruit.com/product/2264)を介して接続します。

## ソフト構成

|ソフト|フォルダ|目的|ライセンス|
|-|-|-|-|
|開発対象|PeachCam|カメラアプリと[mbed](https://www.mbed.com/)、[がじぇるねライブラリ](https://github.com/d-kato/mbed-gr-libs)||
|[cURL](https://curl.haxx.se/)|libcurl|HTTPクライアント|[Curlライセンス](https://github.com/curl/curl/blob/master/COPYING)|
|[WolfSSL](https://www.wolfssl.com/)|libwolfssl|暗号化通信ライブラリ|GPL 2.0／商用|
|[Expat](https://libexpat.github.io/)|libexpat|XMLパーサー|MITライセンス|
|[zlib](https://www.zlib.net/)|libzlib|データ圧縮伸長ライブラリ|zlibライセンス|
|[OpenCV](https://opencv.org/)|opencv-lib|画像処理用|BSDライセンス|
|[libjpeg](http://www.ijg.org/)|opencv-lib/3rdparty/libjpeg|JPEG画像圧縮伸長ライブラリ|独自ライセンス|
|[libpng](http://www.libpng.org/pub/png/libpng.html)|opencv-lib/3rdparty/libpng|PNG画像圧縮伸長ライブラリ|独自ライセンス|
|[libzxing-cpp](https://github.com/augmate/libzxing-cpp)|libzxing|2D Code読み取りライブラリ|Apache 2.0ライセンス|
|[FLIR Lepton SDK](https://lepton.flir.com/software-sdk/)|liblepton|赤外線放射量計測用|独自ライセンス|
|[Natural Tiny Shell](https://www.cubeatsystems.com/ntshell/)|PeachCam/ntshell|コマンドシェル用|MITライセンス<br/>他|
|シミュレータ|TeraTremConsole|C#化した[TeraTerm](https://ja.osdn.net/projects/ttssh2/)のWinFormコントロール|BSDライセンス|
||LibMPSSE|FT232Hを使うためのライブラリ<br/>FLIR LeptonとPCのUSBとの接続用||
||ITestBench|PeachCamと本体とのインターフェイス||
||TestBenchInit|PeachCamロード前に準備するための処理||
||TestBench|アプリケーション||

各ソフトウェアのライセンスの詳細は、それぞれのサイトを確認してください。

## ライセンス

Apatch 2.0
