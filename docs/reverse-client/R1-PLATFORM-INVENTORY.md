# R1 platform inventory — 2026-10-22

This is a bounded read-only inventory, not a client build or GPU test. The stopped WinForms/GDI+/WASD prototype remains historical. No replacement engine or third-party package has been installed or selected.

## Reusable code and missing layers

- `src/resource_codec/ResourceContainers.cs` supplies ARZ records and ARC member bytes, with the existing LZ4 implementation. It does not decode scene geometry, meshes, textures or navigation/collision data.
- The existing quest, inventory/reward and `src/session_runtime/SessionStore.cs` code can be considered individually against their recorded support boundaries. Existing authored fixture values are not native initialization defaults.
- Current `src/scene_runtime` renders authored GDI+ geometry and uses authored keyboard motion/collision. Its geometry, motion constants and HUD are not recovered native scene, navigation or UI behavior. Do not revive it by changing only the input keys.
- No new R1 mouse/navigation runtime, independent GPU renderer, scene mesh/texture decoder or native UI dispatcher was found in the current source inventory. Static Rea evidence is not an implementation of those layers.
- The inventoried `ResourceInput.OpenGameResource` helper contains a historical hardcoded installation path; it must not be treated as portable discovery. Prefer the already explicit resource-file input interface.

## Existing build inputs

Only the named files and known installation configuration were checked. No compilation, linking, shader compilation or graphics process was executed for this inventory.

| Input | Observed location/version |
| --- | --- |
| Existing C# compiler | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`; previous M1/M2 manifests record actual use |
| MSVC tools | `D:\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC\14.44.35207\bin\Hostx64\x64\`; `cl.exe` 19.44.35229.0 and `link.exe` 14.44.35229.0 exist |
| Windows SDK | `C:\Program Files (x86)\Windows Kits\10\`, version 10.0.22621.0 |
| SDK headers | `Include\10.0.22621.0\um\windows.h`, `um\d3d11.h`, `shared\dxgi.h` under that SDK root |
| x64 import libraries | `Lib\10.0.22621.0\um\x64\d3d11.lib`, `dxgi.lib`, `user32.lib` under that SDK root |
| Shader compiler runtime | `C:\Windows\System32\D3DCompiler_47.dll`, version 10.0.26100.9711, 4,669,440 bytes |

The checked standard `VC\Auxiliary\Build\vcvars64.bat` and `Microsoft.VCToolsVersion.default.txt` locations were absent. A future narrow native-platform build must resolve its actual include/library/CRT inputs explicitly. Existence of the listed files does not establish full transitive build inputs, a working graphics adapter, feature level, successful presentation or a recovered renderer.

## Implementation decision boundary

A project-owned Win32/D3D11 platform adapter is a candidate using existing Windows inputs; it would implement window/event transport and presentation, not stand in for recovered gameplay. Any selected implementation must connect one shared client state through native-evidence-based mouse/UI routing, camera conversion, scene targeting, navigation and motion. A window or authored scene alone cannot pass R1.

The other independent gap is a scene-data path supplying geometry, render references and collision/navigation inputs. The limited authored content direction may be retained with clear provenance, but its format and traversal behavior must not be labelled as recovered native formats. Resolve these alongside the finite mouse-controller and navigation targets rather than expanding an unlimited low-level call tree.
