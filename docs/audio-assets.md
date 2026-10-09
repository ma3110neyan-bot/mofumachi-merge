# 音源と出典

2026-10-09。承認済みのオリジナル旋律・編曲を、CC0の実録ピアノ、マリンバ、ベル、柔らかい弦、シェイカー／ボンゴでレンダリング。旧実行時オシレーターは削除しました。第三者の曲は使用していません。

VCSL revision `c1ea7bcc3c7309650ab0da9d15c9cd1fbc4a4c7e`、VSCO-2-CE revision `440300901dfe9275fd84e0b7763af1f8443ae62e`。全23サンプルの原出典、SHA、ライセンス全文は [音素材記録](../References/Preview/release-review-2026-10-09/audio/sources/) に保存しています。UIFontはOFL1.1、UIFont-LICENSE.txt参照。キャラクターは既存の正式原画で変更ありません。

BGMは102BPM・16小節、終止コードを除き余韻を先頭へ折り返した連続ループ。BGMはStreaming、SEはDecompressOnLoad、2D。常駐AudioManagerが音量・ON/OFFを別々に管理します。SEは一つの音声で重なりを制限し、操作／キャラ音は成功音を遮りません。新しい成功イベントは前の成功音を置換します。これは高速な操作で音量が加算されることを防ぐための実装上の判断です。

FFmpegデコード検査は有限値、ピーク、長さ、境界差分のみ。境界差分は音質評価ではありません。ループの継ぎ目・各SE・最大音量時の音割れは、Windows再生とPixel 3aスピーカーで未確認です。

```json
[
  {
    "file": "bgm-town-loop.ogg",
    "sha256": "aeec5af9fb8eba5015d06343aead86cbe228ad95130491b3632016aceb0b4c58",
    "bytes": 743083,
    "seconds": 37.64705215419501,
    "peakDbFS": -11.69,
    "boundaryDelta": 0.001363
  },
  {
    "file": "se-character.ogg",
    "sha256": "57852e403d9bf60ef1b563c62ad7bebeaa311c7812148e18097ba4bf89dd981e",
    "bytes": 12572,
    "seconds": 0.3570975056689342,
    "peakDbFS": -9.11,
    "boundaryDelta": 0.001824
  },
  {
    "file": "se-confirm.ogg",
    "sha256": "fe23178e0455881cb8009c7182a1af8bfef734577e9acec2cfd12d59770f67bb",
    "bytes": 10916,
    "seconds": 0.37709750566893424,
    "peakDbFS": -9.09,
    "boundaryDelta": 0.000507
  },
  {
    "file": "se-delivery.ogg",
    "sha256": "3b72589de6f771f94b8a19896751d550e2d22e060eb3ae0440dee38c16c1eb53",
    "bytes": 25007,
    "seconds": 1.18,
    "peakDbFS": -7.08,
    "boundaryDelta": 9.1e-05
  },
  {
    "file": "se-growth.ogg",
    "sha256": "45b1a31c8eaa0261a9d8f8a3606c6ef93b6859fe4ad945a95631dd7e332e9248",
    "bytes": 61399,
    "seconds": 2.85,
    "peakDbFS": -7.11,
    "boundaryDelta": 0.00012
  },
  {
    "file": "se-merge.ogg",
    "sha256": "928991d1c0ef89e6aeac1c2b20adb87a99f93ec6f518bf3d243272255666a57b",
    "bytes": 22234,
    "seconds": 0.9970975056689343,
    "peakDbFS": -6.63,
    "boundaryDelta": 0.000185
  },
  {
    "file": "se-reward.ogg",
    "sha256": "14a61947f725dfe4c8a33c97ba400943f04919c2f9c17129828ee09c3338db2c",
    "bytes": 38747,
    "seconds": 1.85,
    "peakDbFS": -6.92,
    "boundaryDelta": 0.000105
  }
]
```
