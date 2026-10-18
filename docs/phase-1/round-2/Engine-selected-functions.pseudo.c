/* Selected-function Ghidra pseudocode. Not original source; no auto-analysis.
   Types/signatures and shared tail-call expansion are incomplete/unverified. */

/* 1800738e0 ?SetSavePath@Engine@GAME@@QEAAXAEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z */

/* WARNING: Removing unreachable block (ram,0x0001800231aa) */

undefined8 *
_SetSavePath_Engine_GAME__QEAAXAEBV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std___Z
          (longlong param_1,undefined8 *param_2)

{
  code *pcVar1;
  undefined8 *puVar2;
  undefined8 *in_RAX;
  undefined8 *puVar3;
  ulonglong uVar4;
  
                    /* 0x738e0  5509
                       ?SetSavePath@Engine@GAME@@QEAAXAEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z
                        */
  puVar3 = (undefined8 *)(param_1 + 0x240);
  if (puVar3 == param_2) {
    return in_RAX;
  }
  uVar4 = 0xffffffffffffffff;
  if (param_2[2] != 0xffffffffffffffff) {
    uVar4 = param_2[2];
  }
  if (puVar3 == param_2) {
    if (*(ulonglong *)(param_1 + 0x250) < uVar4) {
      __Xout_of_range_std__YAXPEBD_Z(&UNK_180339fa0);
      pcVar1 = (code *)swi(3);
      puVar3 = (undefined8 *)(*pcVar1)();
      return puVar3;
    }
    *(ulonglong *)(param_1 + 0x250) = uVar4;
    puVar2 = puVar3;
    if (0xf < *(ulonglong *)(param_1 + 600)) {
      puVar2 = (undefined8 *)*puVar3;
    }
    *(undefined1 *)((longlong)puVar2 + uVar4) = 0;
    FUN_180022e30(puVar3,0);
  }
  else {
    if (uVar4 == 0xffffffffffffffff) {
      __Xlength_error_std__YAXPEBD_Z(&UNK_180339fb8);
      pcVar1 = (code *)swi(3);
      puVar3 = (undefined8 *)(*pcVar1)();
      return puVar3;
    }
    if (*(ulonglong *)(param_1 + 600) < uVar4) {
      FUN_180022f70(puVar3,uVar4,*(undefined8 *)(param_1 + 0x250));
      if (uVar4 == 0) {
        return puVar3;
      }
    }
    else if (uVar4 == 0) {
      *(undefined8 *)(param_1 + 0x250) = 0;
      if (0xf < *(ulonglong *)(param_1 + 600)) {
        *(undefined1 *)*puVar3 = 0;
        return puVar3;
      }
      *(undefined1 *)puVar3 = 0;
      return puVar3;
    }
    if (0xf < (ulonglong)param_2[3]) {
      param_2 = (undefined8 *)*param_2;
    }
    puVar2 = puVar3;
    if (0xf < *(ulonglong *)(param_1 + 600)) {
      puVar2 = (undefined8 *)*puVar3;
    }
    if (uVar4 != 0) {
      func_0x0001802ddb0e(puVar2,param_2,uVar4);
    }
    *(ulonglong *)(param_1 + 0x250) = uVar4;
    puVar2 = puVar3;
    if (0xf < *(ulonglong *)(param_1 + 600)) {
      puVar2 = (undefined8 *)*puVar3;
    }
    *(undefined1 *)((longlong)puVar2 + uVar4) = 0;
  }
  return puVar3;
}


/* 180073700 ?GetBaseDataPathFromOptions@Engine@GAME@@QEAAXAEAV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z */

void _GetBaseDataPathFromOptions_Engine_GAME__QEAAXAEAV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std___N_Z
               (undefined8 param_1,undefined1 *param_2)

{
  undefined8 uVar1;
  undefined1 *puVar2;
  undefined1 *puVar3;
  longlong lVar4;
  undefined1 uStack_198;
  undefined7 uStack_197;
  longlong lStack_188;
  ulonglong uStack_180;
  undefined8 uStack_178;
  undefined8 auStack_170 [3];
  ulonglong uStack_158;
  int aiStack_148 [80];
  
                    /* 0x73700  2396
                       ?GetBaseDataPathFromOptions@Engine@GAME@@QEAAXAEAV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@_N@Z
                        */
  uStack_178 = 0xfffffffffffffffe;
  uVar1 = _GetOptions_Engine_GAME__QEAAPEAVOptions_2_XZ();
  _GetString_Options_GAME__QEBA_BV__basic_string_DU__char_traits_D_std__V__allocator_D_2__std__W4StringName_12__Z
            (uVar1,&uStack_198,1);
  if (lStack_188 == 0) goto LAB_18007387e;
  puVar3 = (undefined1 *)CONCAT71(uStack_197,uStack_198);
  puVar2 = &uStack_198;
  if (0xf < uStack_180) {
    puVar2 = puVar3;
  }
  if (puVar2[lStack_188 + -1] == '\\') {
LAB_180073785:
    puVar3 = (undefined1 *)FUN_18007ac80(&uStack_198,auStack_170,0,lStack_188 + -1);
    if (&uStack_198 != puVar3) {
      if (0xf < uStack_180) {
        FUN_180023090(CONCAT71(uStack_197,uStack_198),uStack_180 + 1,1);
      }
      uStack_180 = 0xf;
      lStack_188 = 0;
      uStack_198 = 0;
      FUN_180022860(&uStack_198,puVar3);
    }
    if (0xf < uStack_158) {
      FUN_180023090(auStack_170[0],uStack_158 + 1,1);
    }
    puVar3 = (undefined1 *)CONCAT71(uStack_197,uStack_198);
  }
  else {
    puVar2 = &uStack_198;
    if (0xf < uStack_180) {
      puVar2 = puVar3;
    }
    if (puVar2[lStack_188 + -1] == '/') goto LAB_180073785;
  }
  puVar2 = &uStack_198;
  if (0xf < uStack_180) {
    puVar2 = puVar3;
  }
  lVar4 = FindFirstFileExA(puVar2,0,aiStack_148,0,0,0);
  if ((lVar4 == -1) || (aiStack_148[0] != 0x10)) {
    FUN_180022ce0(&uStack_198,&UNK_180307f69,0);
  }
  if (lStack_188 != 0) {
    FUN_1800310d0(&uStack_198,&UNK_180309af4,1);
  }
LAB_18007387e:
  if (param_2 != &uStack_198) {
    FUN_180023180(param_2,&uStack_198,0,0xffffffffffffffff);
  }
  if (0xf < uStack_180) {
    FUN_180023090(CONCAT71(uStack_197,uStack_198),uStack_180 + 1,1);
  }
  return;
}


/* 1800777d0 ?GetDatabaseArchive@Engine@GAME@@QEAAPEAVDatabaseArchive@2@XZ */

undefined8 _GetDatabaseArchive_Engine_GAME__QEAAPEAVDatabaseArchive_2_XZ(longlong param_1)

{
                    /* 0x777d0  2586  ?GetDatabaseArchive@Engine@GAME@@QEAAPEAVDatabaseArchive@2@XZ
                        */
  return *(undefined8 *)(param_1 + 0x3d0);
}


