UniTask is vendored here on purpose.

Unity 6.6 turned UnityEditor.IMGUI.Controls TreeView, TreeViewItem and TreeViewState into
deprecated-as-error, and UniTask's Tracker window still uses them:
Editor/UniTaskTrackerTreeView.cs fails to compile with six CS0619 errors. 2.5.11 and master
carry the same code, so there is no published version to move to.

The package normally resolves into Library/PackageCache, which Unity regenerates, so it cannot
be patched in place. Embedding it here makes it editable, and UniTask.Editor.asmdef now carries
a defineConstraint of UNITASK_TRACKER_ENABLED, which nothing defines - so that editor assembly
is simply not compiled. Only the UniTask Tracker debug window is lost; runtime UniTask, which is
what the games actually use, is untouched.

To undo once Cysharp ships a fix: delete this folder and put
  "com.cysharp.unitask": "<version>"
back into Packages/manifest.json (the OpenUPM scoped registry entry was left in place).

Vendored from 2.5.10.
