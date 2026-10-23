# Item's two GameEngine field sources

2026-10-21, bounded investigation in progress. This is native static evidence, separate from own-model tests and loaded runtime compatibility.

## Fixed original and recovered body

Game.dll remains the pinned11385856-byte file with SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. A metadata-only read established .pdata entry10475: `[2C8750,2D41A1)`,47697 bytes, unwind820208. Only this body was then read once. One dumpbin invocation decoded a50176-byte synthetic PE, never the original DLL as a program. All10886 instruction byte strings concatenate exactly to the saved body, with contiguous addresses and no decode issues. There are1904 syntactic calls and715 direct branches; these counts do not establish their runtime success or reachability.

The boundary reader returned exit0 in tool chunk5f633f. Its inspection SHA256 is `6eeb71489737ba0e3a63c8a6b716eebe954642cdd40cb46939d88efd37db630f`. The body reader was fully reviewed in1b4e21 and ran once inb882fc, exit0. Its inspection is `offline-item-global-load-body/inspection.json` (SHA256 `26a01be264dbad6706bdc6e8553d37ba8811f7cf523eda93e0e5d21d0a9f33a8`); decoded instructions SHA256 `cee579cfa26e049167595ac55792993201e6268bd459474da6689f648e382262`; exact body SHA256 `b3f481a409a68dde7d65601d3beeeea085d958e5b1879cfc7a3dd41963b5f661`. Both stages used same-handle64KiB before/after original hashes, which matched, and closed their handles.

## Direct writes and their actual order

The saved Item caller reads GameEngine+1B58 and+1B5C into Item+B68 and+B6C. In2C8750 the directly addressed writers are:

| RVA | Operation |
| --- | --- |
| 2CEC27 | Read the current vptr from the table held in RSI |
| 2CEC2A / 2CEC33 | Read scalar7A80B8 and pass its low32 bits as the third argument in XMM2 |
| 2CEC37 / 2CEC3E / 2CEC41 | Key pointer696378, this=RSI, call captured vptr+48 |
| 2CEC44 | MOVSS DWORD[original GameEngine+1B58],XMM0 |
| 2CEC4D / 2CEC50 | Read RSI's vptr again, then scalar7A80C8 into XMM2 |
| 2CEC58 / 2CEC5F / 2CEC62 | Key pointer696360, same RSI identity, call this newly captured vptr+48 |
| 2CEC65 | MOVSS DWORD[original GameEngine+1B5C],XMM0 |

There is no conditional branch or success test between these two calls and stores. A first normal getter return reaches its DWORD store before the second vptr/default read. A second unresolved or abnormal return does not justify erasing that first write. Each store copies raw32-bit return bits; no integer conversion or clamping is visible here. The twelve-byte own global view must retain bytes+1B60..63 when modeling this two-store window. The following2CEC7E call through+38 and2CEC81 DWORD write to+1B60 are distinct and are not silently included.

R13 captures original this at2C87E3. It is temporarily repurposed later, then restored at2C9AFD from the saved entry argument; the independent writer review records the stack/register proof. Claims here are conditional on the normal x64 preservation of nonvolatile registers by reached callees, not evidence that every preceding call returns normally.

## The source table is a second record

The entry path obtains its first table into R14 at2C881B. The two target fields instead use RSI assigned at2CA485, from the result of2CA47F through Game IAT609A90. Immediately before that, the first table's string getter at2CA411 (vptr+28, key694728, default pointer61812F) supplies a C string. The caller scans that returned string and assigns it into a fresh local string through016460 at2CA453. It then calls609090, obtains the singleton again, and calls609A90 with that local name. The609090 return is ignored. This is not a verified null/failed-load fallback branch.

The pointer key identities, scalar bits and fixed import/slot bindings are being read separately. Existing string getter evidence cannot identify a float getter. Existing TableDepot::LoadFile evidence also cannot be substituted for ObjectManager::LoadTableFile/GetLoadTable until their direct relation is independently bound. Missing records, unknown table kinds, null references, preceding calls, later writers, object lifetime/publication and exception cleanup therefore remain explicit conditions.

No own code was compiled or tested for this new round at this stage. The earlier59/10 ItemReturn and76/9 ItemName results are historical and have not been rerun. No game/Rea/Java, save/cloud, installation, security change, commit or push occurred.

## Fixed inputs now bound

The separately reviewed input reader ran once in5ffd37, exit0. `offline-item-global-load-inputs/inspection.json` SHA256 is `75046b8f1e04fff6d60c07a175ffdfa20cbb24218aa8ff424d39894190b9f1d0`. It read26 Game code-anchor bytes,90 Game data bytes,401 Game import-metadata bytes and16 Engine slot bytes. All five ASCII strings were read only through their first NUL; no record/archive contents or live globals were accessed. Both original hashes matched before and after.

| Input | Confirmed disk contents |
| --- | --- |
| Game693A48 | `records/game/gameengine.dbr` |
| Game694728 | `itemInfoRecord` |
| Game61812F | Empty C string |
| Game696378 | `itemSparkleMinTime` |
| Game696360 | `itemSparkleMaxTime` |
| Game7A80B8 | `00004040`, binary32 bits40400000 (3.0) |
| Game7A80C8 | `0000a040`, binary32 bits40A00000 (5.0) |
| Engine LoadTable341F10+48 | Preferred pointer18013CA00 |
| Engine LoadTableBinary341FE0+48 | Preferred pointer18013FD50 |

The26-byte constructor anchors independently verify the local2C08CC assignment of the primary record-name pointer and the2C18CA..2C18D5 argument setup/direct call to2C8750. Fixed import metadata binds609090 to `ObjectManager::LoadTableFile` (hint4239),609098 to `Singleton<ObjectManager>::Get` (hint2305), and609A90 to `ObjectManager::GetLoadTable` (hint3007). This verifies symbol identity, not their wrapper behavior, loaded singleton identity or current vtable targets.

Thus the field-source route is the primary record's `itemInfoRecord` string, followed by the secondary table's `itemSparkleMinTime` and `itemSparkleMaxTime` getters.3.0/5.0 are actual supplied default arguments here, not yet proof that getters select them or that they remain the live global values after loading. Next selected scope: exact Engine13CA00/13FD50 getter bodies and the two fixed ObjectManager exports bound through their exact import hints/names. Each body is capped at2048 bytes, with no callee recursion or export scan.

The intermediate writer checkpoint is `.tooling/checkpoints/item-global-load-writers-static/manifest.json`,15 files/2651822 payload bytes, SHA256 `192dba0eb06f4422833703984b364f0465d014b5aadd3607e3dc60d42f7a0f11`. Its copies preserve this document before this appended input section; all copies were verified at sealing. It references the prior565-file checkpoint without duplicating it.

## Getter and wrapper boundaries

The four-target reader was reviewed in d54431 and executed once in b4307c, exit0. It read406 Engine code bytes and637 metadata bytes, using one dumpbin call. Base getter13CA00 has an exact406-byte body ending13CB96. The Binary13FD50 and exported ObjectManager17EE80/17EE90 entries fall in .pdata gaps; no code was read at those entries by that reader. Its inspection SHA256 is `4be3c63f988ab540100dbb80129506bc13fa254f5bfa868fd358b45090900b78`. This is a successful bounded observation with three evidence frontiers, not a tool denial or failed execution.

Base float13CA00 uses the existing13EBC0 lower_bound/search proof, key C-string copy and gate contract. A miss retains the supplied default. A match selects the stored value's inline/heap storage without the string getter's empty-value-length test, and calls4DD80 with format pointer316034 and a local DWORD initialized from the default. Only a normal EAX==1 causes that local DWORD to replace the retained default. Other normal return counts preserve the default. Unknown parsing, unreadable storage and abnormal return must not be collapsed into that known fallback. `float-base-review.md` preserves the independent review;4DD80 and the format contents remain unexamined here.

To identify the three short entries without inventing function ends, a separately reviewed reader (9e5cd4) read only three fixed windows totaling80 bytes and ran once (9ab33d, exit0). Windows stop at known next entries; only the first linear prefix through its first transfer is interpreted. Remaining bytes are unclassified and are not assigned to the selected function. Inspection SHA256: `75d55f7a959c2b012cc5e9374a25bf588f6457bf1f2ef5a8867805942a2d1e66`.

-13FD50's first16 bytes reread this.vptr, move the original third argument XMM2 to XMM3, set R8D=0, then tail-jump through the newly read vptr+98. It does **not** directly transfer to13FD70. The conditional13FD70 read was therefore not selected. There are two distinct dispatch reads: the Game caller's+48 selection, then this Binary entry's own+98 selection.
-17EE80's first12 bytes add170 to ObjectManager this and tail-jump to1C4C30. The requested record-name argument is unchanged.
-17EE90's first12 bytes add170 to this and tail-jump to1C4D20. This now binds LoadTableFile to the previously saved TableDepot::LoadFile contract, including its cache/miss branches; it does not bind GetLoadTable to the same helper.

The next narrow read is the exported Binary vtable341FE0+98 slot342078 and only that pointer's exact body (at most1024 bytes), plus the directly referenced1C4C30 exact body (at most1024 bytes). No neighboring function, Singleton implementation, Base parser/CRT or recursive callee traversal is selected. All original before/after hashes still match; no build/test was needed to obtain these static facts.

## Bound Binary getter and secondary-table selection

That selected read completed once in38d9a6 (full script review6d3a45):385 code bytes,168 metadata bytes,8 slot bytes,2 dumpbin calls. Inspection `offline-item-global-load-direct/inspection.json` SHA256 is `dd24c4565a04de81051c8866e7d927cff0e807bd5ec48d88a899c1febff65b1c`. Slot342078 contains the preferred pointer1801402E0. Its exact .pdata body is [1402E0,140378),152 bytes. The second body is [1C4C30,1C4D19),233 bytes. This does not assign the unrelated neighboring13FD70 body to the Binary scalar getter.

1402E0 saves the fourth-position XMM3 default, R8D element index and original table/key identity. It enters the gate addressed from table+28, then reads archive pointer table+C8. A known null archive skips the query/name cache and numeric table entirely and returns the default after unlock. A present archive supplies archive+88 to283840 with the original key pointer. Its returned EAX is passed, without a -1 test, to140420 with the original table, saved index and default. Cache writes made by283840 precede numeric access and survive any later unsupported own-model stop. The native gate pointer is reread for unlock; managed finally/release does not establish native EH or changing-gate behavior.

1C4C30 is GetLoadTable's distinct TableDepot helper. It locks depot+78, copies the name using its full stored length, normalizes through27B3C0, and looks up depot+8 through03C8E0. A sentinel result **or a matching node with null payload** selects the pointer at depot+48. Otherwise it captures node+30. It frees the temporary string where required, unlocks, and returns the captured pointer. It does not increment a hit counter or call a loader. In contrast, the previously evidenced LoadFile helper increments its hit count even for a null cached payload and returns that null. The depot+48 fallback's identity, dynamic type, constructor and lifetime remain unknown. Neither helper proves that the two Singleton calls return the same live ObjectManager.

Independent review: `item-global-load-validation/direct-review.md`, SHA256 `ade55f7667899e363ac9607c6be4d16265047bcf6cf72b80bf3c585933ba7070`.

## Numeric140420 reachable control flow

The last selected reader was fully reviewed ine5a772 and executed once in8317e5, exit0. It verified raw CALL140349 from the pinned152-byte body before selecting140420. No exact .pdata entry begins there: previous [1403F9,140419), next [140490,14051A). The read is therefore explicitly the112-byte entry window [140420,140490), **not an unwind-proven complete function boundary**. Three new .pdata entries add36 metadata bytes; one dumpbin call decodes all112 bytes. Inspection SHA256 is `9eed3516ef05b1cc0de18a24e3ea2b8e28050cb29480ffaaba8be7dc3930da4a`; selected bytes SHA256 `dc5545b6d0f7bc5a0f7812ea8863b9a839f1ad5a1ccb3d2c4786bedf21fabe51`.

All seven conditional branches from the entry target internal instruction boundaries. Both normal RETs (140464 and14048E) are inside the window; no call or indirect jump exists. The final14048F byte is CC padding, not reachable from the established entry CFG and not asserted to establish a function boundary. This closes the normal instruction paths locally, subject to validity of the data each path reads:

1. Sign-extend the int32 name ID into a QWORD. Search table+B8's bucket array using the stored shift at+A0 and mask at+A8. Null bucket storage or no exact QWORD key returns the supplied raw default.
2. Dereference the found node's payload at+8. An unknown or invalid payload is not a proven miss. If its DWORD type at+0 differs from1, return the default.
3. Read DWORD count at payload+8. Compare the requested element index unsigned. If index>=count, replace it with count-1 modulo2^32. Count0 consequently yields FFFFFFFF, not index0 or a fallback.
4. Add payload's DWORD base index at+4 to that index modulo2^32. Read the raw DWORD at table+48's buffer plus the zero-extended sum times4. MOVSS returns those bits unchanged.

There is no integer-to-float conversion, arithmetic, NaN filtering or textual parsing in this helper. This establishes tag1 as the raw floating-value buffer for this getter. It does not revise the parser's existing narrower accepted input domain (positive counts, bounded fresh buffers), make malformed storage safe, or cover all binary getter variants.

## Conditions remaining at the integration boundary

The verified source is now primary record `records/game/gameengine.dbr` -> its `itemInfoRecord` string -> the second selected table -> two float getters with explicit3/5 defaults -> GameEngine DWORDs1B58/1B5C -> the existing Item copy/conditional return model. The own integration may use a fresh parsed binary fixture, shared name cache and the same mutable12-byte GameEngine view. It must keep the original table/key/field identities, read vptr+48 each time, then independently reread vptr+98 inside13FD50, and preserve completed first writes/cache effects when later work stops.

This investigation does not close all1904 calls in2C8750, native archive fetching, the actual chosen second record and values in a formal sample, Singleton/fallback lifetime, Base textual float conversion, gEngine publication/later writers, or native allocations/threading/EH. The Item RNG/FP conditions, string alias/growth and cleanup gaps, and complete QuestItem constructor remain separate. Disk constants are an explicitly selectable input profile, not a promise about every loaded process. Original source handles were read-only, closed, and matched the pinned before/after hashes in every new reader; no original module was loaded or executed.
