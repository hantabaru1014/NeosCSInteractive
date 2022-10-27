# NeosCSInteractive

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that allow C# scripting.

**Currently in WIP!!**

## Memo
- System.Collections.Immutable.dllのバージョン競合問題のため、Microsoft.CodeAnalysis.CSharp.Scriptingのバージョンはあえて古いの使ってる。解決策:自前でビルド??
- `lib/websocket-sharp.dll`はSmartPadで`BeginInvoke`関係のエラーが出たため、[このPR](https://github.com/sta/websocket-sharp/pull/712)をベースにwindowsでも`BeginInvoke`を使わないようにしたもの。
    - net6が原因で`BeginInvoke`が使えない([参照](https://github.com/dotnet/runtime/issues/16312#issuecomment-182107557))のだが、net48とかにすると、今度はRoslynPadが上手く動かなかった。