/* Selected-function Ghidra pseudocode. Not original source; no auto-analysis.
   Types/signatures and shared tail-call expansion are incomplete/unverified. */

/* 1802c7f50 ?GetRootSavePath@GameEngine@GAME@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ */

undefined8
_GetRootSavePath_GameEngine_GAME__SA_AV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__XZ
          (undefined8 param_1,undefined8 param_2,undefined8 param_3,undefined8 param_4)

{
  undefined8 uVar1;
  undefined4 uVar2;
  undefined8 uVar3;
  undefined8 auStack_28 [3];
  ulonglong uStack_10;
  
                    /* 0x2c7f50  15991
                       ?GetRootSavePath@GameEngine@GAME@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ
                        */
  uVar3 = 0xfffffffffffffffe;
  uVar2 = 0;
  uVar1 = FUN_180593560(auStack_28);
  FUN_180301350(param_1,uVar1,&UNK_180693d00,param_4,uVar2,uVar3);
  if (0xf < uStack_10) {
    FUN_180016620(auStack_28[0],uStack_10 + 1,1);
  }
  return param_1;
}


/* 1802e3270 ?GetUserSaveDataFolder@GameEngine@GAME@@QEBA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ */

undefined8 *
_GetUserSaveDataFolder_GameEngine_GAME__QEBA_AV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__XZ
          (longlong param_1,undefined8 *param_2,undefined8 param_3,undefined8 param_4)

{
  undefined8 *puVar1;
  undefined8 uVar2;
  undefined4 uVar3;
  undefined8 uVar4;
  undefined8 auStack_28 [3];
  ulonglong uStack_10;
  
                    /* 0x2e3270  17181
                       ?GetUserSaveDataFolder@GameEngine@GAME@@QEBA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ
                        */
  uVar4 = 0xfffffffffffffffe;
  param_2[3] = 0xf;
  param_2[2] = 0;
  puVar1 = param_2;
  if (0xf < (ulonglong)param_2[3]) {
    puVar1 = (undefined8 *)*param_2;
  }
  *(undefined1 *)puVar1 = 0;
  uVar3 = 1;
  if (*(char *)(param_1 + 0x375ea) == '\0') {
    uVar2 = _GetBaseFolder_GameEngine_GAME__SA_AV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__XZ
                      (auStack_28);
    FUN_1800309b0(param_2,uVar2);
    if (0xf < uStack_10) {
      FUN_180016620(auStack_28[0],uStack_10 + 1,1);
    }
  }
  FUN_180089e10(param_2,&UNK_18069a2b8,5,param_4,uVar3,uVar4);
  return param_2;
}


/* 1802e3100 ?GetBaseFolder@GameEngine@GAME@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ */

undefined8
_GetBaseFolder_GameEngine_GAME__SA_AV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__XZ
          (undefined8 param_1,undefined8 param_2,undefined8 param_3,undefined8 param_4)

{
  undefined8 uVar1;
  undefined4 uVar2;
  undefined8 uVar3;
  undefined8 auStack_28 [3];
  ulonglong uStack_10;
  
                    /* 0x2e3100  12123
                       ?GetBaseFolder@GameEngine@GAME@@SA?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@XZ
                        */
  uVar3 = 0xfffffffffffffffe;
  uVar2 = 0;
  uVar1 = _GetRootSavePath_GameEngine_GAME__SA_AV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__XZ
                    (auStack_28);
  FUN_180301350(param_1,uVar1,&UNK_1806190ec,param_4,uVar2,uVar3);
  if (0xf < uStack_10) {
    FUN_180016620(auStack_28[0],uStack_10 + 1,1);
  }
  return param_1;
}


