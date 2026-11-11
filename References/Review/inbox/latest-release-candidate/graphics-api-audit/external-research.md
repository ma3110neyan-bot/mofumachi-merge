# 外部公式情報と取得限界

調査日は2026-11-11（日本時間）。Unityを起動せず、公開URLをHTTPで読むだけ。ローカルsource・snapshot・秘密値を送信していない。取得記録とSHAはexternal-retrieval.json。

## 対象版と一致する取得済み一次情報

- [公式6000.6.4f1 README](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.6.4f1/README.md)：先頭でUnity6000.6.4f1のC#参照ソースと明記。
- [公式6000.6.4f1 PlayerSettings bindings](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.6.4f1/Editor/Mono/PlayerSettings.bindings.cs)：904〜914行。GetGraphicsAPIsとSetGraphicsAPIsはnative実装へ委譲し、setterはeditor cacheを同期する。1856行のYAML serializationもexternで、C++ serializer本文はこのC#ファイルに含まれない。
- [公式6000.6.4f1 GraphicsEnums](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.6.4f1/Runtime/Export/Graphics/GraphicsEnums.cs)：1455行でOpenGLES3の列挙値11を確認。

8桁hexを4byte little-endian整数として解釈すれば0b000000は11になる。しかし、この数値対応だけでは、追加文字iを無視してよいというUnity YAML仕様や、変更後scalarの意味値の等価性を証明できない。取得できたC# bindings/enumは末尾iの正規保存表現を規定していない。この限定ソースに記述がないことをもって、Unity全体にその仕様がないとは断定しない。

masterのREADMEは7000.0.0a16だったため、対象6000.6.4f1の根拠から除外した。6000.6ブランチも取得したが、対象タグを優先する。外部ソース本文をそのまま再配布せず、参照URL・版・SHAと調査所見のみ共有する。

## 取得できなかった情報

6000.6公式のSetGraphicsAPIs APIページ、UnityYAML/FormatDescription/script-serialization manual、Issue Trackerの0b000000i/m_APIs検索、公式forumの完全一致検索は、通信経路でTunnel connection failed:403 Forbiddenとなった。権限付きHTTPでも同じ制約であり、本文未取得。この結果は検索0件・文書不存在・仕様不存在の証拠ではない。

検索対象語：Unity6000.6 ProjectSettings m_APIs、Android m_BuildTargetGraphicsAPIs serialization、OpenGLES3 serialized ProjectSettings、0b000000i、YAML integer suffix i Unity、Unity serialized integer suffix i。公式サイト本文の確認を完了したとは扱わない。

## 判定

公式情報で確認できたのはAPIのnative委譲とOpenGLES3=11。**末尾iの正当性・意味値等価性は未確認**。正当なUnity副作用とも、corruptionとも断定しない。guard exact許可候補はNO。
