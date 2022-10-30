# NeosCSInteractive

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that allow C# scripting.

## Installation
Download the zip file from [Releases](https://github.com/hantabaru1014/NeosCSInteractive/releases/latest) and extract it in a folder with folders such as nml_mods.

## Usage
If the mod loaded successfully, it should have added a screen named NCSI to your DashMenu.  
When you press the "Launch SmartPad" button on the upper right, software that can use REPL and IntelliSense will be launched.  
Have fun!!

### Key bindings in the SmartPad REPL Tab
- Run with Enter
- Line break with Shift+Enter
- Call history with Ctrl+UpArrow/DownArrow

---

## インストール
[Releases](https://github.com/hantabaru1014/NeosCSInteractive/releases/latest)からzipファイルをダウンロードして、nml_mods等のフォルダがある階層で展開してください。

## 使い方
Modが正常に読み込めている場合、ダッシュメニューにNCSIという名前のタブが追加されています。  
右上の「Launch SmartPad」ボタンを押すと、REPLが使えたり、IntelliSenseが使えたりするソフトが立ち上がります。

### SmartPadのREPLタブでの操作方法
- Enterで実行
- Shift+Enterで改行
- Ctrl+UpArrow/DownArrowで履歴の呼び出し

---

## 私用Memo
- System.Collections.Immutable.dllのバージョン競合問題のため、Microsoft.CodeAnalysis.CSharp.Scriptingのバージョンはあえて古いの使ってる。解決策:自前でビルド??
- `lib/websocket-sharp.dll`はSmartPadで`BeginInvoke`関係のエラーが出たため、[このPR](https://github.com/sta/websocket-sharp/pull/712)をベースにwindowsでも`BeginInvoke`を使わないようにしたもの。
    - net6が原因で`BeginInvoke`が使えない([参照](https://github.com/dotnet/runtime/issues/16312#issuecomment-182107557))のだが、net48とかにすると、今度はRoslynPadが上手く動かなかった。